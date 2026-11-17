using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.ApplicationServices;
using JFTools.Helpers;

namespace JFTools
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ExportSheetsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                L.Init(commandData.Application.Application.Language == LanguageType.Brazilian_Portuguese);

                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                if (uiDoc == null)
                {
                    TaskDialog.Show("JFTools", L.NoDocOpen);
                    return Result.Cancelled;
                }

                var window = new Views.MainWindow(uiApp);
                var helper = new System.Windows.Interop.WindowInteropHelper(window);
                helper.Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("JFTools — " + L.AppErrorLabel, ex.ToString());
                return Result.Failed;
            }
        }
    }
}
