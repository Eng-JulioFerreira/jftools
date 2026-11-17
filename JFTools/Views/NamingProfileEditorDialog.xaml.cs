using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.Revit.DB;
using JFTools.Helpers;
using WpfGrid = System.Windows.Controls.Grid;

namespace JFTools.Views
{
    public class AvailableParam
    {
        public string Name { get; set; }
        public string Sample { get; set; }
    }

    public class PatternToken : INotifyPropertyChanged
    {
        private string _display;
        public string Display { get => _display; set { _display = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Display))); } }
        public string Raw { get; set; }
        public bool IsParam { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public partial class NamingProfileEditorDialog : Window
    {
        private readonly Document _doc;
        private readonly ViewSheet _sampleSheet;
        private Dictionary<string, string> _profiles;
        private readonly ObservableCollection<PatternToken> _tokens = new ObservableCollection<PatternToken>();
        private bool _loading;

        public Dictionary<string, string> ResultProfiles { get; private set; }

        public NamingProfileEditorDialog(Document doc, Dictionary<string, string> profiles)
        {
            InitializeComponent();
            _doc = doc;
            _profiles = new Dictionary<string, string>(profiles);

            _sampleSheet = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Sheets)
                .WhereElementIsNotElementType()
                .Cast<ViewSheet>()
                .FirstOrDefault();

            LoadAvailableParams();
            RefreshProfileCombo();

            listPattern.ItemsSource = _tokens;
            _tokens.CollectionChanged += (s, e) => { if (!_loading) SyncPatternToProfile(); };
        }

        // ── Available params ─────────────────────────────────────
        private void LoadAvailableParams()
        {
            // Sheet params
            var stdSheet = new[]
            {
                "Sheet Number", "Sheet Name", "Sheet Issue Date",
                "Drawn By", "Checked By", "Approved By",
                "Current Revision", "Current Revision Date", "Current Revision Description"
            };
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sheetList = new List<AvailableParam>();
            foreach (var n in stdSheet)
            {
                seen.Add(n);
                sheetList.Add(new AvailableParam { Name = n, Sample = GetSheetSample(n) });
            }
            if (_sampleSheet != null)
            {
                foreach (Parameter p in _sampleSheet.Parameters)
                {
                    string name = p.Definition?.Name;
                    if (string.IsNullOrWhiteSpace(name) || name.StartsWith("_") || seen.Contains(name)) continue;
                    seen.Add(name);
                    sheetList.Add(new AvailableParam { Name = name, Sample = p.AsString() ?? p.AsValueString() ?? "" });
                }
            }
            listSheetParams.ItemsSource = sheetList.OrderBy(x => x.Name).ToList();

            // Project params
            var stdProj = new[]
            {
                "Project Name", "Project Number", "Project Address",
                "Client Name", "Building Name", "Author",
                "Organization Name", "Organization Description",
                "Project Status", "Project Issue Date"
            };
            var seenProj = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var projList = new List<AvailableParam>();
            foreach (var n in stdProj)
            {
                seenProj.Add(n);
                projList.Add(new AvailableParam { Name = n, Sample = GetProjectSample(n) });
            }
            var pi = _doc.ProjectInformation;
            if (pi != null)
            {
                foreach (Parameter p in pi.Parameters)
                {
                    string name = p.Definition?.Name;
                    if (string.IsNullOrWhiteSpace(name) || name.StartsWith("_") || seenProj.Contains(name)) continue;
                    seenProj.Add(name);
                    projList.Add(new AvailableParam { Name = name, Sample = p.AsString() ?? p.AsValueString() ?? "" });
                }
            }
            listProjectParams.ItemsSource = projList.OrderBy(x => x.Name).ToList();
        }

        private string GetSheetSample(string name)
        {
            if (_sampleSheet == null) return "";
            return NamingHelper.GetParamValue(_sampleSheet, name, _doc);
        }

        private string GetProjectSample(string name)
        {
            var pi = _doc.ProjectInformation;
            if (pi == null) return "";
            var p = pi.LookupParameter(name);
            return p?.AsString() ?? p?.AsValueString() ?? "";
        }

        // ── Profile combo ────────────────────────────────────────
        private void RefreshProfileCombo(string selectName = null)
        {
            comboProfile.ItemsSource = null;
            comboProfile.ItemsSource = _profiles.Keys.ToList();
            if (selectName != null && _profiles.ContainsKey(selectName))
                comboProfile.SelectedItem = selectName;
            else if (_profiles.Count > 0)
                comboProfile.SelectedIndex = 0;
            UpdateDeleteEnabled();
        }

        private void OnProfileSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string name = comboProfile.SelectedItem as string;
            if (name == null) { _tokens.Clear(); return; }
            LoadPatternFromString(_profiles[name]);
        }

        private void UpdateDeleteEnabled()
        {
            btnDelete.IsEnabled = _profiles.Count > 1;
        }

        // ── Pattern load/save ────────────────────────────────────
        private void LoadPatternFromString(string pattern)
        {
            _loading = true;
            _tokens.Clear();
            if (!string.IsNullOrEmpty(pattern))
            {
                var literal = new StringBuilder();
                bool reading = false;
                var token = new StringBuilder();
                foreach (char c in pattern)
                {
                    if (c == '{')
                    {
                        if (literal.Length > 0) { FlushLiteral(literal.ToString()); literal.Clear(); }
                        reading = true; token.Clear();
                    }
                    else if (c == '}' && reading)
                    {
                        reading = false;
                        string pName = token.ToString();
                        _tokens.Add(new PatternToken { Display = pName, Raw = "{" + pName + "}", IsParam = true });
                    }
                    else if (reading) token.Append(c);
                    else literal.Append(c);
                }
                if (literal.Length > 0) FlushLiteral(literal.ToString());
            }
            _loading = false;
            UpdatePatternDisplay();
        }

        private void FlushLiteral(string text)
        {
            _tokens.Add(new PatternToken { Display = text, Raw = text, IsParam = false });
        }

        private string BuildPattern() => string.Concat(_tokens.Select(t => t.Raw));

        private void SyncPatternToProfile()
        {
            string name = comboProfile.SelectedItem as string;
            if (name != null) _profiles[name] = BuildPattern();
            UpdatePatternDisplay();
        }

        private void UpdatePatternDisplay()
        {
            string pattern = BuildPattern();
            txtPatternDisplay.Text = pattern;
            if (_sampleSheet != null && !string.IsNullOrEmpty(pattern))
            {
                var tmp = new Dictionary<string, string> { { "__p__", pattern } };
                txtPreview.Text = NamingHelper.GenerateName(_sampleSheet, "__p__", tmp, _doc);
            }
            else
            {
                txtPreview.Text = string.IsNullOrEmpty(pattern) ? "—" : pattern;
            }
        }

        // ── Add param ─────────────────────────────────────────────
        private AvailableParam GetSelectedParam()
        {
            if (tabParams.SelectedIndex == 0) return listSheetParams.SelectedItem as AvailableParam;
            return listProjectParams.SelectedItem as AvailableParam;
        }

        private void AddParamToken(AvailableParam p)
        {
            if (p == null) return;
            var tok = new PatternToken { Display = p.Name, Raw = "{" + p.Name + "}", IsParam = true };
            InsertToken(tok);
        }

        private void InsertToken(PatternToken tok)
        {
            int idx = listPattern.SelectedIndex;
            if (idx >= 0 && idx < _tokens.Count) _tokens.Insert(idx + 1, tok);
            else _tokens.Add(tok);
            listPattern.SelectedItem = tok;
        }

        private void OnAddParam(object sender, RoutedEventArgs e) => AddParamToken(GetSelectedParam());

        private void OnParamDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((sender as ListBox)?.SelectedItem is AvailableParam p) AddParamToken(p);
        }

        // ── Separators ────────────────────────────────────────────
        private void AddSepToken(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            InsertToken(new PatternToken { Display = text, Raw = text, IsParam = false });
        }

        private void OnAddSep(object sender, RoutedEventArgs e) => AddSepToken((sender as Button)?.Tag as string);

        private void OnAddCustomText(object sender, RoutedEventArgs e)
        {
            string t = txtCustomSep.Text;
            if (!string.IsNullOrEmpty(t)) { AddSepToken(t); txtCustomSep.Clear(); }
        }

        // ── Move / Remove ─────────────────────────────────────────
        private void OnMoveUp(object sender, RoutedEventArgs e)
        {
            int i = listPattern.SelectedIndex;
            if (i <= 0) return;
            _tokens.Move(i, i - 1);
            listPattern.SelectedIndex = i - 1;
        }

        private void OnMoveDown(object sender, RoutedEventArgs e)
        {
            int i = listPattern.SelectedIndex;
            if (i < 0 || i >= _tokens.Count - 1) return;
            _tokens.Move(i, i + 1);
            listPattern.SelectedIndex = i + 1;
        }

        private void OnRemoveToken(object sender, RoutedEventArgs e)
        {
            int i = listPattern.SelectedIndex;
            if (i < 0) return;
            _tokens.RemoveAt(i);
            if (_tokens.Count > 0) listPattern.SelectedIndex = Math.Min(i, _tokens.Count - 1);
        }

        private void OnPatternKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete) OnRemoveToken(null, null);
        }

        private void OnPatternSelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // ── Profile CRUD ──────────────────────────────────────────
        private void OnNewProfile(object sender, RoutedEventArgs e)
        {
            string name = PromptText(L.TitleNewProfile, L.PromptProfileName, "", this);
            if (string.IsNullOrWhiteSpace(name)) return;
            if (_profiles.ContainsKey(name)) { MessageBox.Show(L.MsgProfileExists, L.TitleNewProfile); return; }
            _profiles[name] = "";
            RefreshProfileCombo(name);
        }

        private void OnRenameProfile(object sender, RoutedEventArgs e)
        {
            string old = comboProfile.SelectedItem as string;
            if (old == null) return;
            string newName = PromptText(L.TitleRenameProfile, L.PromptNewName, old, this);
            if (string.IsNullOrWhiteSpace(newName) || newName == old) return;
            if (_profiles.ContainsKey(newName)) { MessageBox.Show(L.MsgProfileExists, L.TitleRenameProfile); return; }
            string pattern = _profiles[old];
            // Preserve order: rebuild dict
            var newDict = new Dictionary<string, string>();
            foreach (var kv in _profiles) newDict[kv.Key == old ? newName : kv.Key] = kv.Value;
            _profiles = newDict;
            RefreshProfileCombo(newName);
        }

        private void OnDuplicateProfile(object sender, RoutedEventArgs e)
        {
            string source = comboProfile.SelectedItem as string;
            if (source == null) return;
            string newName = PromptText(L.TitleDuplProfile, L.PromptDuplName, source + L.CopySuffix, this);
            if (string.IsNullOrWhiteSpace(newName)) return;
            if (_profiles.ContainsKey(newName)) { MessageBox.Show(L.MsgProfileExists, L.TitleDuplProfile); return; }
            _profiles[newName] = _profiles[source];
            RefreshProfileCombo(newName);
        }

        private void OnDeleteProfile(object sender, RoutedEventArgs e)
        {
            string name = comboProfile.SelectedItem as string;
            if (name == null || _profiles.Count <= 1) return;
            if (MessageBox.Show(L.MsgDeleteProfile(name), L.TitleDelProfile, MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            _profiles.Remove(name);
            RefreshProfileCombo();
        }

        // ── Save / Cancel ─────────────────────────────────────────
        private void OnSave(object sender, RoutedEventArgs e)
        {
            ResultProfiles = _profiles;
            NamingHelper.SaveProfiles(NamingHelper.GetProfilesPath(), _profiles);
            DialogResult = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e) => Close();

        // ── Prompt helper ─────────────────────────────────────────
        private static string PromptText(string title, string label, string defaultValue, Window owner)
        {
            string result = null;
            var dlg = new Window
            {
                Title = title, Width = 360,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 12, ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false
            };
            var g = new WpfGrid { Margin = new Thickness(16) };
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var lbl = new TextBlock { Text = label };
            WpfGrid.SetRow(lbl, 0); g.Children.Add(lbl);
            var tb = new TextBox { Height = 28, Text = defaultValue, VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(4, 0, 4, 0) };
            WpfGrid.SetRow(tb, 2); g.Children.Add(tb);
            var btnOk = new Button { Content = "OK", Width = 80, Height = 28, IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right };
            btnOk.Click += (s, ev) => { result = tb.Text; dlg.Close(); };
            WpfGrid.SetRow(btnOk, 4); g.Children.Add(btnOk);
            dlg.Content = g;
            dlg.Loaded += (s, ev) => { tb.SelectAll(); tb.Focus(); };
            dlg.ShowDialog();
            return result;
        }
    }
}
