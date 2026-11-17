using System;
using System.Diagnostics;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JFTools.Helpers;
using JFTools.Views;

namespace JFTools
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ExcelCommand : IExternalCommand
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

                var window = new ExcelDialog(uiApp);
                var helper = new WindowInteropHelper(window);
                helper.Owner = Process.GetCurrentProcess().MainWindowHandle;
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
