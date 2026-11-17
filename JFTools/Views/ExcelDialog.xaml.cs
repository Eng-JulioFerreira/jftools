using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JFTools.Helpers;
using Forms = System.Windows.Forms;

namespace JFTools.Views
{
    public partial class ExcelDialog : Window
    {
        private readonly UIApplication _uiApp;
        private List<Document> _docs;
        private List<ViewSchedule> _schedules = new List<ViewSchedule>();

        public ExcelDialog(UIApplication uiApp)
        {
            _uiApp = uiApp;
            InitializeComponent();
            LoadDocs();
        }

        private void LoadDocs()
        {
            _docs = new List<Document>();
            foreach (Document doc in _uiApp.Application.Documents)
            {
                if (doc.IsFamilyDocument) continue;
                try { if (doc.IsLinked) continue; } catch { }
                _docs.Add(doc);
            }

            var names = _docs.Select(d => d.Title ?? "(untitled)").ToList();
            comboExportDoc.ItemsSource = new List<string>(names);
            comboImportDoc.ItemsSource = new List<string>(names);

            if (_docs.Count > 0)
            {
                comboExportDoc.SelectedIndex = 0;
                comboImportDoc.SelectedIndex = 0;
            }
        }

        // ── EXPORT ────────────────────────────────────────────────────────────

        private void OnExportDocChanged(object sender, SelectionChangedEventArgs e)
        {
            listSchedules.Items.Clear();
            _schedules.Clear();

            int idx = comboExportDoc.SelectedIndex;
            if (idx < 0 || idx >= _docs.Count) return;

            try
            {
                _schedules = new FilteredElementCollector(_docs[idx])
                    .OfClass(typeof(ViewSchedule))
                    .Cast<ViewSchedule>()
                    .Where(s => !s.IsTemplate && !s.IsTitleblockRevisionSchedule)
                    .OrderBy(s => s.Name)
                    .ToList();

                foreach (var s in _schedules)
                    listSchedules.Items.Add(s.Name);
            }
            catch (Exception ex)
            {
                MessageBox.Show(L.MsgSchedListError + ex.Message,
                    "JFTools", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnScheduleSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Auto-suggest file name based on selected schedule
            if (listSchedules.SelectedIndex >= 0 && _schedules.Count > listSchedules.SelectedIndex)
            {
                string name = ExcelHelper.SanitizeFileName(_schedules[listSchedules.SelectedIndex].Name);
                string dir  = string.IsNullOrEmpty(txtExportPath.Text)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : Path.GetDirectoryName(txtExportPath.Text);
                txtExportPath.Text = Path.Combine(dir ?? "", name + ".xlsx");
            }
        }

        private void OnBrowseExport(object sender, RoutedEventArgs e)
        {
            string suggested = string.Empty;
            if (listSchedules.SelectedIndex >= 0 && _schedules.Count > listSchedules.SelectedIndex)
                suggested = ExcelHelper.SanitizeFileName(_schedules[listSchedules.SelectedIndex].Name) + ".xlsx";

            var dlg = new Forms.SaveFileDialog
            {
                Title      = L.SaveExcelTitle,
                Filter     = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                FileName   = suggested
            };
            if (!string.IsNullOrEmpty(txtExportPath.Text))
                dlg.InitialDirectory = Path.GetDirectoryName(txtExportPath.Text);

            if (dlg.ShowDialog() == Forms.DialogResult.OK)
                txtExportPath.Text = dlg.FileName;
        }

        private void OnExport(object sender, RoutedEventArgs e)
        {
            int schedIdx = listSchedules.SelectedIndex;
            if (schedIdx < 0 || schedIdx >= _schedules.Count)
            {
                MessageBox.Show(L.MsgSelectSchedule, "JFTools",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string filePath = txtExportPath.Text.Trim();
            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show(L.MsgSelectDestFile, "JFTools",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ExcelHelper.ExportScheduleForEditing(_schedules[schedIdx], filePath);
                MessageBox.Show(
                    L.MsgExcelExportSuccess(filePath),
                    L.TitleExcelExport, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(L.MsgExcelExportError + ex.Message,
                    "JFTools", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── IMPORT ────────────────────────────────────────────────────────────

        private void OnBrowseImport(object sender, RoutedEventArgs e)
        {
            var dlg = new Forms.OpenFileDialog
            {
                Title      = L.OpenExcelTitle,
                Filter     = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx"
            };
            if (dlg.ShowDialog() != Forms.DialogResult.OK) return;

            txtImportPath.Text = dlg.FileName;
            comboWorksheet.ItemsSource = null;
            txtResults.Text = string.Empty;

            try
            {
                var names = ExcelHelper.GetWorksheetNames(dlg.FileName);
                comboWorksheet.ItemsSource = names;
                if (names.Count > 0) comboWorksheet.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(L.MsgExcelReadError + ex.Message,
                    "JFTools", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnImport(object sender, RoutedEventArgs e)
        {
            string filePath = txtImportPath.Text.Trim();
            string wsName   = comboWorksheet.SelectedItem as string;
            int    docIdx   = comboImportDoc.SelectedIndex;

            if (string.IsNullOrEmpty(filePath))
            { MessageBox.Show(L.MsgSelectExcelFile, "JFTools", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (string.IsNullOrEmpty(wsName))
            { MessageBox.Show(L.MsgSelectWorksheet, "JFTools", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (docIdx < 0 || docIdx >= _docs.Count)
            { MessageBox.Show(L.MsgSelectTargetProj, "JFTools", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            try
            {
                var log = ExcelHelper.ImportParametersFromExcel(filePath, wsName, _docs[docIdx]);
                txtResults.Text = string.Join(Environment.NewLine, log);

                bool hasError = log.Any(line => line.StartsWith("Error") || line.StartsWith("Not found"));
                MessageBox.Show(log.Count > 0 ? log[0] : L.MsgImportDone,
                    L.TitleExcelImport,
                    MessageBoxButton.OK,
                    hasError ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(L.MsgExcelImportError + ex.Message,
                    "JFTools", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
