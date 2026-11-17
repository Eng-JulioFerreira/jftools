using System;
using System.Reflection;
using System.Threading;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using JFTools.Helpers;

namespace JFTools
{
    public class JFToolsApp : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            try
            {
                bool isPt = Thread.CurrentThread.CurrentUICulture
                    .TwoLetterISOLanguageName.Equals("pt", StringComparison.OrdinalIgnoreCase);
                L.Init(isPt);

                string tabName = "JFTools";
                app.CreateRibbonTab(tabName);

                var panel = app.CreateRibbonPanel(tabName, L.RibbonPanel);
                string dllPath = Assembly.GetExecutingAssembly().Location;

                var btnData = new PushButtonData(
                    "ExportSheets",
                    "Export",
                    dllPath,
                    "JFTools.ExportSheetsCommand"
                );
                btnData.ToolTip = L.BtnExportTooltip;
                btnData.LargeImage = LoadEmbeddedImage("JFTools.Resources.impressora.png");

                panel.AddItem(btnData);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("JFTools", L.AppLoadError + ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;

        private BitmapImage LoadEmbeddedImage(string resourceName)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = stream;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
            }
            catch { return null; }
        }
    }
}
