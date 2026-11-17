using System;
using System.Runtime.InteropServices;
using Autodesk.Revit.DB;

namespace JFTools.Helpers
{
    /// <summary>
    /// Differences between the .NET Framework 4.8 build (Revit 2022–2024)
    /// and the .NET 8/10 builds (Revit 2025+).
    /// </summary>
    internal static class Compat
    {
        /// <summary>
        /// Numeric value of an ElementId. ElementId.IntegerValue was removed in the
        /// Revit 2026 API; ElementId.Value (long) exists from Revit 2024 on.
        /// </summary>
        internal static long IdValue(this ElementId id)
        {
#if NETFRAMEWORK
            return id.IntegerValue;
#else
            return id.Value;
#endif
        }

        // Marshal.GetActiveObject does not exist on .NET Core / .NET 5+,
        // so the running-object lookup is done through OLE directly (works on both).
        [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
        private static extern int CLSIDFromProgID(string progId, out Guid clsid);

        [DllImport("oleaut32.dll")]
        private static extern int GetActiveObject(ref Guid clsid, IntPtr reserved,
            [MarshalAs(UnmanagedType.IUnknown)] out object obj);

        /// <summary>Returns the running COM object for a ProgID, or null if none is running.</summary>
        internal static object GetActiveComObject(string progId)
        {
            if (CLSIDFromProgID(progId, out Guid clsid) < 0) return null;
            return GetActiveObject(ref clsid, IntPtr.Zero, out object obj) < 0 ? null : obj;
        }
    }
}
