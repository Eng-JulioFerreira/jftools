using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JFTools.Helpers;
using JFTools.Models;
using Forms = System.Windows.Forms;
using WpfGrid = System.Windows.Controls.Grid;
using WpfBinding = System.Windows.Data.Binding;

namespace JFTools.Views
{
    public partial class MainWindow : Window
    {
        private readonly UIApplication _uiApp;
        private readonly List<Document> _selectedDocs = new List<Document>();
        private List<SheetItem> _allSheets = new List<SheetItem>();
        private Dictionary<string, string> _profiles;
        private Dictionary<string, ExportSettingsProfile> _exportProfiles;
        private bool _applyingExportProfile;
        private Dictionary<int, string> _dwgSetupPerDoc = new Dictionary<int, string>();
        private DwgExportConfig _inSessionConfig = new DwgExportConfig(); // saved In-Session state
        private string _sortCol;
        private bool _sortAsc = true;
        private bool _exportCancelled;
        private string _selectedFormat = "PDF";

        private static readonly string[] _ifcVersionEnum    = { "IFC2x3CV2","IFC2x3","IFC4RV","IFC4DTV","IFC4","IFCCOBIE","IFC2x3FM","IFC2x3BFM","IFC2x2","IFCBCA","IFC4x3" };
        private static readonly string[] _ifcVersionDisplay = { "IFC 2x3 Coordination View 2.0","IFC 2x3 Coordination View","IFC 4 Reference View","IFC 4 Design Transfer View","IFC 4","IFC 2x3 COBie 2.4","IFC 2x3 Extended FM Handover View","IFC 2x3 Basic FM Handover View","IFC 2x2 Coordination View","IFC BCA","IFC 4x3" };
        private static readonly string[] _ifcFileTypeEnum    = { "Ifc","IfcXML","IfcZIP","IfcXMLZIP" };
        private static readonly string[] _ifcFileTypeDisplay = { "IFC (.ifc)","IFC XML (.ifcxml)","IFC ZIP (.ifczip)","IFC XML ZIP (.ifcxmlzip)" };
        private static readonly string[] _ifcSitePlacementEnum    = { "Shared","Site","Project","Internal","ProjectInTN","InternalInTN" };
        private static readonly string[] _ifcSitePlacementDisplay = { "Shared coordinates","Site survey point","Project base point","Internal origin","Project base point (True North)","Internal origin (True North)" };
        private static readonly string[] _ifcExchangeReqEnum    = { "NotDefined","Architecture","BuildingService","Structural" };
        private static readonly string[] _ifcExchangeReqDisplay = { "Not Defined","Architecture","Building Service","Structural" };

        public MainWindow(UIApplication uiApp)
        {
            _sortCol = L.ColNumber;
            InitializeComponent();
            _uiApp = uiApp;

            var doc = uiApp.ActiveUIDocument.Document;
            _selectedDocs.Add(doc);

            // Populate combos
            comboQuality.ItemsSource = L.QualityItems;
            comboQuality.SelectedIndex = 2;
            comboColorMode.ItemsSource = L.ColorItems;
            comboColorMode.SelectedIndex = 0;

            comboIfcVersion.ItemsSource = _ifcVersionDisplay;
            comboIfcVersion.SelectedIndex = 0;
            comboIfcFileType.ItemsSource = _ifcFileTypeDisplay;
            comboIfcFileType.SelectedIndex = 0;
            comboIfcSpaceBoundaries.ItemsSource = L.IfcSpaceBoundaryItems;
            comboIfcSpaceBoundaries.SelectedIndex = 0;
            comboIfcSitePlacement.ItemsSource = _ifcSitePlacementDisplay;
            comboIfcSitePlacement.SelectedIndex = 0;
            comboIfcExchangeReq.ItemsSource = _ifcExchangeReqDisplay;
            comboIfcExchangeReq.SelectedIndex = 0;
            comboIfcLinkedFiles.ItemsSource = L.IfcLinkedItems;
            comboIfcLinkedFiles.SelectedIndex = 0;

            comboNwcCoords.ItemsSource = L.NwcCoordItems;
            comboNwcCoords.SelectedIndex = 0;
            comboNwcParams.ItemsSource = L.NwcParamItems;
            comboNwcParams.SelectedIndex = 0;

            // DWG settings – combos (version, units, ACA objects, layer overrides)
            comboDwgVersion.ItemsSource       = L.DwgVersionItems;
            comboDwgUnits.ItemsSource         = L.DwgUnitsItems;
            comboDwgACA.ItemsSource           = L.DwgACAItems;
            comboDwgPropOverrides.ItemsSource = L.DwgPropOverridesItems;
            LoadDwgConfig(new DwgExportConfig());   // apply defaults

            _profiles = NamingHelper.LoadProfiles(NamingHelper.GetProfilesPath());
            RefreshProfileCombo();
            _exportProfiles = ExportSettingsHelper.LoadProfiles(ExportSettingsHelper.GetProfilesPath());
            RefreshExportProfileCombo();
            RefreshProjectChips();
            RefreshSheetList();
            UpdateFormatGroups();
        }

        // ── SHEETS ──────────────────────────────────────────────
        private void RefreshSheetList(bool keepChecked = false)
        {
            var checkedSet = new HashSet<string>();
            if (keepChecked)
                foreach (var s in _allSheets.Where(s => s.IsChecked))
                    checkedSet.Add(s.DocRef.GetHashCode() + "|" + s.Number);

            _allSheets.Clear();
            foreach (var d in _selectedDocs)
            {
                if (d == null || !d.IsValidObject) continue;
                string title = NamingHelper.DocShortTitle(d);
                try
                {
                    var sheets = new FilteredElementCollector(d)
                        .OfCategory(BuiltInCategory.OST_Sheets)
                        .WhereElementIsNotElementType()
                        .Cast<ViewSheet>()
                        .Where(s => s != null && s.IsValidObject)
                        .OrderBy(s => s.SheetNumber);
                    foreach (var s in sheets)
                    {
                        var item = new SheetItem(s, d, title);
                        if (checkedSet.Contains(d.GetHashCode() + "|" + item.Number))
                            item.IsChecked = true;
                        _allSheets.Add(item);
                    }
                }
                catch { }
            }
            ApplyFilterAndSort();
            UpdateCount();
            UpdatePreview();
        }

        private void ApplyFilterAndSort()
        {
            string query = txtFilter.Text.Trim().ToLower();
            string projFilter = comboProjectFilter.SelectedItem as string ?? L.AllProjects;

            var visible = _allSheets.Where(i =>
            {
                if (!string.IsNullOrEmpty(query) &&
                    !i.Number.ToLower().Contains(query) &&
                    !i.Name.ToLower().Contains(query) &&
                    !i.DocTitle.ToLower().Contains(query))
                    return false;
                if (projFilter != L.AllProjects && i.DocTitle != projFilter)
                    return false;
                return true;
            });

            visible = _sortAsc
                ? visible.OrderBy(i => GetSortValue(i))
                : visible.OrderByDescending(i => GetSortValue(i));

            sheetList.ItemsSource = visible.ToList();
        }

        private string GetSortValue(SheetItem i)
        {
            if (_sortCol == L.ColSheetName)   return i.Name;
            if (_sortCol == L.ColProject)     return i.DocTitle;
            if (_sortCol == L.ColRev)         return i.Revision;
            if (_sortCol == L.ColRevDate)     return i.RevisionDate;
            if (_sortCol == L.ColDescription) return i.RevisionDescription;
            return i.Number;
        }

        private void UpdateCount()
        {
            lblCount.Text = _allSheets.Count(s => s.IsChecked).ToString();
        }

        // ── EVENTS ──────────────────────────────────────────────
        private void OnFilterChanged(object s, TextChangedEventArgs e)
        {
            if (sheetList == null) return;
            txtFilterPlaceholder.Visibility = string.IsNullOrEmpty(txtFilter.Text)
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
            ApplyFilterAndSort();
        }
        private void OnProjectFilterChanged(object s, SelectionChangedEventArgs e) { if (sheetList != null) ApplyFilterAndSort(); }

        private void OnColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader header && header.Content is string name)
            {
                name = name.Replace(" ▲", "").Replace(" ▼", "").Trim();
                if (name == _sortCol) _sortAsc = !_sortAsc;
                else { _sortCol = name; _sortAsc = true; }
                ApplyFilterAndSort();
            }
        }

        private void OnSheetCheckedChanged(object s, RoutedEventArgs e) { if (lblCount != null) { UpdateCount(); UpdatePreview(); } }
        private void OnSelectAll(object s, RoutedEventArgs e)
        {
            foreach (SheetItem i in sheetList.ItemsSource) i.IsChecked = true;
            sheetList.Items.Refresh();
            UpdateCount(); UpdatePreview();
        }
        private void OnDeselectAll(object s, RoutedEventArgs e)
        {
            foreach (SheetItem i in sheetList.ItemsSource) i.IsChecked = false;
            sheetList.Items.Refresh();
            UpdateCount(); UpdatePreview();
        }

        // ── PROJECTS ────────────────────────────────────────────
        private void RefreshProjectChips()
        {
            lstProjects.ItemsSource = _selectedDocs.Select(d => NamingHelper.DocShortTitle(d)).ToList();
            btnRemoveProject.IsEnabled = _selectedDocs.Count > 1;
            RefreshProjectFilterCombo();
            RefreshDwgSetups();
            bool multi = _selectedDocs.Count > 1;
            if (!multi) _dwgSetupPerDoc.Clear();

            comboDWG.IsEnabled         = !multi;
            btnDWGPerProject.IsEnabled =  multi;
            if (pnlDwgSettingsArea != null)
                pnlDwgSettingsArea.IsEnabled = !multi;
        }

        private void RefreshProjectFilterCombo()
        {
            var items = new List<string> { L.AllProjects };
            items.AddRange(_selectedDocs.Select(d => NamingHelper.DocShortTitle(d)));
            string prev = comboProjectFilter.SelectedItem as string;
            comboProjectFilter.ItemsSource = items;
            comboProjectFilter.SelectedItem = items.Contains(prev) ? prev : L.AllProjects;
        }

        private void OnAddProject(object s, RoutedEventArgs e)
        {
            var existing = new HashSet<string>(_selectedDocs.Where(d => !string.IsNullOrEmpty(d.PathName)).Select(d => d.PathName));
            var available = new List<Document>();
            foreach (Document d in _uiApp.Application.Documents)
            {
                try
                {
                    if (!d.IsFamilyDocument && !d.IsLinked && !string.IsNullOrEmpty(d.PathName) && !existing.Contains(d.PathName))
                        available.Add(d);
                }
                catch { }
            }
            if (available.Count == 0)
            {
                MessageBox.Show(L.MsgNoOtherProj, L.TitleAddProject);
                return;
            }

            var dlg = new ProjectPickerDialog(available) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                foreach (var d in dlg.SelectedDocs)
                    if (!existing.Contains(d.PathName))
                        _selectedDocs.Add(d);
                RefreshProjectChips();
                RefreshDwgSetups();
                RefreshSheetList(true);
            }
        }

        private void OnRemoveProject(object s, RoutedEventArgs e)
        {
            if (_selectedDocs.Count <= 1) return;
            var removable = _selectedDocs.Skip(1).ToList();
            var dlg = new ProjectPickerDialog(removable, true) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                foreach (var d in dlg.SelectedDocs)
                    _selectedDocs.Remove(d);
                RefreshProjectChips();
                RefreshDwgSetups();
                RefreshSheetList(true);
            }
        }

        // ── PROFILES ────────────────────────────────────────────
        private void RefreshProfileCombo()
        {
            comboProfile.ItemsSource = _profiles.Keys.ToList();
            if (_profiles.Count > 0) comboProfile.SelectedIndex = 0;
        }

        private void OnProfileChanged(object s, SelectionChangedEventArgs e) { if (txtPreview != null) UpdatePreview(); }
        private void OnRefreshProfiles(object s, RoutedEventArgs e)
        {
            _profiles = NamingHelper.LoadProfiles(NamingHelper.GetProfilesPath());
            RefreshProfileCombo();
            UpdatePreview();
        }
        private void OnEditProfiles(object s, RoutedEventArgs e)
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            var dlg = new NamingProfileEditorDialog(doc, _profiles) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _profiles = dlg.ResultProfiles;
                RefreshProfileCombo();
                UpdatePreview();
            }
        }

        // ── EXPORT SETTINGS PROFILES ────────────────────────────────
        private void RefreshExportProfileCombo()
        {
            string prev = comboExportProfile.SelectedItem as string;
            comboExportProfile.ItemsSource = _exportProfiles.Keys.ToList();
            if (prev != null && _exportProfiles.ContainsKey(prev))
                comboExportProfile.SelectedItem = prev;
            else if (_exportProfiles.Count > 0)
                comboExportProfile.SelectedIndex = 0;
        }

        private void OnExportProfileSelectionChanged(object s, SelectionChangedEventArgs e)
        {
            if (_applyingExportProfile) return;
            string name = comboExportProfile.SelectedItem as string;
            if (name == null || !_exportProfiles.TryGetValue(name, out var profile)) return;
            _applyingExportProfile = true;
            try { ApplyExportProfile(profile); }
            finally { _applyingExportProfile = false; }
        }

        private void OnSaveExportProfile(object s, RoutedEventArgs e)
        {
            string current = comboExportProfile.SelectedItem as string;

            // Ask: overwrite or new?
            if (current != null && _exportProfiles.ContainsKey(current))
            {
                var result = MessageBox.Show(
                    string.Format(L.MsgOverwriteProfile, current),
                    L.GrpExportProfile,
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel) return;

                if (result == MessageBoxResult.Yes)
                {
                    // Overwrite
                    var p = ReadCurrentExportProfile();
                    p.Name = current;
                    _exportProfiles[current] = p;
                    ExportSettingsHelper.SaveProfiles(ExportSettingsHelper.GetProfilesPath(), _exportProfiles);
                    return;
                }
                // No → fall through to save as new
            }

            // Save as new — open editor dialog pre-focused on "New from current"
            var dlg = new ExportProfileEditorDialog(_exportProfiles, ReadCurrentExportProfile(), current, openNew: true) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _exportProfiles = dlg.ResultProfiles;
                ExportSettingsHelper.SaveProfiles(ExportSettingsHelper.GetProfilesPath(), _exportProfiles);
                RefreshExportProfileCombo();
                if (dlg.SelectedProfileName != null)
                    comboExportProfile.SelectedItem = dlg.SelectedProfileName;
            }
        }

        private void OnEditExportProfiles(object s, RoutedEventArgs e)
        {
            string selected = comboExportProfile.SelectedItem as string;
            var current = ReadCurrentExportProfile();
            var dlg = new ExportProfileEditorDialog(_exportProfiles, current, selected, openNew: false) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _exportProfiles = dlg.ResultProfiles;
                ExportSettingsHelper.SaveProfiles(ExportSettingsHelper.GetProfilesPath(), _exportProfiles);
                RefreshExportProfileCombo();
                if (dlg.SelectedProfileName != null)
                    comboExportProfile.SelectedItem = dlg.SelectedProfileName;
            }
        }

        private ExportSettingsProfile ReadCurrentExportProfile() => new ExportSettingsProfile
        {
            NamingProfile              = comboProfile.SelectedItem as string ?? "",
            // Formats
            FormatPDF                  = chkPDF.IsChecked == true,
            FormatDWG                  = chkDWG.IsChecked == true,
            FormatIFC                  = chkIFC.IsChecked == true,
            FormatNWC                  = chkNWC.IsChecked == true,
            // PDF
            PdfUseRaster               = radioRaster.IsChecked == true,
            PdfQuality                 = comboQuality.SelectedIndex,
            PdfColorMode               = comboColorMode.SelectedIndex,
            PdfHideRef                 = chkRef.IsChecked == true,
            PdfHideTags                = chkTags.IsChecked == true,
            PdfHideScope               = chkScope.IsChecked == true,
            PdfHideCrop                = chkCrop.IsChecked == true,
            PdfCombined                = chkPDFSingle.IsChecked == true,
            PdfSingleName              = txtPdfSingleName.Text,
            // DWG
            DwgSetup                   = comboDWG.SelectedItem as string ?? ExportHelper.InSessionSetupName,
            DwgBindOle                 = chkBindOle.IsChecked == true,
            DwgVersionIndex            = comboDwgVersion.SelectedIndex,
            DwgMergedViews             = chkDwgViewXRef.IsChecked != true,
            DwgExportingAreas          = chkDwgAreas.IsChecked == true,
            DwgPreserveCoincident      = chkDwgCoincident.IsChecked == true,
            DwgMarkNonplot             = chkDwgNonplot.IsChecked == true,
            DwgHideReferencePlane      = chkDwgHideRef.IsChecked == true,
            DwgHideScopeBox           = chkDwgHideScope.IsChecked == true,
            DwgHideUnrefTags           = chkDwgHideTags.IsChecked == true,
            DwgNonplotSuffix           = txtDwgNonplotSuffix.Text,
            DwgACAIndex                = comboDwgACA.SelectedIndex,
            DwgPropOverridesIndex      = comboDwgPropOverrides.SelectedIndex,
            DwgColorIndex              = radioDwgColorIndex.IsChecked == true ? 1
                                       : radioDwgColorPerView.IsChecked == true ? 2 : 0,
            DwgTextIndex               = radioDwgTextApprox.IsChecked == true ? 1 : 0,
            DwgSolidsIndex             = radioDwgSolidsACIS.IsChecked == true ? 1 : 0,
            DwgSharedCoords            = radioDwgCoordShared.IsChecked == true,
            DwgUnitsIndex              = comboDwgUnits.SelectedIndex,
            DwgLineScalingIndex        = radioDwgLineModel.IsChecked == true ? 1
                                       : radioDwgLinePaper.IsChecked == true ? 2 : 0,
            // IFC
            IfcVersionIndex            = comboIfcVersion.SelectedIndex,
            IfcFileTypeIndex           = comboIfcFileType.SelectedIndex,
            IfcSpaceBoundaries         = comboIfcSpaceBoundaries.SelectedIndex,
            IfcLinkedFilesIndex        = comboIfcLinkedFiles.SelectedIndex,
            IfcSplitWalls              = chkIfcSplitWalls.IsChecked == true,
            IfcIncludeSteel            = chkIfcSteelElements.IsChecked == true,
            IfcExport2D                = chkIfc2D.IsChecked == true,
            IfcVisibleOnly             = chkIfcVisibleOnly.IsChecked == true,
            IfcExportRooms             = chkIfcRooms.IsChecked == true,
            IfcTessellationIndex       = comboIfcTessellation.SelectedIndex,
            IfcRevitPsets              = chkIfcRevitPsets.IsChecked == true,
            IfcCommonPsets             = chkIfcCommonPsets.IsChecked == true,
            IfcBaseQty                 = chkIfcBaseQty.IsChecked == true,
            IfcSchedulesPsets          = chkIfcSchedulesPsets.IsChecked == true,
            IfcMaterialPsets           = chkIfcMaterialPsets.IsChecked == true,
            IfcUserPsets               = chkIfcUserPsets.IsChecked == true,
            IfcUserPsetsFile           = txtIfcUserPsetsFile.Text,
            IfcParamMapping            = chkIfcParamMapping.IsChecked == true,
            IfcParamMappingFile        = txtIfcParamMappingFile.Text,
            IfcParts                   = chkIfcParts.IsChecked == true,
            IfcSolidModel              = chkIfcSolidModel.IsChecked == true,
            IfcActiveView              = chkIfcActiveView.IsChecked == true,
            IfcFamilyTypeName          = chkIfcFamilyTypeName.IsChecked == true,
            Ifc2DRoomBoundary          = chkIfc2DRoomBoundary.IsChecked == true,
            IfcSiteElevation           = chkIfcSiteElevation.IsChecked == true,
            IfcStoreGUID               = chkIfcStoreGUID.IsChecked == true,
            IfcTriangulationOnly       = chkIfcTriangulationOnly.IsChecked == true,
            IfcTypeNameOnly            = chkIfcTypeNameOnly.IsChecked == true,
            IfcVisibleName             = chkIfcVisibleName.IsChecked == true,
            IfcSitePlacementIndex      = comboIfcSitePlacement.SelectedIndex,
            IfcExchangeReqIndex        = comboIfcExchangeReq.SelectedIndex,
            // NWC
            NwcCoordsIndex             = comboNwcCoords.SelectedIndex,
            NwcParamsIndex             = comboNwcParams.SelectedIndex,
            NwcExportLinks             = chkNwcLinks.IsChecked == true,
            NwcConvertProps            = chkNwcConvertProps.IsChecked == true,
            NwcDivide                  = chkNwcDivide.IsChecked == true,
            NwcFaceting                = txtNwcFaceting.Text,
            NwcExportUrls              = chkNwcUrls.IsChecked == true,
            NwcMissingMaterials        = chkNwcMissingMat.IsChecked == true,
            NwcRoomAsAttribute         = chkNwcRoomAttr.IsChecked == true,
            NwcRoomGeometry            = chkNwcRoomGeom.IsChecked == true,
            // Output
            OutputUseProjectSubfolders = chkProjectSubfolders.IsChecked == true,
            OutputUseFormatSubfolders  = chkSubfolders.IsChecked == true,
        };

        private void ApplyExportProfile(ExportSettingsProfile p)
        {
            // Naming profile
            if (!string.IsNullOrEmpty(p.NamingProfile) && _profiles.ContainsKey(p.NamingProfile))
                comboProfile.SelectedItem = p.NamingProfile;

            // Formats
            chkPDF.IsChecked = p.FormatPDF;
            chkDWG.IsChecked = p.FormatDWG;
            chkIFC.IsChecked = p.FormatIFC;
            chkNWC.IsChecked = p.FormatNWC;

            // PDF
            if (p.PdfUseRaster) radioRaster.IsChecked = true; else radioVector.IsChecked = true;
            comboQuality.SelectedIndex    = Clamp(p.PdfQuality,    0, comboQuality.Items.Count - 1);
            comboColorMode.SelectedIndex  = Clamp(p.PdfColorMode,  0, comboColorMode.Items.Count - 1);
            chkRef.IsChecked              = p.PdfHideRef;
            chkTags.IsChecked             = p.PdfHideTags;
            chkScope.IsChecked            = p.PdfHideScope;
            chkCrop.IsChecked             = p.PdfHideCrop;
            chkPDFSingle.IsChecked        = p.PdfCombined;
            if (!string.IsNullOrEmpty(p.PdfSingleName)) txtPdfSingleName.Text = p.PdfSingleName;

            // DWG setup combo — only apply when single doc (multi-doc panel is disabled)
            if (_selectedDocs.Count == 1)
            {
                var setups = comboDWG.ItemsSource as IEnumerable<string>;
                if (setups != null && setups.Contains(p.DwgSetup))
                    comboDWG.SelectedItem = p.DwgSetup;
            }
            chkBindOle.IsChecked = p.DwgBindOle;

            // DWG settings panel
            var dwgCfg = new DwgExportConfig
            {
                VersionIndex            = p.DwgVersionIndex,
                MergedViews             = p.DwgMergedViews,
                ExportingAreas          = p.DwgExportingAreas,
                PreserveCoincidentLines = p.DwgPreserveCoincident,
                MarkNonplotLayers       = p.DwgMarkNonplot,
                HideReferencePlane      = p.DwgHideReferencePlane,
                HideScopeBox           = p.DwgHideScopeBox,
                HideUnreferenceViewTags = p.DwgHideUnrefTags,
                NonplotSuffix           = p.DwgNonplotSuffix,
                ACAIndex                = p.DwgACAIndex,
                PropOverridesIndex      = p.DwgPropOverridesIndex,
                ColorIndex              = p.DwgColorIndex,
                TextIndex               = p.DwgTextIndex,
                SolidsIndex             = p.DwgSolidsIndex,
                SharedCoords            = p.DwgSharedCoords,
                UnitsIndex              = p.DwgUnitsIndex,
                LineScalingIndex        = p.DwgLineScalingIndex,
            };
            LoadDwgConfig(dwgCfg);

            // IFC
            comboIfcVersion.SelectedIndex        = Clamp(p.IfcVersionIndex,       0, comboIfcVersion.Items.Count - 1);
            comboIfcFileType.SelectedIndex        = Clamp(p.IfcFileTypeIndex,      0, comboIfcFileType.Items.Count - 1);
            comboIfcSpaceBoundaries.SelectedIndex = Clamp(p.IfcSpaceBoundaries,    0, comboIfcSpaceBoundaries.Items.Count - 1);
            comboIfcLinkedFiles.SelectedIndex     = Clamp(p.IfcLinkedFilesIndex,   0, comboIfcLinkedFiles.Items.Count - 1);
            chkIfcSplitWalls.IsChecked            = p.IfcSplitWalls;
            chkIfcSteelElements.IsChecked         = p.IfcIncludeSteel;
            chkIfc2D.IsChecked                    = p.IfcExport2D;
            chkIfcVisibleOnly.IsChecked           = p.IfcVisibleOnly;
            chkIfcRooms.IsChecked                 = p.IfcExportRooms;
            comboIfcTessellation.SelectedIndex    = Clamp(p.IfcTessellationIndex,  0, comboIfcTessellation.Items.Count - 1);
            chkIfcRevitPsets.IsChecked            = p.IfcRevitPsets;
            chkIfcCommonPsets.IsChecked           = p.IfcCommonPsets;
            chkIfcBaseQty.IsChecked               = p.IfcBaseQty;
            chkIfcSchedulesPsets.IsChecked        = p.IfcSchedulesPsets;
            chkIfcMaterialPsets.IsChecked         = p.IfcMaterialPsets;
            chkIfcUserPsets.IsChecked             = p.IfcUserPsets;
            txtIfcUserPsetsFile.Text              = p.IfcUserPsetsFile;
            chkIfcParamMapping.IsChecked          = p.IfcParamMapping;
            txtIfcParamMappingFile.Text           = p.IfcParamMappingFile;
            chkIfcParts.IsChecked                 = p.IfcParts;
            chkIfcSolidModel.IsChecked            = p.IfcSolidModel;
            chkIfcActiveView.IsChecked            = p.IfcActiveView;
            chkIfcFamilyTypeName.IsChecked        = p.IfcFamilyTypeName;
            chkIfc2DRoomBoundary.IsChecked        = p.Ifc2DRoomBoundary;
            chkIfcSiteElevation.IsChecked         = p.IfcSiteElevation;
            chkIfcStoreGUID.IsChecked             = p.IfcStoreGUID;
            chkIfcTriangulationOnly.IsChecked     = p.IfcTriangulationOnly;
            chkIfcTypeNameOnly.IsChecked          = p.IfcTypeNameOnly;
            chkIfcVisibleName.IsChecked           = p.IfcVisibleName;
            comboIfcSitePlacement.SelectedIndex   = Clamp(p.IfcSitePlacementIndex, 0, comboIfcSitePlacement.Items.Count - 1);
            comboIfcExchangeReq.SelectedIndex     = Clamp(p.IfcExchangeReqIndex,   0, comboIfcExchangeReq.Items.Count - 1);

            // NWC
            comboNwcCoords.SelectedIndex  = Clamp(p.NwcCoordsIndex, 0, comboNwcCoords.Items.Count - 1);
            comboNwcParams.SelectedIndex  = Clamp(p.NwcParamsIndex, 0, comboNwcParams.Items.Count - 1);
            chkNwcLinks.IsChecked         = p.NwcExportLinks;
            chkNwcConvertProps.IsChecked  = p.NwcConvertProps;
            chkNwcDivide.IsChecked        = p.NwcDivide;
            txtNwcFaceting.Text           = p.NwcFaceting;
            chkNwcUrls.IsChecked          = p.NwcExportUrls;
            chkNwcMissingMat.IsChecked    = p.NwcMissingMaterials;
            chkNwcRoomAttr.IsChecked      = p.NwcRoomAsAttribute;
            chkNwcRoomGeom.IsChecked      = p.NwcRoomGeometry;

            // Output
            chkProjectSubfolders.IsChecked = p.OutputUseProjectSubfolders;
            chkSubfolders.IsChecked        = p.OutputUseFormatSubfolders;

            UpdateFormatGroups();
            UpdatePreview();
        }

        private static int Clamp(int val, int min, int max) =>
            val < min ? min : val > max ? max : val;

        private void UpdatePreview()
        {
            if (txtPreview == null || comboProfile == null) return; // not yet initialized
            string profile = comboProfile.SelectedItem as string ?? "";
            var example = _allSheets.FirstOrDefault(s => s.IsChecked && s.Sheet != null && s.Sheet.IsValidObject)
                       ?? _allSheets.FirstOrDefault(s => s.Sheet != null && s.Sheet.IsValidObject)
                       ?? _allSheets.FirstOrDefault();
            if (example == null) { txtPreview.Text = "—"; txtPreviewExt.Text = ""; return; }
            txtPreview.Text = NamingHelper.GenerateName(example.Sheet, profile, _profiles, example.DocRef, example.Number, example.Name);
            var exts = new List<string>();
            if (chkPDF.IsChecked == true) exts.Add(".pdf");
            if (chkDWG.IsChecked == true) exts.Add(".dwg");
            if (chkIFC.IsChecked == true) exts.Add(".ifc");
            if (chkNWC.IsChecked == true) exts.Add(".nwc");
            txtPreviewExt.Text = string.Join("  •  ", exts);
        }

        // ── FORMAT OPTIONS ──────────────────────────────────────
        private void OnFormatChanged(object s, RoutedEventArgs e) => UpdateFormatGroups();
        private void UpdateFormatGroups()
        {
            if (cardPDF == null || panelPDF == null || panelDWG == null || tabIFC == null || tabNWC == null) return;
            panelPDF.IsEnabled  = chkPDF.IsChecked == true;
            panelDWG.IsEnabled  = chkDWG.IsChecked == true;
            tabIFC.IsEnabled    = chkIFC.IsChecked == true;
            tabNWC.IsEnabled    = chkNWC.IsChecked == true;
            UpdateCardStyles();
            UpdatePreview();
        }

        private void SelectFormat(string fmt)
        {
            if (panelPDF == null || panelDWG == null || tabIFC == null || tabNWC == null) return;
            _selectedFormat = fmt;
            panelPDF.Visibility  = fmt == "PDF" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            panelDWG.Visibility  = fmt == "DWG" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            tabIFC.Visibility    = fmt == "IFC" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            tabNWC.Visibility    = fmt == "NWC" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            UpdateCardStyles();
        }

        private void UpdateCardStyles()
        {
            if (cardPDF == null || lblCardPDF == null) return;
            ApplyCardStyle(cardPDF, lblCardPDF, _selectedFormat == "PDF", chkPDF.IsChecked == true);
            ApplyCardStyle(cardDWG, lblCardDWG, _selectedFormat == "DWG", chkDWG.IsChecked == true);
            ApplyCardStyle(cardIFC, lblCardIFC, _selectedFormat == "IFC", chkIFC.IsChecked == true);
            ApplyCardStyle(cardNWC, lblCardNWC, _selectedFormat == "NWC", chkNWC.IsChecked == true);
        }

        private static void ApplyCardStyle(System.Windows.Controls.Border card, System.Windows.Controls.TextBlock lbl, bool selected, bool enabled)
        {
            if (selected)
            {
                card.BorderBrush    = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 115, 232));
                card.BorderThickness = new System.Windows.Thickness(2);
                card.Background     = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 240, 254));
                lbl.Foreground      = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 115, 232));
            }
            else
            {
                card.BorderBrush    = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 225, 231));
                card.BorderThickness = new System.Windows.Thickness(1);
                card.Background     = enabled
                    ? System.Windows.Media.Brushes.White
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 249, 250));
                lbl.Foreground      = enabled
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 64, 67))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170));
            }
        }

        private void OnCardPDFClick(object s, MouseButtonEventArgs e) => SelectFormat("PDF");
        private void OnCardDWGClick(object s, MouseButtonEventArgs e) => SelectFormat("DWG");
        private void OnCardIFCClick(object s, MouseButtonEventArgs e) => SelectFormat("IFC");
        private void OnCardNWCClick(object s, MouseButtonEventArgs e) => SelectFormat("NWC");

        private void OnImportNwcSettings(object s, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title  = L.BtnImportNwcSettings,
                Filter = "NWC Settings (*.txt;*.xml)|*.txt;*.xml|All files (*.*)|*.*",
                DefaultExt = ".txt"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var cfg = NwcExportConfig.LoadFromFile(dlg.FileName);
                comboNwcCoords.SelectedIndex      = cfg.Coordinates == "Internal" ? 1 : 0;
                int nwcParamIdx = new[] { "None", "ModelProperties", "AllProperties" }.ToList().IndexOf(cfg.Parameters);
                comboNwcParams.SelectedIndex      = nwcParamIdx < 0 ? 0 : nwcParamIdx;
                chkNwcLinks.IsChecked             = cfg.ExportLinks;
                chkNwcConvertProps.IsChecked      = cfg.ConvertElementProperties;
                chkNwcDivide.IsChecked            = cfg.DivideFileIntoLevels;
                txtNwcFaceting.Text               = cfg.FacetingFactor.ToString(System.Globalization.CultureInfo.InvariantCulture);
                chkNwcUrls.IsChecked              = cfg.ExportUrls;
                chkNwcMissingMat.IsChecked        = cfg.FindMissingMaterials;
                chkNwcRoomAttr.IsChecked          = cfg.ExportRoomAsAttribute;
                chkNwcRoomGeom.IsChecked          = cfg.ExportRoomGeometry;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "JFTools", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnSaveNwcSettings(object s, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title      = L.BtnSaveNwcSettings,
                Filter     = "NWC Settings (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".txt",
                FileName   = "nwc_export_settings.txt"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var cfg = new NwcExportConfig
                {
                    Coordinates              = comboNwcCoords.SelectedIndex == 1 ? "Internal" : "Shared",
                    ExportLinks              = chkNwcLinks.IsChecked == true,
                    ConvertElementProperties = chkNwcConvertProps.IsChecked == true,
                    DivideFileIntoLevels     = chkNwcDivide.IsChecked == true,
                    Parameters               = new[] { "None", "ModelProperties", "AllProperties" }
                                              [comboNwcParams.SelectedIndex >= 0 ? comboNwcParams.SelectedIndex : 0],
                    FacetingFactor           = double.TryParse(txtNwcFaceting.Text,
                                                  System.Globalization.NumberStyles.Float,
                                                  System.Globalization.CultureInfo.InvariantCulture,
                                                  out double ff) ? ff : 1.0,
                    ExportUrls               = chkNwcUrls.IsChecked == true,
                    FindMissingMaterials     = chkNwcMissingMat.IsChecked == true,
                    ExportRoomAsAttribute    = chkNwcRoomAttr.IsChecked == true,
                    ExportRoomGeometry       = chkNwcRoomGeom.IsChecked == true,
                };
                cfg.SaveToFile(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "JFTools", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnImportIfcSettings(object s, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title      = L.BtnImportIfcSettings,
                Filter     = "IFC Settings (*.txt;*.xml)|*.txt;*.xml|All files (*.*)|*.*",
                DefaultExt = ".txt"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var cfg = IfcExportConfig.LoadFromFile(dlg.FileName);

                // Version / type
                int vIdx = Array.IndexOf(_ifcVersionEnum, cfg.IfcVersion);
                comboIfcVersion.SelectedIndex = vIdx >= 0 ? vIdx : 0;
                int ftIdx = Array.IndexOf(_ifcFileTypeEnum, cfg.FileType);
                comboIfcFileType.SelectedIndex = ftIdx >= 0 ? ftIdx : 0;
                comboIfcSpaceBoundaries.SelectedIndex = cfg.SpaceBoundaries;

                // Linked files (combo only — no separate checkbox)
                comboIfcLinkedFiles.SelectedIndex = cfg.LinkedFilesMode == "Separate" ? 1 : 0;

                // Tessellation
                var tessLevels = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 };
                int tIdx = Array.IndexOf(tessLevels, cfg.TessellationLevelOfDetail);
                comboIfcTessellation.SelectedIndex = tIdx >= 0 ? tIdx : 2;

                // Site placement / exchange req
                int spIdx = Array.IndexOf(_ifcSitePlacementEnum, cfg.SitePlacement);
                comboIfcSitePlacement.SelectedIndex = spIdx >= 0 ? spIdx : 0;
                int erIdx = Array.IndexOf(_ifcExchangeReqEnum, cfg.ExchangeRequirement);
                comboIfcExchangeReq.SelectedIndex = erIdx >= 0 ? erIdx : 0;

                // Basic Settings checkboxes
                chkIfcSplitWalls.IsChecked         = cfg.SplitWallsAndColumns;
                chkIfcSteelElements.IsChecked      = cfg.IncludeSteelElements;
                chkIfc2D.IsChecked                 = cfg.Export2DElements;
                chkIfcVisibleOnly.IsChecked        = cfg.VisibleElementsOfCurrentView;
                chkIfcRooms.IsChecked              = cfg.ExportRoomsInView;
                chkIfcRevitPsets.IsChecked         = cfg.ExportInternalRevitPropertySets;
                chkIfcCommonPsets.IsChecked        = cfg.ExportIFCCommonPropertySets;
                chkIfcBaseQty.IsChecked            = cfg.ExportBaseQuantities;
                chkIfcSchedulesPsets.IsChecked     = cfg.ExportSchedulesAsPsets;
                chkIfcMaterialPsets.IsChecked      = cfg.ExportMaterialPsets;
                chkIfcUserPsets.IsChecked          = cfg.ExportUserDefinedPsets;
                txtIfcUserPsetsFile.Text           = cfg.ExportUserDefinedPsetsFileName;
                chkIfcParamMapping.IsChecked       = cfg.ExportUserDefinedParameterMapping;
                txtIfcParamMappingFile.Text        = cfg.ExportUserDefinedParameterMappingFileName;

                // Advanced checkboxes
                chkIfcParts.IsChecked              = cfg.ExportPartsAsBuildingElements;
                chkIfcSolidModel.IsChecked         = cfg.ExportSolidModelRep;
                chkIfcActiveView.IsChecked         = cfg.UseActiveViewGeometry;
                chkIfcFamilyTypeName.IsChecked     = cfg.UseFamilyAndTypeNameForReference;
                chkIfc2DRoomBoundary.IsChecked     = cfg.Use2DRoomBoundaryForVolume;
                chkIfcSiteElevation.IsChecked      = cfg.IncludeSiteElevation;
                chkIfcStoreGUID.IsChecked          = cfg.StoreIFCGUID;
                chkIfcTriangulationOnly.IsChecked  = cfg.UseOnlyTriangulation;
                chkIfcTypeNameOnly.IsChecked       = cfg.UseTypeNameOnlyForIfcType;
                chkIfcVisibleName.IsChecked        = cfg.UseVisibleRevitNameAsEntityName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "JFTools", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnSaveIfcSettings(object s, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title      = L.BtnSaveIfcSettings,
                Filter     = "IFC Settings (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".txt",
                FileName   = "ifc_export_settings.txt"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var cfg = new IfcExportConfig
                {
                    IfcVersion  = _ifcVersionEnum[comboIfcVersion.SelectedIndex >= 0 ? comboIfcVersion.SelectedIndex : 0],
                    FileType    = _ifcFileTypeEnum[comboIfcFileType.SelectedIndex >= 0 ? comboIfcFileType.SelectedIndex : 0],
                    SpaceBoundaries = comboIfcSpaceBoundaries.SelectedIndex >= 0 ? comboIfcSpaceBoundaries.SelectedIndex : 0,
                    LinkedFilesMode = comboIfcLinkedFiles.SelectedIndex == 1 ? "Separate" : "None",
                    TessellationLevelOfDetail = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 }[comboIfcTessellation.SelectedIndex >= 0 ? comboIfcTessellation.SelectedIndex : 2],
                    SitePlacement       = _ifcSitePlacementEnum[comboIfcSitePlacement.SelectedIndex >= 0 ? comboIfcSitePlacement.SelectedIndex : 0],
                    ExchangeRequirement = _ifcExchangeReqEnum[comboIfcExchangeReq.SelectedIndex >= 0 ? comboIfcExchangeReq.SelectedIndex : 0],
                    SplitWallsAndColumns         = chkIfcSplitWalls.IsChecked == true,
                    IncludeSteelElements         = chkIfcSteelElements.IsChecked == true,
                    Export2DElements             = chkIfc2D.IsChecked == true,
                    ExportRoomsInView            = chkIfcRooms.IsChecked == true,
                    VisibleElementsOfCurrentView = chkIfcVisibleOnly.IsChecked == true,
                    UseActiveViewGeometry        = chkIfcActiveView.IsChecked == true,
                    ExportInternalRevitPropertySets   = chkIfcRevitPsets.IsChecked == true,
                    ExportIFCCommonPropertySets       = chkIfcCommonPsets.IsChecked == true,
                    ExportBaseQuantities              = chkIfcBaseQty.IsChecked == true,
                    ExportSchedulesAsPsets            = chkIfcSchedulesPsets.IsChecked == true,
                    ExportMaterialPsets               = chkIfcMaterialPsets.IsChecked == true,
                    ExportUserDefinedPsets            = chkIfcUserPsets.IsChecked == true,
                    ExportUserDefinedPsetsFileName    = txtIfcUserPsetsFile.Text,
                    ExportUserDefinedParameterMapping = chkIfcParamMapping.IsChecked == true,
                    ExportUserDefinedParameterMappingFileName = txtIfcParamMappingFile.Text,
                    ExportPartsAsBuildingElements    = chkIfcParts.IsChecked == true,
                    ExportSolidModelRep              = chkIfcSolidModel.IsChecked == true,
                    UseFamilyAndTypeNameForReference = chkIfcFamilyTypeName.IsChecked == true,
                    Use2DRoomBoundaryForVolume       = chkIfc2DRoomBoundary.IsChecked == true,
                    IncludeSiteElevation             = chkIfcSiteElevation.IsChecked == true,
                    StoreIFCGUID                     = chkIfcStoreGUID.IsChecked == true,
                    UseOnlyTriangulation             = chkIfcTriangulationOnly.IsChecked == true,
                    UseTypeNameOnlyForIfcType        = chkIfcTypeNameOnly.IsChecked == true,
                    UseVisibleRevitNameAsEntityName  = chkIfcVisibleName.IsChecked == true,
                };
                cfg.SaveToFile(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "JFTools", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnPdfSingleChanged(object s, RoutedEventArgs e)
        {
            if (lblPdfSingleName == null) return;
            var vis = chkPDFSingle.IsChecked == true ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            lblPdfSingleName.Visibility = vis;
            txtPdfSingleName.Visibility = vis;
        }

        // ── DWG SETUPS ──────────────────────────────────────────
        private void RefreshDwgSetups()
        {
            var setups = ExportHelper.GetDwgSetups(_selectedDocs);
            comboDWG.ItemsSource = setups;
            // Prefer first named setup over In-Session when one is available
            comboDWG.SelectedIndex = setups.Count > 1 ? 1 : 0;
        }


        private void OnDwgSetupSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Save In-Session panel state before switching away from it
            if (e.RemovedItems.Count > 0 &&
                e.RemovedItems[0] as string == ExportHelper.InSessionSetupName)
                _inSessionConfig = ReadDwgConfig();

            string setup = comboDWG.SelectedItem as string;
            if (setup == null) return;

            if (setup == ExportHelper.InSessionSetupName)
            {
                LoadDwgConfig(_inSessionConfig);
                return;
            }

            // Named setup: load its values into the panel
            try
            {
                var doc = _selectedDocs?.FirstOrDefault();
                if (doc == null) return;
                var opt = DWGExportOptions.GetPredefinedOptions(doc, setup);
                if (opt != null) LoadDwgConfig(DwgExportConfig.FromOptions(opt));
            }
            catch { }
        }

        /// <summary>Pushes a <see cref="DwgExportConfig"/> into all DWG panel controls.</summary>
        private void LoadDwgConfig(DwgExportConfig cfg)
        {
            // Combos
            comboDwgVersion.SelectedIndex       = cfg.VersionIndex;
            comboDwgACA.SelectedIndex           = cfg.ACAIndex;
            comboDwgPropOverrides.SelectedIndex = cfg.PropOverridesIndex;
            comboDwgUnits.SelectedIndex         = cfg.UnitsIndex;

            // Checkboxes
            chkDwgAreas.IsChecked      = cfg.ExportingAreas;
            chkDwgCoincident.IsChecked = cfg.PreserveCoincidentLines;
            chkDwgNonplot.IsChecked    = cfg.MarkNonplotLayers;
            chkDwgHideRef.IsChecked    = cfg.HideReferencePlane;
            chkDwgHideScope.IsChecked  = cfg.HideScopeBox;
            chkDwgHideTags.IsChecked   = cfg.HideUnreferenceViewTags;
            txtDwgNonplotSuffix.Text   = cfg.NonplotSuffix ?? "";

            // Checkbox – Export views as XREF (checked = XREF = MergedViews false)
            chkDwgViewXRef.IsChecked = !cfg.MergedViews;

            // Radio – Colors (0=TrueColor, 1=IndexColors, 2=TrueColorPerView)
            radioDwgColorTrue.IsChecked    = cfg.ColorIndex == 0;
            radioDwgColorIndex.IsChecked   = cfg.ColorIndex == 1;
            radioDwgColorPerView.IsChecked = cfg.ColorIndex == 2;

            // Radio – Text (0=Exact, 1=Approximate)
            radioDwgTextExact.IsChecked  = cfg.TextIndex == 0;
            radioDwgTextApprox.IsChecked = cfg.TextIndex == 1;

            // Radio – Solids (0=Polymesh, 1=ACIS)
            radioDwgSolidsPolymesh.IsChecked = cfg.SolidsIndex == 0;
            radioDwgSolidsACIS.IsChecked     = cfg.SolidsIndex == 1;

            // Radio – Coordinates
            radioDwgCoordProject.IsChecked = !cfg.SharedCoords;
            radioDwgCoordShared.IsChecked  = cfg.SharedCoords;

            // Radio – Line scaling (0=ViewScale, 1=ModelSpace, 2=PaperSpace)
            radioDwgLineViewScale.IsChecked = cfg.LineScalingIndex == 0;
            radioDwgLineModel.IsChecked     = cfg.LineScalingIndex == 1;
            radioDwgLinePaper.IsChecked     = cfg.LineScalingIndex == 2;
        }

        private DwgExportConfig ReadDwgConfig() => new DwgExportConfig
        {
            VersionIndex            = comboDwgVersion.SelectedIndex,
            ACAIndex                = comboDwgACA.SelectedIndex,
            PropOverridesIndex      = comboDwgPropOverrides.SelectedIndex,
            UnitsIndex              = comboDwgUnits.SelectedIndex,
            ExportingAreas          = chkDwgAreas.IsChecked == true,
            PreserveCoincidentLines = chkDwgCoincident.IsChecked == true,
            MarkNonplotLayers       = chkDwgNonplot.IsChecked == true,
            HideReferencePlane      = chkDwgHideRef.IsChecked == true,
            HideScopeBox            = chkDwgHideScope.IsChecked == true,
            HideUnreferenceViewTags = chkDwgHideTags.IsChecked == true,
            NonplotSuffix           = txtDwgNonplotSuffix.Text,
            MergedViews             = chkDwgViewXRef.IsChecked != true,
            ColorIndex              = radioDwgColorIndex.IsChecked == true ? 1
                                    : radioDwgColorPerView.IsChecked == true ? 2 : 0,
            TextIndex               = radioDwgTextApprox.IsChecked == true ? 1 : 0,
            SolidsIndex             = radioDwgSolidsACIS.IsChecked == true ? 1 : 0,
            SharedCoords            = radioDwgCoordShared.IsChecked == true,
            LineScalingIndex        = radioDwgLineModel.IsChecked == true ? 1
                                    : radioDwgLinePaper.IsChecked == true ? 2 : 0,
        };

        // ── BROWSE ──────────────────────────────────────────────
        private void OnBrowse(object s, RoutedEventArgs e)
        {
            var dlg = new Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == Forms.DialogResult.OK)
                txtFolder.Text = dlg.SelectedPath;
        }

        // ── BULK CHECKBOX TOGGLE ─────────────────────────────────
        private void OnSheetListMouseDown(object sender, MouseButtonEventArgs e)
        {
            var cb = FindVisualAncestor<System.Windows.Controls.CheckBox>(e.OriginalSource as DependencyObject);
            if (cb == null) return;
            var item = cb.DataContext as SheetItem;
            if (item == null) return;

            bool newValue = !item.IsChecked;
            var selected = sheetList.SelectedItems.OfType<SheetItem>().ToList();

            if (selected.Count > 1 && selected.Contains(item))
                foreach (var row in selected) row.IsChecked = newValue;
            else
                item.IsChecked = newValue;

            sheetList.Items.Refresh();
            UpdateCount();
            UpdatePreview();
            e.Handled = true;
        }

        private static T FindVisualAncestor<T>(DependencyObject element) where T : DependencyObject
        {
            var current = element;
            while (current != null)
            {
                if (current is T target) return target;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        // ── ADD REVISION ─────────────────────────────────────────
        private void OnAddRevision(object sender, RoutedEventArgs e)
        {
            var checked_ = _allSheets.Where(s => s.IsChecked).ToList();
            if (checked_.Count == 0)
            {
                MessageBox.Show(L.MsgSelectOneSheet, L.RevTitle);
                return;
            }
            var dlg = new RevisionEditorDialog(checked_) { Owner = this };
            dlg.ShowDialog();
            RefreshSheetList(keepChecked: true);
        }

        // ── DWG PER PROJECT ──────────────────────────────────────
        private void OnDwgPerProject(object sender, RoutedEventArgs e)
        {
            var dlg = new DwgPerProjectDialog(_selectedDocs, comboDWG.SelectedItem as string ?? "", _dwgSetupPerDoc)
            {
                Owner = this
            };
            if (dlg.ShowDialog() == true)
                _dwgSetupPerDoc = dlg.Result;
        }

        // ── IFC HELPERS ──────────────────────────────────────────
        private void OnBrowseIfcUserPsets(object sender, RoutedEventArgs e)
        {
            var dlg = new Forms.OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*" };
            if (dlg.ShowDialog() == Forms.DialogResult.OK) txtIfcUserPsetsFile.Text = dlg.FileName;
        }

        private void OnBrowseIfcParamMapping(object sender, RoutedEventArgs e)
        {
            var dlg = new Forms.OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*" };
            if (dlg.ShowDialog() == Forms.DialogResult.OK) txtIfcParamMappingFile.Text = dlg.FileName;
        }

        // ── CANCEL ──────────────────────────────────────────────
        private void OnCancel(object s, RoutedEventArgs e)
        {
            if (!btnExport.IsEnabled) { _exportCancelled = true; btnCancel.Content = L.BtnCancelling; btnCancel.IsEnabled = false; }
            else Close();
        }

        // ── EXPORT ──────────────────────────────────────────────
        private void OnExport(object s, RoutedEventArgs e)
        {
            bool doPdf = chkPDF.IsChecked == true;
            bool doDwg = chkDWG.IsChecked == true;
            bool doIfc = chkIFC.IsChecked == true;
            bool doNwc = chkNWC.IsChecked == true;
            if (!doPdf && !doDwg && !doIfc && !doNwc) { MessageBox.Show(L.MsgSelectFormat, L.TitleExport); return; }

            var items = _allSheets.Where(i => i.IsChecked).ToList();
            bool onlyModelExport = (doIfc || doNwc) && !doPdf && !doDwg;
            if (items.Count == 0 && !onlyModelExport) { MessageBox.Show(L.MsgNoSheets, L.TitleExport); return; }
            if (string.IsNullOrWhiteSpace(txtFolder.Text)) { MessageBox.Show(L.MsgSelectFolder, L.TitleExport); return; }

            var settings = new ExportSettings
            {
                ExportPdf = doPdf,
                ExportDwg = doDwg,
                OutputFolder = txtFolder.Text.Trim(),
                UseSubfolders = chkSubfolders.IsChecked == true,
                UseProjectSubfolders = chkProjectSubfolders.IsChecked == true,
                ProfileName = comboProfile.SelectedItem as string ?? "",
                Profiles = _profiles,
                UseRaster = radioRaster.IsChecked == true,
                RasterQuality = GetRasterQuality(),
                ColorMode = GetColorMode(),
                HideRef = chkRef.IsChecked == true,
                HideTags = chkTags.IsChecked == true,
                HideScope = chkScope.IsChecked == true,
                HideCrop = chkCrop.IsChecked == true,
                PdfSingle = chkPDFSingle.IsChecked == true,
                PdfSingleName = string.IsNullOrWhiteSpace(txtPdfSingleName.Text) ? L.DefaultPdfName : txtPdfSingleName.Text.Trim(),
                DwgSetup = comboDWG.SelectedItem as string ?? "",
                DwgSetupPerDoc = _dwgSetupPerDoc,
                DwgConfig = ReadDwgConfig(),
                BindImagesAsOle = chkBindOle.IsChecked == true,
                ExportIfc = doIfc,
                IfcDocuments = _selectedDocs.ToList(),
                ExportNwc = doNwc,
                NwcDocuments = _selectedDocs.ToList(),
                NwcConfig = new NwcExportConfig
                {
                    Coordinates              = comboNwcCoords.SelectedIndex == 1 ? "Internal" : "Shared",
                    ExportLinks              = chkNwcLinks.IsChecked == true,
                    ConvertElementProperties = chkNwcConvertProps.IsChecked == true,
                    DivideFileIntoLevels     = chkNwcDivide.IsChecked == true,
                    Parameters               = new[] { "None", "ModelProperties", "AllProperties" }
                                              [comboNwcParams.SelectedIndex >= 0 ? comboNwcParams.SelectedIndex : 0],
                    // Advanced
                    FacetingFactor           = double.TryParse(txtNwcFaceting.Text,
                                                  System.Globalization.NumberStyles.Float,
                                                  System.Globalization.CultureInfo.InvariantCulture,
                                                  out double ff) ? ff : 1.0,
                    ExportUrls               = chkNwcUrls.IsChecked == true,
                    FindMissingMaterials     = chkNwcMissingMat.IsChecked == true,
                    ExportRoomAsAttribute    = chkNwcRoomAttr.IsChecked == true,
                    ExportRoomGeometry       = chkNwcRoomGeom.IsChecked == true,
                },
                IfcConfig = new IfcExportConfig
                {
                    IfcVersion  = _ifcVersionEnum[comboIfcVersion.SelectedIndex >= 0 ? comboIfcVersion.SelectedIndex : 0],
                    FileType    = _ifcFileTypeEnum[comboIfcFileType.SelectedIndex >= 0 ? comboIfcFileType.SelectedIndex : 0],
                    SpaceBoundaries = comboIfcSpaceBoundaries.SelectedIndex >= 0 ? comboIfcSpaceBoundaries.SelectedIndex : 0,

                    Export2DElements             = chkIfc2D.IsChecked == true,
                    ExportRoomsInView            = chkIfcRooms.IsChecked == true,
                    ExportPartsAsBuildingElements = chkIfcParts.IsChecked == true,
                    ExportSolidModelRep          = chkIfcSolidModel.IsChecked == true,
                    IncludeSiteElevation         = chkIfcSiteElevation.IsChecked == true,
                    IncludeSteelElements         = chkIfcSteelElements.IsChecked == true,
                    LinkedFilesMode              = comboIfcLinkedFiles.SelectedIndex == 1 ? "Separate" : "None",
                    VisibleElementsOfCurrentView = chkIfcVisibleOnly.IsChecked == true,
                    UseActiveViewGeometry        = chkIfcActiveView.IsChecked == true,

                    ExportBaseQuantities              = chkIfcBaseQty.IsChecked == true,
                    ExportIFCCommonPropertySets       = chkIfcCommonPsets.IsChecked == true,
                    ExportInternalRevitPropertySets   = chkIfcRevitPsets.IsChecked == true,
                    ExportSchedulesAsPsets            = chkIfcSchedulesPsets.IsChecked == true,
                    ExportMaterialPsets               = chkIfcMaterialPsets.IsChecked == true,
                    ExportUserDefinedPsets            = chkIfcUserPsets.IsChecked == true,
                    ExportUserDefinedPsetsFileName    = txtIfcUserPsetsFile.Text,
                    ExportUserDefinedParameterMapping = chkIfcParamMapping.IsChecked == true,
                    ExportUserDefinedParameterMappingFileName = txtIfcParamMappingFile.Text,

                    TessellationLevelOfDetail = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 }[comboIfcTessellation.SelectedIndex >= 0 ? comboIfcTessellation.SelectedIndex : 2],

                    SplitWallsAndColumns          = chkIfcSplitWalls.IsChecked == true,
                    StoreIFCGUID                  = chkIfcStoreGUID.IsChecked == true,
                    UseOnlyTriangulation          = chkIfcTriangulationOnly.IsChecked == true,
                    Use2DRoomBoundaryForVolume    = chkIfc2DRoomBoundary.IsChecked == true,
                    UseFamilyAndTypeNameForReference = chkIfcFamilyTypeName.IsChecked == true,
                    UseTypeNameOnlyForIfcType     = chkIfcTypeNameOnly.IsChecked == true,
                    UseVisibleRevitNameAsEntityName = chkIfcVisibleName.IsChecked == true,
                    SitePlacement       = _ifcSitePlacementEnum[comboIfcSitePlacement.SelectedIndex >= 0 ? comboIfcSitePlacement.SelectedIndex : 0],
                    ExchangeRequirement = _ifcExchangeReqEnum[comboIfcExchangeReq.SelectedIndex >= 0 ? comboIfcExchangeReq.SelectedIndex : 0],
                },
            };

            btnExport.IsEnabled = false;
            _exportCancelled = false;
            btnCancel.Content = L.BtnCancel;
            btnCancel.IsEnabled = true;

            var result = ExportHelper.RunExport(items, settings,
                (cur, tot, label) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        progressBar.Maximum = tot;
                        progressBar.Value = cur;
                        int pct = tot > 0 ? (int)Math.Round(cur * 100.0 / tot) : 0;
                        progressPct.Text = pct + "%";
                        progressLabel.Text = L.MsgExporting(cur, tot, label);
                    }, DispatcherPriority.Background);
                    DoEvents();
                },
                () => _exportCancelled);

            btnExport.IsEnabled = true;
            btnCancel.Content = L.BtnCancel;
            btnCancel.IsEnabled = true;
            progressBar.Value = 0;
            progressPct.Text = "";
            progressLabel.Text = "";

            string destMsg = "";
            if (settings.UseSubfolders)
            {
                if (doPdf) destMsg += $"\n  PDF  ->  {Path.Combine(settings.OutputFolder, "PDF")}";
                if (doDwg) destMsg += $"\n  DWG  ->  {Path.Combine(settings.OutputFolder, "DWG")}";
                if (doIfc) destMsg += $"\n  IFC  ->  {Path.Combine(settings.OutputFolder, "IFC")}";
                if (doNwc) destMsg += $"\n  NWC  ->  {Path.Combine(settings.OutputFolder, "NWC")}";
            }
            else destMsg = "\n  " + settings.OutputFolder;

            if (result.Cancelled)
                MessageBox.Show(L.MsgExportCancelled(result.Processed, items.Count) + destMsg, L.TitleExportCancelled);
            else if (result.Errors.Count > 0)
            {
                MessageBox.Show(
                    L.MsgExportErrors(result.Processed, result.Errors.Count) + "\n\n" +
                    string.Join("\n", result.Errors) + destMsg,
                    L.TitleExportErrors);
            }
            else
                MessageBox.Show(L.MsgExportSuccess(items.Count) + destMsg, L.TitleExportSuccess);
        }

        private RasterQualityType GetRasterQuality()
        {
            // Items: 0=Low, 1=Medium, 2=High, 3=Presentation (matches L.QualityItems order)
            switch (comboQuality.SelectedIndex)
            {
                case 0: return RasterQualityType.Low;
                case 1: return RasterQualityType.Medium;
                case 3: return RasterQualityType.Presentation;
                default: return RasterQualityType.High;
            }
        }

        private ColorDepthType GetColorMode()
        {
            // Items: 0=Color, 1=Grayscale, 2=Black & White (matches L.ColorItems order)
            switch (comboColorMode.SelectedIndex)
            {
                case 1: return ColorDepthType.GrayScale;
                case 2: return ColorDepthType.BlackLine;
                default: return ColorDepthType.Color;
            }
        }

        private void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action<DispatcherFrame>(f => f.Continue = false), frame);
            Dispatcher.PushFrame(frame);
        }

    }

    // ── Simple Project Picker Dialog ────────────────────────────
    public class ProjectPickerDialog : Window
    {
        private readonly List<Document> _docs;
        private readonly ListView _list;
        public List<Document> SelectedDocs { get; } = new List<Document>();

        public class DocRow : INotifyPropertyChanged
        {
            private bool _checked;
            public bool IsChecked { get => _checked; set { _checked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked))); } }
            public string Title { get; set; }
            public event PropertyChangedEventHandler PropertyChanged;
        }

        public ProjectPickerDialog(List<Document> docs, bool isRemove = false)
        {
            _docs = docs;
            Title = isRemove ? L.TitleRemoveProjects : L.TitleAddProjects;
            Width = 460; Height = 320;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 12;
            ResizeMode = ResizeMode.NoResize;

            var grid = new WpfGrid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lbl = new TextBlock { Text = isRemove ? L.LblSelectToRemove : L.LblSelectToAdd };
            WpfGrid.SetRow(lbl, 0);
            grid.Children.Add(lbl);

            _list = new ListView { SelectionMode = SelectionMode.Single };
            var gv = new GridView();
            var col1 = new GridViewColumn { Width = 36 };
            col1.CellTemplate = new DataTemplate();
            var cb = new FrameworkElementFactory(typeof(CheckBox));
            cb.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, new WpfBinding("IsChecked"));
            col1.CellTemplate.VisualTree = cb;
            gv.Columns.Add(col1);
            var col2 = new GridViewColumn { Header = L.ColProjectPicker, Width = 360, DisplayMemberBinding = new WpfBinding("Title") };
            gv.Columns.Add(col2);
            _list.View = gv;

            var rows = docs.Select(d => new DocRow { Title = NamingHelper.DocShortTitle(d) }).ToList();
            _list.ItemsSource = rows;
            WpfGrid.SetRow(_list, 1);
            grid.Children.Add(_list);

            var btnGrid = new WpfGrid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            WpfGrid.SetRow(btnGrid, 3);

            var btnOk = new Button { Content = isRemove ? L.BtnRemove : L.BtnAdd, Width = 80, Height = 28, FontWeight = FontWeights.SemiBold };
            btnOk.Background = isRemove ? System.Windows.Media.Brushes.Red : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 115, 232));
            btnOk.Foreground = System.Windows.Media.Brushes.White;
            btnOk.BorderThickness = new Thickness(0);
            btnOk.Click += (s, e) =>
            {
                for (int i = 0; i < rows.Count; i++)
                    if (rows[i].IsChecked) SelectedDocs.Add(_docs[i]);
                DialogResult = true;
                Close();
            };
            WpfGrid.SetColumn(btnOk, 2);
            btnGrid.Children.Add(btnOk);
            grid.Children.Add(btnGrid);

            Content = grid;
        }
    }

    // ── DWG Per-Project Setup Dialog ────────────────────────────
    public class DwgPerProjectDialog : Window
    {
        private readonly List<Document> _docs;
        private readonly List<System.Windows.Controls.ComboBox> _combos = new List<System.Windows.Controls.ComboBox>();
        public Dictionary<int, string> Result { get; } = new Dictionary<int, string>();

        public DwgPerProjectDialog(List<Document> docs, string defaultSetup, Dictionary<int, string> savedSetups = null)
        {
            _docs = docs;
            Title = L.DwgPerProjTitle;
            Width = 680;
            SizeToContent = SizeToContent.Height;
            MinHeight = 120;
            MaxHeight = 700;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 12;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            ShowInTaskbar = false;

            var root = new WpfGrid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new TextBlock
            {
                Text = L.DwgPerProjHeader,
                Foreground = System.Windows.Media.Brushes.DimGray,
                TextWrapping = TextWrapping.Wrap
            };
            WpfGrid.SetRow(header, 0);
            root.Children.Add(header);

            var itemsPanel = new StackPanel { Orientation = Orientation.Vertical };
            WpfGrid.SetRow(itemsPanel, 2);
            root.Children.Add(itemsPanel);

            foreach (var d in docs)
            {
                var row = new WpfGrid { Margin = new Thickness(0, 0, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

                var lbl = new TextBlock
                {
                    Text = NamingHelper.DocShortTitle(d),
                    ToolTip = d.PathName,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(60, 64, 67)),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                WpfGrid.SetColumn(lbl, 0);
                row.Children.Add(lbl);

                var docSetups = new List<string> { ExportHelper.InSessionSetupName };
                try { docSetups.AddRange(BaseExportOptions.GetPredefinedSetupNames(d)); }
                catch { }

                var cbo = new System.Windows.Controls.ComboBox { Height = 26 };
                foreach (var s in docSetups) cbo.Items.Add(s);

                // Restore previously saved selection for this doc, then fall back to defaultSetup,
                // then prefer first named setup over In-Session.
                string saved = null;
                savedSetups?.TryGetValue(d.GetHashCode(), out saved);
                if (saved != null && docSetups.Contains(saved))
                    cbo.SelectedItem = saved;
                else if (docSetups.Contains(defaultSetup))
                    cbo.SelectedItem = defaultSetup;
                else
                    cbo.SelectedIndex = docSetups.Count > 1 ? 1 : 0;
                WpfGrid.SetColumn(cbo, 2);
                row.Children.Add(cbo);

                itemsPanel.Children.Add(row);
                _combos.Add(cbo);
            }

            var btnGrid = new WpfGrid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            WpfGrid.SetRow(btnGrid, 4);

            var btnOk = new Button
            {
                Content = L.BtnSet, Width = 90, Height = 28, FontWeight = FontWeights.SemiBold,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(26, 115, 232)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btnOk.Click += (s, e) =>
            {
                for (int i = 0; i < _docs.Count; i++)
                    Result[_docs[i].GetHashCode()] = _combos[i].SelectedItem as string ?? "";
                DialogResult = true;
                Close();
            };
            WpfGrid.SetColumn(btnOk, 2);
            btnGrid.Children.Add(btnOk);

            var btnCancel = new Button { Content = L.BtnCancel, Width = 90, Height = 28 };
            btnCancel.Click += (s, e) => Close();
            WpfGrid.SetColumn(btnCancel, 4);
            btnGrid.Children.Add(btnCancel);

            root.Children.Add(btnGrid);
            Content = root;
        }
    }

    // ── Export Profile Editor Dialog ────────────────────────────
    public class ExportProfileEditorDialog : Window
    {
        private readonly ExportSettingsProfile _currentSettings;
        private Dictionary<string, ExportSettingsProfile> _profiles;
        private readonly System.Windows.Controls.ListBox _list;
        private readonly TextBlock _infoBlock;

        public Dictionary<string, ExportSettingsProfile> ResultProfiles => _profiles;
        public string SelectedProfileName => _list.SelectedItem as string;

        private static System.Windows.Media.SolidColorBrush Brush(byte r, byte g, byte b)
            => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));

        private static readonly System.Windows.Media.SolidColorBrush _blue   = Brush(26, 115, 232);
        private static readonly System.Windows.Media.SolidColorBrush _red    = Brush(217, 48, 37);
        private static readonly System.Windows.Media.SolidColorBrush _gray   = Brush(95, 99, 104);
        private static readonly System.Windows.Media.SolidColorBrush _border = Brush(221, 225, 231);
        private static readonly System.Windows.Media.SolidColorBrush _bg     = Brush(248, 249, 250);
        private static readonly System.Windows.Media.SolidColorBrush _hover  = Brush(232, 240, 254);

        public ExportProfileEditorDialog(
            Dictionary<string, ExportSettingsProfile> profiles,
            ExportSettingsProfile currentSettings,
            string selectedName,
            bool openNew = false)
        {
            _currentSettings = currentSettings;
            _profiles = new Dictionary<string, ExportSettingsProfile>(profiles, StringComparer.OrdinalIgnoreCase);

            Title = L.GrpExportProfile;
            Width = 540;
            Height = 420;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode  = ResizeMode.NoResize;
            ShowInTaskbar = false;
            FontFamily  = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize    = 12;

            // ── Root grid ─────────────────────────────────────────
            var root = new WpfGrid { Margin = new Thickness(20, 18, 20, 18) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });              // 0 description
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });           // 1 spacer
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 2 main
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });           // 3 spacer
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });              // 4 divider
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });           // 5 spacer
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });              // 6 save/cancel

            // Row 0 – description
            var desc = new TextBlock
            {
                Text = L.ExportProfileEditorHeader,
                Foreground = _gray, FontSize = 11, TextWrapping = TextWrapping.Wrap
            };
            WpfGrid.SetRow(desc, 0);
            root.Children.Add(desc);

            // Row 2 – main two-column layout
            var mainGrid = new WpfGrid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(196) });
            WpfGrid.SetRow(mainGrid, 2);
            root.Children.Add(mainGrid);

            // ── Left: profile ListBox ─────────────────────────────
            _list = new System.Windows.Controls.ListBox
            {
                BorderBrush = _border, BorderThickness = new Thickness(1),
                ItemContainerStyle = BuildListItemStyle()
            };
            _list.SelectionChanged += (s, e) => UpdateInfo();
            WpfGrid.SetColumn(_list, 0);
            mainGrid.Children.Add(_list);

            // ── Right: info tile + action buttons ─────────────────
            var rightPanel = new StackPanel();
            WpfGrid.SetColumn(rightPanel, 2);
            mainGrid.Children.Add(rightPanel);

            _infoBlock = new TextBlock
            {
                FontSize = 11, Foreground = _gray, TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(10, 8, 10, 8), MinHeight = 50
            };
            rightPanel.Children.Add(new Border
            {
                Background = _bg, BorderBrush = _border, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3), Margin = new Thickness(0, 0, 0, 12),
                Child = _infoBlock
            });

            Button MakeBtn(string label, bool primary = false, bool destructive = false)
            {
                var b = new Button
                {
                    Content = label, Height = 28,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(10, 0, 10, 0),
                    Margin  = new Thickness(0, 0, 0, 6),
                    FontWeight = (primary || destructive) ? FontWeights.SemiBold : FontWeights.Normal,
                };
                if (primary)     { b.Background = _blue; b.Foreground = System.Windows.Media.Brushes.White; b.BorderThickness = new Thickness(0); }
                else if (destructive) { b.Background = _red;  b.Foreground = System.Windows.Media.Brushes.White; b.BorderThickness = new Thickness(0); }
                rightPanel.Children.Add(b);
                return b;
            }

            var btnNew       = MakeBtn(L.BtnNewFromCurrent, primary: true);
            var btnDuplicate = MakeBtn(L.BtnDuplicate);
            var btnRename    = MakeBtn(L.BtnRename);
            var btnDelete    = MakeBtn(L.BtnDelete, destructive: true);

            // Row 4 – divider
            var divider = new Border
            {
                BorderBrush = _border, BorderThickness = new Thickness(0, 1, 0, 0)
            };
            WpfGrid.SetRow(divider, 4);
            root.Children.Add(divider);

            // Row 6 – Save / Cancel
            var btnRow = new WpfGrid();
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var btnOk = new Button
            {
                Content = L.BtnSave, Width = 90, Height = 28, FontWeight = FontWeights.SemiBold,
                Background = _blue, Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0)
            };
            btnOk.Click += (s, e) => { DialogResult = true; Close(); };
            WpfGrid.SetColumn(btnOk, 1);
            btnRow.Children.Add(btnOk);
            var btnCancel = new Button { Content = L.BtnCancel, Width = 90, Height = 28 };
            btnCancel.Click += (s, e) => Close();
            WpfGrid.SetColumn(btnCancel, 3);
            btnRow.Children.Add(btnCancel);
            WpfGrid.SetRow(btnRow, 6);
            root.Children.Add(btnRow);

            Content = root;

            // Populate list
            RefreshList(selectedName);

            // ── Button handlers ───────────────────────────────────
            btnNew.Click += (s, e) =>
            {
                string name = AskName(L.MsgExportProfileName);
                if (name == null) return;
                if (_profiles.ContainsKey(name) &&
                    MessageBox.Show(L.MsgExportProfileExists, L.GrpExportProfile,
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
                var p = Clone(_currentSettings); p.Name = name;
                _profiles[name] = p;
                RefreshList(name);
            };

            btnDuplicate.Click += (s, e) =>
            {
                string src = _list.SelectedItem as string;
                if (src == null) return;
                string name = AskName(L.MsgExportProfileName, src + " (2)");
                if (name == null) return;
                if (_profiles.ContainsKey(name) &&
                    MessageBox.Show(L.MsgExportProfileExists, L.GrpExportProfile,
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
                var p = Clone(_profiles[src]); p.Name = name;
                _profiles[name] = p;
                RefreshList(name);
            };

            btnRename.Click += (s, e) =>
            {
                string old = _list.SelectedItem as string;
                if (old == null) return;
                string name = AskName(L.MsgExportProfileName, old);
                if (name == null || name == old) return;
                if (_profiles.ContainsKey(name) &&
                    MessageBox.Show(L.MsgExportProfileExists, L.GrpExportProfile,
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
                var p = _profiles[old]; p.Name = name;
                _profiles.Remove(old); _profiles[name] = p;
                RefreshList(name);
            };

            btnDelete.Click += (s, e) =>
            {
                string name = _list.SelectedItem as string;
                if (name == null) return;
                if (MessageBox.Show(string.Format(L.MsgConfirmDeleteExportProfile, name),
                        L.GrpExportProfile, MessageBoxButton.YesNo, MessageBoxImage.Warning)
                    != MessageBoxResult.Yes) return;
                _profiles.Remove(name);
                RefreshList(null);
            };

            if (openNew)
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,
                    new Action(() => btnNew.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
        }

        private static Style BuildListItemStyle()
        {
            var style = new Style(typeof(ListBoxItem));
            style.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));

            // ControlTemplate: transparent Border wrapping ContentPresenter
            var borderFac = new FrameworkElementFactory(typeof(Border));
            borderFac.Name = "bd";
            borderFac.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));
            borderFac.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            var cpFac = new FrameworkElementFactory(typeof(ContentPresenter));
            borderFac.AppendChild(cpFac);
            var ct = new ControlTemplate(typeof(ListBoxItem)) { VisualTree = borderFac };

            // Selected → blue background, white text
            var selTrig = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            selTrig.Setters.Add(new Setter(Border.BackgroundProperty, Brush(26, 115, 232), "bd"));
            selTrig.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty, System.Windows.Media.Brushes.White));
            ct.Triggers.Add(selTrig);

            // Hover (not selected) → light blue
            var hoverTrig = new MultiTrigger();
            hoverTrig.Conditions.Add(new Condition(UIElement.IsMouseOverProperty, true));
            hoverTrig.Conditions.Add(new Condition(ListBoxItem.IsSelectedProperty, false));
            hoverTrig.Setters.Add(new Setter(Border.BackgroundProperty, Brush(232, 240, 254), "bd"));
            ct.Triggers.Add(hoverTrig);

            style.Setters.Add(new Setter(System.Windows.Controls.Control.TemplateProperty, ct));
            return style;
        }

        private void RefreshList(string selectName)
        {
            _list.ItemsSource = null;
            _list.ItemsSource = _profiles.Keys.ToList();
            if (selectName != null && _profiles.ContainsKey(selectName))
                _list.SelectedItem = selectName;
            else if (_profiles.Count > 0)
                _list.SelectedIndex = 0;
            else
                UpdateInfo();
        }

        private void UpdateInfo()
        {
            if (_infoBlock == null) return;
            string name = _list.SelectedItem as string;
            if (name == null || !_profiles.TryGetValue(name, out var p))
            {
                _infoBlock.Text = "—";
                return;
            }
            var fmts = new List<string>();
            if (p.FormatPDF) fmts.Add("PDF");
            if (p.FormatDWG) fmts.Add("DWG");
            if (p.FormatIFC) fmts.Add("IFC");
            if (p.FormatNWC) fmts.Add("NWC");
            string naming = string.IsNullOrEmpty(p.NamingProfile) ? "—" : p.NamingProfile;
            _infoBlock.Text = L.ExportProfileSummary(fmts.Count > 0 ? string.Join(", ", fmts) : L.NoneLower, naming);
        }

        private string AskName(string prompt, string defaultValue = "")
        {
            var dlg = new Window
            {
                Title = L.GrpExportProfile, Width = 400, SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this,
                ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 12
            };
            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) });
            var tb = new System.Windows.Controls.TextBox
            {
                Text = defaultValue, Height = 28,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4, 0, 4, 0), Margin = new Thickness(0, 0, 0, 14)
            };
            panel.Children.Add(tb);
            var row = new WpfGrid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var ok = new Button
            {
                Content = L.BtnSave, Width = 80, Height = 28, FontWeight = FontWeights.SemiBold,
                Background = _blue, Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            ok.Click += (s, e) => { if (!string.IsNullOrWhiteSpace(tb.Text)) { dlg.DialogResult = true; dlg.Close(); } };
            WpfGrid.SetColumn(ok, 2);
            row.Children.Add(ok);
            panel.Children.Add(row);
            tb.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter && !string.IsNullOrWhiteSpace(tb.Text))
                { dlg.DialogResult = true; dlg.Close(); }
            };
            dlg.Content = panel;
            dlg.Loaded += (s, e) => { tb.Focus(); tb.SelectAll(); };
            return dlg.ShowDialog() == true ? tb.Text.Trim() : null;
        }

        private static ExportSettingsProfile Clone(ExportSettingsProfile src)
        {
            var p = new ExportSettingsProfile();
            foreach (var line in src.ToLines())
            {
                int eq = line.IndexOf('=');
                if (eq > 0) p.SetKey(line.Substring(0, eq), line.Substring(eq + 1));
            }
            return p;
        }
    }
}
