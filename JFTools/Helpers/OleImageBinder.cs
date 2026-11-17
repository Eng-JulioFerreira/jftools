using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace JFTools.Helpers
{
    /// <summary>
    /// Post-export processor: opens a DWG in AutoCAD via COM automation,
    /// converts every AcDbRasterImage entity to an embedded OLE picture,
    /// deletes source image files after the DWG is closed, and removes
    /// AutoCAD sidecar files (.bak, .pcp).
    /// </summary>
    internal static class OleImageBinder
    {
        // ── Windows clipboard API ────────────────────────────────────────
        [DllImport("user32.dll")] static extern bool OpenClipboard(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool EmptyClipboard();
        [DllImport("user32.dll")] static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
        [DllImport("user32.dll")] static extern bool CloseClipboard();
        [DllImport("gdi32.dll")]  static extern IntPtr GetEnhMetaFile(string lpszMetaFile);
        [DllImport("gdi32.dll")]  static extern bool   DeleteEnhMetaFile(IntPtr hemf);
        private const uint CF_ENHMETAFILE = 14;

        // ── Windows dialog dismissal API ─────────────────────────────────
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        private const uint WM_COMMAND = 0x0111;
        private const int  IDOK       = 1;

        // ── Public API ───────────────────────────────────────────────────

        public static bool IsAvailable()
        {
            using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Autodesk\AutoCAD"))
                if (k != null) return true;
            using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Autodesk\AutoCAD"))
                return k != null;
        }

        public static string BindImages(string dwgPath)
        {
            if (!File.Exists(dwgPath))
                return $"File not found: {dwgPath}";

            bool launched = false;
            dynamic acad = null;
            try
            {
                acad = GetOrLaunch(out launched);
                if (acad == null)
                    return "AutoCAD not installed or could not start.";

                bool wasVisible = false;
                try { wasVisible = (bool)acad.Visible; } catch { }
                if (!wasVisible) try { acad.Visible = false; } catch { }

                try { acad.SetSystemVariable("FILEDIA", 0); } catch { }
                try { acad.SetSystemVariable("CMDECHO", 0); } catch { }

                var filesToDelete = new List<string>();

                dynamic doc = null;
                try
                {
                    doc = acad.Documents.Open(dwgPath, false);
                    if (!wasVisible) try { acad.Visible = false; } catch { }
                    doc.Activate();
                    try { acad.SetSystemVariable("ISAVEBAK", 0); } catch { }

                    BindAll(doc, filesToDelete);
                    doc.Save();
                    return null;
                }
                finally
                {
                    if (doc != null) try { doc.Close(false); } catch { }
                    if (!wasVisible) try { acad.Visible = false; } catch { }

                    // Delete image files after AutoCAD releases them
                    foreach (var f in filesToDelete)
                        try { if (File.Exists(f)) File.Delete(f); } catch { }

                    CleanSidecarFiles(dwgPath);
                }
            }
            catch (Exception ex) { return ex.Message; }
            finally
            {
                if (launched && acad != null) try { acad.Quit(); } catch { }
                if (acad != null) try { Marshal.ReleaseComObject(acad); } catch { }
            }
        }

        // ── Core logic ──────────────────────────────────────────────────

        private static void BindAll(dynamic doc, List<string> filesToDelete)
        {
            var modelImgs = Collect(doc.ModelSpace);
            if (modelImgs.Count > 0)
            {
                try { doc.SendCommand("TILEMODE 1\n"); } catch { }
                foreach (var img in modelImgs)
                    ProcessOne(doc, doc.ModelSpace, img, filesToDelete);
            }

            try { doc.SendCommand("TILEMODE 0\n"); } catch { }
            foreach (dynamic layout in doc.Layouts)
            {
                string lname;
                try { lname = (string)layout.Name; } catch { continue; }
                if (lname.Equals("Model", StringComparison.OrdinalIgnoreCase)) continue;

                dynamic block = layout.Block;
                var imgs = Collect(block);
                if (imgs.Count == 0) continue;

                string escaped = lname.Replace("\\", "\\\\").Replace("\"", "\\\"");
                try { doc.SendCommand($"(setvar \"CTAB\" \"{escaped}\")\n"); } catch { continue; }
                try { doc.SendCommand("PSPACE\n"); } catch { }

                foreach (var img in imgs)
                    ProcessOne(doc, block, img, filesToDelete);
            }
        }

        private sealed class ImgInfo
        {
            public dynamic Entity;
            public string  File;
            public double  X, Y, W, H;
        }

        private static List<ImgInfo> Collect(dynamic space)
        {
            var list = new List<ImgInfo>();
            try
            {
                foreach (dynamic e in space)
                {
                    try
                    {
                        if ((string)e.ObjectName != "AcDbRasterImage") continue;
                        string file = (string)e.ImageFile;
                        if (string.IsNullOrEmpty(file) || !File.Exists(file)) continue;
                        double[] origin = (double[])e.Origin;
                        double w = (double)e.ImageWidth;
                        double h = (double)e.ImageHeight;
                        if (w < 1e-9 || h < 1e-9) continue;
                        list.Add(new ImgInfo { Entity = e, File = file, X = origin[0], Y = origin[1], W = w, H = h });
                    }
                    catch { }
                }
            }
            catch { }
            return list;
        }

        private static void ProcessOne(dynamic doc, dynamic space, ImgInfo img, List<string> filesToDelete)
        {
            string emfPath = Path.ChangeExtension(Path.GetTempFileName(), ".emf");
            try
            {
                if (!CreateEmf(img.File, emfPath)) return;
                if (!SetClipboardEmf(emfPath)) return;

                var beforeHandles = SnapshotAllHandles(space);

                // The "OLE Text Size" dialog is modal and blocks SendCommand.
                // Start a background thread that polls for it and clicks OK
                // automatically, so SendCommand can complete unattended.
                var dismissThread = new Thread(() => DismissOleTextSizeDialog(5000))
                    { IsBackground = true };
                dismissThread.Start();

                try { doc.SendCommand("PASTECLIP\n0,0\n"); }
                catch { return; }

                dismissThread.Join(200); // give it a moment to finish if still running

                dynamic ole = FindNewOle(space, beforeHandles);
                if (ole != null)
                {
                    try
                    {
                        ole.Width          = img.W;
                        ole.Height         = img.H;
                        ole.InsertionPoint = new double[] { img.X, img.Y, 0.0 };
                    }
                    catch { }
                }

                try { img.Entity.Delete(); } catch { }
                filesToDelete.Add(img.File);
            }
            finally
            {
                try { File.Delete(emfPath); } catch { }
            }
        }

        // ── OLE Text Size dialog auto-dismissal ──────────────────────────

        /// <summary>
        /// Polls for the AutoCAD "OLE Text Size" dialog for up to
        /// <paramref name="timeoutMs"/> ms and clicks OK when found.
        /// Handles English and Portuguese dialog titles.
        /// </summary>
        private static void DismissOleTextSizeDialog(int timeoutMs)
        {
            // Known titles across AutoCAD versions and languages
            var titles = new[]
            {
                "OLE Text Size",
                "OLE-Textgröße",
                "Dimensões do Texto OLE",
                "Taille de texte OLE",
                "Tamaño de texto OLE",
            };

            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                foreach (var title in titles)
                {
                    IntPtr hwnd = FindWindow(null, title);
                    if (hwnd != IntPtr.Zero)
                    {
                        // WM_COMMAND with IDOK = click the OK / default button
                        PostMessage(hwnd, WM_COMMAND, (IntPtr)IDOK, IntPtr.Zero);
                        return;
                    }
                }
                Thread.Sleep(50);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static HashSet<string> SnapshotAllHandles(dynamic space)
        {
            var set = new HashSet<string>();
            try
            {
                foreach (dynamic e in space)
                    try { set.Add((string)e.Handle); } catch { }
            }
            catch { }
            return set;
        }

        private static dynamic FindNewOle(dynamic space, HashSet<string> before)
        {
            try
            {
                foreach (dynamic e in space)
                {
                    try
                    {
                        string on = (string)e.ObjectName;
                        if ((on == "AcDbOle2Frame" || on == "AcDbOleFrame")
                            && !before.Contains((string)e.Handle))
                            return e;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private static void CleanSidecarFiles(string dwgPath)
        {
            foreach (var ext in new[] { ".bak", ".pcp" })
            {
                string f = Path.ChangeExtension(dwgPath, ext);
                try { if (File.Exists(f)) File.Delete(f); } catch { }
            }
        }

        // ── EMF creation ────────────────────────────────────────────────

        private static bool CreateEmf(string imageFile, string emfPath)
        {
            try
            {
                using var bmp = new Bitmap(imageFile);
                using var dummy  = new Bitmap(1, 1);
                using var refGfx = Graphics.FromImage(dummy);
                IntPtr hdc = refGfx.GetHdc();
                try
                {
                    var mf = new Metafile(emfPath, hdc,
                        new Rectangle(0, 0, bmp.Width, bmp.Height),
                        MetafileFrameUnit.Pixel,
                        EmfType.EmfPlusDual);
                    using (var g = Graphics.FromImage(mf))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
                    }
                    mf.Dispose();
                }
                finally { refGfx.ReleaseHdc(hdc); }

                return File.Exists(emfPath) && new FileInfo(emfPath).Length > 0;
            }
            catch { return false; }
        }

        // ── Clipboard with CF_ENHMETAFILE ────────────────────────────────

        private static bool SetClipboardEmf(string emfPath)
        {
            IntPtr hemf = GetEnhMetaFile(emfPath);
            if (hemf == IntPtr.Zero) return false;
            bool ok = false;
            try
            {
                if (!OpenClipboard(IntPtr.Zero)) return false;
                try
                {
                    EmptyClipboard();
                    ok = SetClipboardData(CF_ENHMETAFILE, hemf) != IntPtr.Zero;
                    if (ok) hemf = IntPtr.Zero;
                }
                finally { CloseClipboard(); }
            }
            finally
            {
                if (hemf != IntPtr.Zero) DeleteEnhMetaFile(hemf);
            }
            return ok;
        }

        // ── AutoCAD connection ──────────────────────────────────────────

        private static dynamic GetOrLaunch(out bool launched)
        {
            launched = false;
            try
            {
                var obj = Compat.GetActiveComObject("AutoCAD.Application");
                if (obj != null) return obj;
            }
            catch { }

            foreach (var progId in new[]
            {
                "AutoCAD.Application.26",
                "AutoCAD.Application.25",
                "AutoCAD.Application.24",
                "AutoCAD.Application.23",
                "AutoCAD.Application.22",
                "AutoCAD.Application"
            })
            {
                try
                {
                    var t = Type.GetTypeFromProgID(progId);
                    if (t == null) continue;
                    dynamic app = Activator.CreateInstance(t);
                    if (app != null) { launched = true; return app; }
                }
                catch { }
            }
            return null;
        }
    }
}
