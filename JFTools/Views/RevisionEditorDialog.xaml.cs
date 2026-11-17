using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using JFTools.Helpers;
using JFTools.Models;

namespace JFTools.Views
{
    internal class RevisionRow
    {
        public string   Num      { get; set; }
        public string   Date     { get; set; }
        public string   Desc     { get; set; }
        public string   IssuedBy { get; set; }
        public string   IssuedTo { get; set; }
        public string   DocTitle { get; set; }
        public string   Sheets   { get; set; }
        public Document DocRef   { get; set; }
        public ElementId RevId   { get; set; }
    }

    public partial class RevisionEditorDialog : Window
    {
        private readonly List<SheetItem> _checkedItems;
        private readonly List<Document>  _targetDocs;
        private List<RevisionRow>        _allRevRows = new List<RevisionRow>();
        private string                   _mode;       // "add" | "edit"
        private RevisionRow              _editingRow;

        public RevisionEditorDialog(List<SheetItem> checkedItems)
        {
            InitializeComponent();
            _checkedItems = checkedItems;

            // Build unique doc list in insertion order
            var seen = new HashSet<string>();
            _targetDocs = new List<Document>();
            foreach (var item in checkedItems)
            {
                string key = item.DocRef.PathName ?? item.DocRef.Title ?? "";
                if (!seen.Contains(key))
                {
                    seen.Add(key);
                    _targetDocs.Add(item.DocRef);
                }
            }

            // Populate project filter combo
            cboProject.Items.Add(L.AllProjects);
            foreach (var d in _targetDocs)
                cboProject.Items.Add(NamingHelper.DocShortTitle(d));
            cboProject.SelectedIndex = 0;

            // Wire event after SelectedIndex to avoid premature reload
            cboProject.SelectionChanged += OnProjectFilterChanged;

            ReloadList();
        }

        // ── Helpers ─────────────────────────────────────────────

        private List<Document> GetFilteredDocs()
        {
            string sel = cboProject.SelectedItem as string ?? L.AllProjects;
            if (sel == L.AllProjects) return _targetDocs;
            return _targetDocs.Where(d => NamingHelper.DocShortTitle(d) == sel).ToList();
        }

        private List<RevisionRow> ReadRevisions(List<Document> docs)
        {
            var rows    = new List<RevisionRow>();
            var seenKeys = new HashSet<string>();

            foreach (var d in docs)
            {
                try
                {
                    var revisions = new FilteredElementCollector(d)
                        .OfClass(typeof(Revision))
                        .Cast<Revision>();

                    foreach (var rev in revisions)
                    {
                        string key = (d.PathName ?? d.Title ?? "") + "|" + rev.Id.IdValue();
                        if (seenKeys.Contains(key)) continue;
                        seenKeys.Add(key);

                        var sheetsWithRev = new List<string>();
                        foreach (var si in _checkedItems)
                        {
                            if (si.DocRef != d) continue;
                            try
                            {
                                if (si.Sheet != null && si.Sheet.IsValidObject && si.Sheet.GetAllRevisionIds().Contains(rev.Id))
                                    sheetsWithRev.Add(si.Number);
                            }
                            catch { }
                        }

                        rows.Add(new RevisionRow
                        {
                            Num      = rev.SequenceNumber.ToString(),
                            Date     = rev.RevisionDate    ?? "",
                            Desc     = rev.Description     ?? "",
                            IssuedBy = rev.IssuedBy        ?? "",
                            IssuedTo = rev.IssuedTo        ?? "",
                            DocTitle = NamingHelper.DocShortTitle(d),
                            Sheets   = string.Join(", ", sheetsWithRev.OrderBy(s => s)),
                            DocRef   = d,
                            RevId    = rev.Id,
                        });
                    }
                }
                catch { }
            }

            rows.Sort((a, b) =>
            {
                int c = string.Compare(a.DocTitle, b.DocTitle, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                int na = int.TryParse(a.Num, out int _na) ? _na : 0;
                int nb = int.TryParse(b.Num, out int _nb) ? _nb : 0;
                return na.CompareTo(nb);
            });

            return rows;
        }

        private void ReloadList()
        {
            _allRevRows = ReadRevisions(_targetDocs);
            var filteredDocSet = new HashSet<Document>(GetFilteredDocs());

            lvRevs.Items.Clear();
            foreach (var row in _allRevRows)
                if (filteredDocSet.Contains(row.DocRef))
                    lvRevs.Items.Add(row);
        }

        // ── Form helpers ─────────────────────────────────────────

        private void ShowForm(string mode, RevisionRow prefill = null)
        {
            _mode       = mode;
            _editingRow = prefill;

            fDate.Text     = prefill?.Date     ?? DateTime.Today.ToString("dd/MM/yyyy");
            fDesc.Text     = prefill?.Desc     ?? "";
            fIssuedBy.Text = prefill?.IssuedBy ?? "";
            fIssuedTo.Text = prefill?.IssuedTo ?? "";

            pnlAddOpts.Visibility = mode == "add" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            pnlForm.Visibility    = System.Windows.Visibility.Visible;
        }

        private void HideForm(object sender = null, RoutedEventArgs e = null)
        {
            pnlForm.Visibility = System.Windows.Visibility.Collapsed;
            _mode       = null;
            _editingRow = null;
        }

        // ── Event handlers ───────────────────────────────────────

        private void OnProjectFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            HideForm();
            ReloadList();
        }

        private void OnRevAdd(object sender, RoutedEventArgs e) => ShowForm("add");

        private void OnRevEdit(object sender, RoutedEventArgs e)
        {
            var sel = lvRevs.SelectedItem as RevisionRow;
            if (sel == null) { MessageBox.Show(L.MsgSelectRevToEdit, L.RevTitle); return; }
            ShowForm("edit", sel);
        }

        private void OnRevRemove(object sender, RoutedEventArgs e)
        {
            var sel = lvRevs.SelectedItem as RevisionRow;
            if (sel == null) { MessageBox.Show(L.MsgSelectRevToRemove, L.RevTitle); return; }

            try
            {
                using (var tx = new Transaction(sel.DocRef, "JFTools Remove Revision"))
                {
                    tx.Start();
                    try   { sel.DocRef.Delete(sel.RevId); tx.Commit(); }
                    catch (Exception ex) { tx.RollBack(); MessageBox.Show(L.MsgRevError + ex.Message, L.RevTitle); }
                }
            }
            catch (Exception ex) { MessageBox.Show(L.MsgRevError + ex.Message, L.RevTitle); }

            ReloadList();
        }

        private void OnFormSave(object sender, RoutedEventArgs e)
        {
            string dateVal = fDate.Text.Trim();
            string descVal = fDesc.Text.Trim();
            string ibyVal  = fIssuedBy.Text.Trim();
            string itoVal  = fIssuedTo.Text.Trim();
            var errors = new List<string>();

            if (_mode == "add")
            {
                var createIn = rFiltered.IsChecked == true ? GetFilteredDocs() : _targetDocs;
                bool applyToSheets = chkApply.IsChecked == true;

                foreach (var rdoc in createIn)
                {
                    Revision newRev = null;
                    try
                    {
                        using (var tx = new Transaction(rdoc, "JFTools Add Revision"))
                        {
                            tx.Start();
                            try
                            {
                                newRev = Revision.Create(rdoc);
                                if (!string.IsNullOrEmpty(dateVal)) try { newRev.RevisionDate = dateVal; } catch { }
                                if (!string.IsNullOrEmpty(descVal)) try { newRev.Description  = descVal; } catch { }
                                if (!string.IsNullOrEmpty(ibyVal))  try { newRev.IssuedBy     = ibyVal;  } catch { }
                                if (!string.IsNullOrEmpty(itoVal))  try { newRev.IssuedTo     = itoVal;  } catch { }
                                tx.Commit();
                            }
                            catch (Exception ex) { tx.RollBack(); errors.Add(ex.Message); newRev = null; }
                        }
                    }
                    catch (Exception ex) { errors.Add(ex.Message); newRev = null; }

                    if (newRev != null && applyToSheets)
                    {
                        var sheetsToApply = _checkedItems
                            .Where(si => si.DocRef == rdoc)
                            .Select(si => si.Sheet)
                            .ToList();

                        if (sheetsToApply.Count > 0)
                        {
                            try
                            {
                                using (var tx2 = new Transaction(rdoc, "JFTools Apply Revision"))
                                {
                                    tx2.Start();
                                    try
                                    {
                                        foreach (var sh in sheetsToApply)
                                        {
                                            var cur = sh.GetAdditionalRevisionIds().ToList();
                                            if (!cur.Contains(newRev.Id))
                                            {
                                                cur.Add(newRev.Id);
                                                sh.SetAdditionalRevisionIds(cur);
                                            }
                                        }
                                        tx2.Commit();
                                    }
                                    catch (Exception ex) { tx2.RollBack(); errors.Add(ex.Message); }
                                }
                            }
                            catch (Exception ex) { errors.Add(ex.Message); }
                        }
                    }
                }
            }
            else if (_mode == "edit" && _editingRow != null)
            {
                var rdoc  = _editingRow.DocRef;
                var revId = _editingRow.RevId;
                try
                {
                    using (var tx = new Transaction(rdoc, "JFTools Edit Revision"))
                    {
                        tx.Start();
                        try
                        {
                            var rev = rdoc.GetElement(revId) as Revision;
                            if (rev != null)
                            {
                                if (!string.IsNullOrEmpty(dateVal)) try { rev.RevisionDate = dateVal; } catch { }
                                if (!string.IsNullOrEmpty(descVal)) try { rev.Description  = descVal; } catch { }
                                try { rev.IssuedBy = ibyVal; } catch { }
                                try { rev.IssuedTo = itoVal; } catch { }
                            }
                            tx.Commit();
                        }
                        catch (Exception ex) { tx.RollBack(); errors.Add(ex.Message); }
                    }
                }
                catch (Exception ex) { errors.Add(ex.Message); }
            }

            if (errors.Count > 0)
                MessageBox.Show(L.MsgRevErrors + string.Join("\n", errors), L.RevTitle);

            ReloadList();
            HideForm();
        }

        private void OnFormCancel(object sender, RoutedEventArgs e) => HideForm();
        private void OnClose(object sender, RoutedEventArgs e)      => Close();
    }
}
