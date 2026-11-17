# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

Open `JFTools/JFTools.csproj` in Visual Studio 2022 and press **Ctrl+Shift+B**, or use MSBuild from the command line:

```
msbuild JFTools/JFTools.csproj /p:Configuration=Release
```

Output goes to `JFTools/bin/Debug/` or `JFTools/bin/Release/`.

**Prerequisites:** .NET Framework 4.8, Visual Studio 2022, Revit 2022–2024 installed. Revit 2025+ runs on .NET 8 and is NOT supported yet (needs a net8.0-windows build; also `ElementId.IntegerValue` was removed in the Revit 2026 API — used in ExcelHelper, RevisionEditorDialog and ExportHelper).

**Revit API path:** Set in `.csproj` via `<RevitApiPath>` (currently set to `D:\Autodesk\Programas\Revit 2023`). Adjust if your Revit is in a different location. `RevitAPI.dll` and `RevitAPIUI.dll` are referenced from there with `<Private>false</Private>` (not copied to output).

There are no automated tests — the add-in must be tested inside Revit.

## Installation (manual)

1. Copy `JFTools.dll` and `export_profiles.txt` to a folder (e.g., `C:\JFTools\`).
2. Copy `JFTools.addin` to `%APPDATA%\Autodesk\Revit\Addins\<version>\`.
3. Edit the `<Assembly>` path in the `.addin` file to point to the DLL.
4. Restart Revit — the **JFTools** ribbon tab appears with the **Ferramentas** panel. No license or activation is required (the plugin is free and open source).

**Testing after build:** The Inno Setup installer copies the DLL to `C:\ProgramData\JFTools\`. To test without reinstalling, overwrite `C:\ProgramData\JFTools\JFTools.dll` with the newly built DLL from `JFTools\bin\Debug\` and restart Revit.

## Installer

Installer files live in `Installer/`. The script is `Installer/JFTools-Setup.iss` and the compiled output goes to `Installer/Output/JFTools-Setup.exe`.

Before compiling with Inno Setup 6, copy the following files into `Installer/`:
- `JFTools.dll` — from `JFTools/bin/Release/`
- `export_profiles.txt` — from `JFTools/export_profiles.txt`

`icon.ico` is already in `Installer/`. The `.addin` file is generated dynamically by the installer script — no need to place one manually.

The installer writes the `.addin` file only to the 2022, 2023 and 2024 Revit Addins folders (2025+ cannot load a .NET Framework 4.8 DLL).

## Architecture

This is a **Revit external add-in** — a class library (DLL) loaded by Revit at startup. There are no standalone executables. The only NuGet package is EPPlus 4.5.3 (used by the Excel tool).

**Entry points (defined in `JFTools.addin`):**

- `JFTools.JFToolsApp` (`IExternalApplication`) — runs on Revit startup; creates the "JFTools" ribbon tab with the "Ferramentas" panel containing one button: Export (PDF/DWG/IFC/NWC). The button icon is the embedded PNG resource `impressora.png`. `ExcelCommand` is compiled but currently has no ribbon button.
- `JFTools.ExportSheetsCommand` (`IExternalCommand`, `TransactionMode.Manual`) — invoked when the button is clicked; opens `MainWindow` as a modal WPF dialog parented to Revit's main window.

**Data flow:**

1. `MainWindow` (Views/) collects the active Revit document(s) and enumerates sheets using `FilteredElementCollector`. Users can add additional open documents (non-linked, non-family) via `ProjectPickerDialog`.
2. Sheets are wrapped in `SheetItem` (Models/) which implements `INotifyPropertyChanged` for WPF binding. Revision data is read using `BuiltInParameter.SHEET_CURRENT_REVISION` for the alphanumeric revision and the `Revision` class properties (`RevisionDate`, `Description`) directly — not via `LookupParameter` string lookup, which is locale-dependent.
3. `NamingHelper` (Helpers/) loads `export_profiles.txt` (key=pattern pairs) and resolves `{Token}` placeholders against Revit element parameters and `ProjectInformation`. Falls back to `{SheetNumber}_{SheetName}` if the pattern produces an empty string. Parameter lookup is locale-independent: standard parameters are resolved via a `BuiltInParameter` map first; custom parameters fall back to `LookupParameter` by name. `Current Revision Date` and `Current Revision Description` are resolved via `ViewSheet.GetCurrentRevision()` + the `Revision` element, same pattern as `SheetItem`.
4. `ExportHelper` (Helpers/) drives `Document.Export(...)` for PDF (`PDFExportOptions`), DWG (`DWGExportOptions`), IFC (`IFCExportOptions`), and NWC (`NavisworksExportOptions`). PDF can be exported per-sheet or combined into a single file. DWG export uses Revit predefined export setups; when multiple documents are loaded, each can have its own setup via `DwgSetupPerDoc` (populated by the "Per Project" button). IFC and NWC each export one file per unique document. Output folder structure is controlled by two independent flags: `UseProjectSubfolders` (adds `/ProjectName/` level) and `UseSubfolders` (adds `/PDF/`, `/DWG/`, `/IFC/`, or `/NWC/` level); project comes before format: `OutputFolder/ProjectName/NWC/`.

**`export_profiles.txt` format:**

```
ProfileName={Token1}_{Token2}
```

Tokens are Revit parameter names (e.g., `{Sheet Number}`, `{Sheet Name}`, any custom parameter). The file is stored at `%APPDATA%\JFTools\export_profiles.txt` (user-writable, no admin required). On first run it is seeded from the copy installed alongside the DLL in `C:\ProgramData\JFTools\`. Profiles are edited via `NamingProfileEditorDialog` (the "Edit" button in the Output tab) — Notepad is no longer used.

## UI Language / Localization

The plugin is **bilingual: English and Brazilian Portuguese**. Language is selected automatically at command execution time:

- **`ExportSheetsCommand.Execute`** and **`ExcelCommand.Execute`**: call `L.Init(commandData.Application.Application.Language == LanguageType.Brazilian_Portuguese)` before creating any dialog. This is the reliable, per-command init.
- **`JFToolsApp.OnStartup`**: calls `L.Init(...)` using `Thread.CurrentThread.CurrentUICulture` (best available at startup) to localize the ribbon panel name and tooltips.

**`JFTools/Helpers/L.cs`** — the single source of truth for all user-facing strings. Public static properties return English or Portuguese depending on the `_pt` flag set by `L.Init(bool ptBR)`.

**Key rule:** `L.Init()` must be called **before** `InitializeComponent()` of any Window, because XAML uses `{x:Static l:L.PropertyName}` markup — resolved once at BAML load time (i.e., when `InitializeComponent()` runs). All XAML files add `xmlns:l="clr-namespace:JFTools.Helpers"` to the root `<Window>`.

**Adding a new string:** Add a property to `L.cs` using `S(string en, string pt)`, then use `{x:Static l:L.PropertyName}` in XAML or `L.PropertyName` in code-behind. Never hardcode user-visible English strings in XAML or .cs files.

**Combo items that depend on language** (Quality, Color Mode, IFC Linked Files, IFC Space Boundaries) are set via `ItemsSource = L.XxxItems` in code-behind, not in XAML. Sort comparison in `GetSortValue()` uses `L.Col*` properties, not hardcoded strings, so sorting works in both languages. `GetRasterQuality()` and `GetColorMode()` use `SelectedIndex` (not string comparison) so they are language-independent.

**Handling PT text overflow in fixed-width XAML containers:** Portuguese strings are often longer than English. When adding or editing strings, check for overflow against fixed-width buttons and label columns. Prefer:
- **Abbreviate** the PT string when it is significantly longer (e.g., "Configuração de Exportação" → "Config. de Exportação"; "Posicionamento do Terreno" → "Posic. do Terreno").
- **Widen the container** slightly when the difference is small (a few characters). Use `Width="Auto"` for label columns in grids where the remaining space goes to a `Width="*"` combo — this avoids both clipping and unnecessary wasted space.
- Never use `TextTrimming` on labels to hide overflow silently.

## UI Conventions

**Button styles** — two variants used throughout, must be applied consistently:
- **Primary** (`PrimaryBtn` style): blue `#1A73E8` background, white text, no border, `FontWeight=SemiBold`. Used for the main confirm action (Export, Save, Add, Set).
- **Destructive**: red `#D93025` background, white text, `BorderThickness=0`, `FontWeight=SemiBold`. Used for Cancel and Delete. Never use red foreground-only; always fill the background.
- Standard buttons (secondary): default WPF style with default border.

## UI Structure

The main window (`MainWindow.xaml`) uses a custom-styled `TabControl` (flat Google-style: white header bar, blue underline on selected tab, no traditional tab borders) with three tabs:

- **Selection** — sheet list with filter, project chips, bulk select/deselect, and the "＋ Add Revision" button
- **Export Options** — format card selector (PDF, DWG, IFC, NWC); clicking a card shows that format's options panel. DWG has a "✎ Per Project" button when multiple documents are loaded. IFC has a nested 2-tab interface: **Basic Settings** (General + Additional Content + Level of Detail + Property Sets) and **Advanced** (see IFC Export below).
- **Output** — naming profile ("Edit" opens `NamingProfileEditorDialog`), destination folder with two subfolder options (by project, by format), progress bar, and Export/Cancel buttons

The sheet `ListView` uses `SelectionMode="Extended"` with a `PreviewMouseLeftButtonDown` handler that implements bulk checkbox toggle: clicking a checkbox while multiple rows are selected applies the toggle to all selected rows.

**DWG panel layout** (`panelDWG`, a `ScrollViewer`):
1. Export setup combo + "✎ Per Project" button (full width)
2. `chkBindOle` — "Bind images as OLE" primary checkbox (full width, above columns)
3. Two-column `Grid`:
   - **Left — General GroupBox**: AutoCAD file version combo; `chkDwgViewXRef` ("Export views on sheets and links as external references"); 6 checkboxes (rooms/areas, coincident lines, non-plot layers, hide reference planes, hide scope boxes, hide unreferenced view tags); non-plot suffix textbox; ACA objects combo; layer property overrides combo
   - **Right column** — 5 stacked GroupBoxes:
     - **Colors**: 3 radio buttons (`radioDwgColorTrue/Index/PerView`, `GroupName="DwgColor"`)
     - **Text and Fonts**: 2 radio buttons (`radioDwgTextExact/Approx`, `GroupName="DwgText"`)
     - **Solids**: 2 radio buttons (`radioDwgSolidsPolymesh/ACIS`, `GroupName="DwgSolids"`)
     - **Units and Coordinates**: units combo + 2 radio buttons (`radioDwgCoordProject/Shared`, `GroupName="DwgCoord"`)
     - **Lines**: 3 radio buttons (`radioDwgLineViewScale/Model/Paper`, `GroupName="DwgLine"`)

## Implemented Features

### Revision Editor (`RevisionEditorDialog`)
Opened via "＋ Add Revision" on the Selection tab. Requires at least one sheet to be checked. Provides:
- A list of all revisions across the checked sheets' documents, with project filter
- **Add**: inline form with Date, Description, Issued By, Issued To fields; scope option ("All projects" or "Filtered project only"); checkbox to apply the new revision to selected sheets immediately via `SetAdditionalRevisionIds`
- **Edit**: edits the fields of an existing revision (filtered project only)
- **Remove**: deletes a revision with a `Transaction`
- On close, calls `RefreshSheetList(keepChecked: true)` so the Rev/Date/Description columns update

### Excel Tool — Parameter Round-Trip (`ExcelDialog` / `ExcelHelper`)
Two-tab dialog opened via the "Excel" ribbon button (`ExcelCommand`). Purpose: export a Revit schedule to Excel, edit parameter values outside Revit, import back to update the elements.

**Export tab** — selects a project and a single schedule, picks a destination `.xlsx` file. `ExcelHelper.ExportScheduleForEditing` collects elements visible in the schedule via `FilteredElementCollector(doc, schedule.Id)`, reads each field value via `LookupParameter` / `AsValueString()`, and writes:
- Row 1: display headers (`ColumnHeading` from Revit), styled bold/blue (`#D9E1F2`)
- Row 2 (hidden): internal parameter names used as the import key
- Row 3+: data rows; column 1 is `UniqueId` (hidden, grey/italic), remaining columns are `AsValueString()` values
- Hidden and formula-only fields are filtered out
- Rows sorted by the schedule's sort/group fields
- Pane frozen at row 3 + column 2; auto-filter enabled on header row

**Import tab** — selects an exported `.xlsx` file, shows available worksheets (one per exported schedule), selects target project. `ExcelHelper.ImportParametersFromExcel` reads the UniqueId column, calls `doc.GetElement(uniqueId)`, then for each other column calls `elem.LookupParameter(fieldName)` and sets the value:
- `StorageType.String` → `param.Set(value)`
- `StorageType.Integer` → parses "Yes"/"No" as 1/0, otherwise `int.TryParse`
- `StorageType.Double` → skipped (display strings with units cannot be reliably round-tripped without knowing the ForgeTypeId; document this to user)
- Read-only parameters are skipped silently
Returns a `List<string>` result log (updated count, skipped count, per-row errors).

**Key API notes:**
- `FilteredElementCollector(doc, scheduleId)` — collects body elements of the schedule (respects filters); may throw for Key Schedules, handle with try/catch fallback to category collector
- `Element.UniqueId` — stable across sessions, used as the import key; `doc.GetElement(string uniqueId)` retrieves element
- `ScheduleDefinition.GetField(i).GetName()` — internal parameter name (row 2); `ScheduleField.ColumnHeading` — display name (row 1)
- `ScheduleDefinition.GetFieldCount()` — field count; hidden/formula fields filtered via `IsHidden`/`HasFormula`
- `param.IsReadOnly` — skip before setting
- All writes wrapped in a single `Transaction`
- Import matches columns by row 2 (parameter name), with fallback to row 1 (display name)

---

### [Archived] Excel Tool — Visual Import as Drafting View (replaced)
The previous Import implementation created a Drafting View replicating the Excel table visually. Kept here for reference in case the feature is restored:
- Scale 1:1; background colors via `FilledRegion` (one `FilledRegionType` per unique color, named `JFT_Fill_R_G_B`); grid lines via `NewDetailCurve` with merged-cell suppression; text via `TextNote.Create`
- One `TextNoteType` per unique (font, size, bold, italic) combination, cached per import
- Merged cells: `ws.MergedCells` iteration; `hLine[b,c]`/`vLine[r,b]` suppression arrays; segment-merging draw loop; FilledRegion and TextNote span full merge extent
- Conversion: 1 Excel char unit = 7/864 ft; 1 pt = 1/864 ft
- `cell.Text` used (not `cell.Value.ToString()`) to preserve number formatting
- `EncKey(r,c)` = `((long)r << 16) | (uint)c` used as dictionary key for merge lookup

### Naming Profile Editor (`NamingProfileEditorDialog`)
Opened via the "Edit" button in the Output tab. Replaces the previous Notepad-based editing workflow. Layout:
- **Profile bar** (top): ComboBox + New / Duplicate / Rename / Delete buttons. Delete is styled red (destructive pattern).
- **Available Parameters** (left GroupBox, TabControl):
  - *Sheet* tab: standard sheet params (Sheet Number, Sheet Name, Drawn By, etc.) + any custom params found on the first sheet. Sample value from the first sheet shown in italic.
  - *Project* tab: standard project info params (Project Name, Project Number, Client Name, etc.) + any custom params from `ProjectInformation`.
  - Selection highlight: `#1A73E8` (primary blue), sample text turns light blue (`#C5D8F8`).
- **Add → button** (center): inserts selected param after currently selected token; double-click on a param also adds it.
- **Naming Pattern** (right GroupBox): vertical list of tokens. Each row shows a small badge (**P** blue for parameter, **S** gray for separator) and the token display name. Selected token highlighted in `#1A73E8` with white text. Buttons: ↑ ↓ ✕ Remove (Delete key also removes). Quick separator buttons: `_` `-` `.` `/` SP. Custom text field + Add button.
- **Pattern / Preview bars** (bottom): raw pattern string in Consolas + resolved filename preview using the first available sheet.
- On Save: writes all profiles to `%APPDATA%\JFTools\export_profiles.txt` and returns `ResultProfiles` to `MainWindow`, which refreshes the combo and preview without reloading.

### DWG Per Project (`DwgPerProjectDialog`)
When more than one document is loaded, `comboDWG` is disabled and "✎ Per Project" is enabled. The dialog shows one DWG export setup combo per document. The resulting `Dictionary<int, string>` (keyed by `doc.GetHashCode()`) is stored in `_dwgSetupPerDoc` and passed to `ExportHelper` at export time.

### DWG Export Settings (`DwgExportConfig`)
The DWG panel always shows a full settings editor regardless of which setup is selected. The setup combo includes `<In-Session>` (first entry, sentinel `ExportHelper.InSessionSetupName`) plus all named setups from Revit.

**Setup switching state management:**
- `_inSessionConfig` field (`DwgExportConfig`) stores the user's In-Session panel state.
- When switching **away** from `<In-Session>`: `e.RemovedItems[0]` is `<In-Session>` → `_inSessionConfig = ReadDwgConfig()` saves the current panel.
- When switching **to** `<In-Session>`: `LoadDwgConfig(_inSessionConfig)` restores it.
- When switching **to a named setup**: `DWGExportOptions.GetPredefinedOptions(doc, name)` → `DwgExportConfig.FromOptions(opt)` → `LoadDwgConfig(cfg)`.
- `_inSessionConfig` is never overwritten when switching between named setups.

**At export time:** For `<In-Session>`, `DwgConfig.ToExportOptions()` creates a fresh `DWGExportOptions`. For named setups, `GetPredefinedOptions` loads the saved setup (preserving layer/linetype/hatch mappings), then `DwgConfig.ApplyToOptions(opt)` overlays the panel values — user edits always take precedence.

**UI controls → `DwgExportConfig` properties → `DWGExportOptions` API** (only R2018/R2013/R2010/R2007 exist; R2004/R2000 do not):

*General GroupBox (left column):*
- `comboDwgVersion` → `VersionIndex` → `FileVersion` (`ACADVersion.R2018/R2013/R2010/R2007`)
- `chkDwgViewXRef` checkbox — **checked = export as XREF** → `MergedViews = false`; unchecked = merge views → `MergedViews = true`
- `chkDwgAreas` → `ExportingAreas`
- `chkDwgCoincident` → `PreserveCoincidentLines`
- `chkDwgNonplot` → `MarkNonplotLayers`
- `chkDwgHideRef` → `HideReferencePlane`
- `chkDwgHideScope` → `HideScopeBox` (singular — no trailing 's')
- `chkDwgHideTags` → `HideUnreferenceViewTags` (`Unreference`, not `Unreferenced`)
- `txtDwgNonplotSuffix` → `NonplotSuffix`
- `comboDwgACA` → `ACAIndex` → `ACAPreference` (`ACAObjectPreference.Object/Geometry`)
- `comboDwgPropOverrides` → `PropOverridesIndex` → `PropOverrides` (`PropOverrideMode.ByEntity/ByLayer/NewLayer`)

*Colors GroupBox (radio buttons):* `ColorIndex` → `Colors` (`ExportColorMode.TrueColor[0]/IndexColors[1]/TrueColorPerView[2]`)

*Text and Fonts GroupBox (radio buttons):* `TextIndex` → `TextTreatment` (`Exact[0]/Approximate[1]`)

*Solids GroupBox (radio buttons):* `SolidsIndex` → `ExportOfSolids` (`Polymesh[0]/ACIS[1]`)

*Units and Coordinates GroupBox:*
- `comboDwgUnits` → `UnitsIndex` → `TargetUnit` (`ExportUnit.Default[0]/Foot[1]/Inch[2]/Millimeter[3]/Centimeter[4]/Meter[5]`)
- radio buttons → `SharedCoords` → `SharedCoords` (not `SharedCoordinates`)

*Lines GroupBox (radio buttons):* `LineScalingIndex` → `LineScaling` (`ViewScale[0]/ModelSpace[1]/PaperSpace[2]`)

`DWGExportOptions` does **not** have `HideCropBoundaries` (that exists only on `PDFExportOptions`).

**Combos that remain as combos** (too many options for radio buttons): `DwgVersionItems` (4), `DwgUnitsItems` (6), `DwgACAItems` (2), `DwgPropOverridesItems` (3). All others use radio buttons with `GroupName` to enforce mutual exclusion within a ScrollViewer.

### OLE Image Binding (`OleImageBinder`)
Post-export processor that converts every `AcDbRasterImage` entity in a DWG to an embedded OLE object, so images survive as self-contained data instead of external file references.

**Trigger:** Called from `ExportHelper` after DWG export when the user has the option enabled. Runs unattended — AutoCAD GUI is kept hidden.

**Process (per DWG file):**
1. Attaches to a running AutoCAD or launches a new hidden instance via COM (`Marshal.GetActiveObject("AutoCAD.Application")` / `Activator.CreateInstance`)
2. Opens the DWG with `acad.Documents.Open(dwgPath, false)`, immediately sets `acad.Visible = false`
3. Disables backup files: `SetSystemVariable("ISAVEBAK", 0)`
4. For each `AcDbRasterImage` entity (model space + all paper space layouts):
   - Reads display position: `e.Origin` (double[3]) — do NOT use `e.InsertionPoint` (doesn't exist on raster images)
   - Reads display size: `e.ImageWidth` / `e.ImageHeight` (drawing units) — do NOT use `e.Width`/`e.Height` (returns pixel dimensions)
   - Converts the source PNG/JPEG to an EMF (Enhanced Metafile) via `System.Drawing.Imaging.Metafile` with `EmfType.EmfPlusDual`
   - Places the EMF on the Windows clipboard via `SetClipboardData(CF_ENHMETAFILE=14, hemf)` using raw Win32 APIs
   - Starts a background thread polling for the AutoCAD "OLE Text Size" dialog (and PT/DE/FR/ES variants) and auto-dismisses it via `PostMessage(hwnd, WM_COMMAND, IDOK, 0)`
   - Sends `PASTECLIP\n0,0\n` to AutoCAD — this pastes the EMF as an `AcDbOle2Frame`
   - Repositions and resizes the new OLE frame: `ole.Width = img.W; ole.Height = img.H; ole.InsertionPoint = new double[]{ img.X, img.Y, 0 }`
   - Deletes the original raster entity: `img.Entity.Delete()`
   - Queues the source image file path for deletion after `doc.Close()`
5. Saves and closes the DWG; deletes queued image files after close (AutoCAD releases the file lock on close)
6. Removes sidecar files: `.bak`, `.pcp`
7. If AutoCAD was launched by us (not pre-existing), quits it after processing

**Key COM property gotchas:**
- `AcDbRasterImage.Origin` → position (not `InsertionPoint`)
- `AcDbRasterImage.ImageWidth` / `ImageHeight` → display size in drawing units (use these, not `Width`/`Height`)
- `AcDbOle2Frame.InsertionPoint`, `Width`, `Height` → position and size (writable)
- Finding the new OLE: snapshot all entity handles before `PASTECLIP`, then scan for new `AcDbOle2Frame`/`AcDbOleFrame` handles

**Why CF_ENHMETAFILE:** Pasting a bitmap (`CF_BITMAP`) creates another `AcDbRasterImage`, not an OLE frame. EMF format triggers the OLE Package handler, which creates a proper embedded `AcDbOle2Frame`.

## Implemented Features (continued)

### IFC Export
IFC is a third format card in the Export Options tab. Selecting it reveals a nested `TabControl` with two tabs (**Basic Settings** and **Advanced**), all settings bound to `IfcExportConfig` (a plain settings class defined in `ExportHelper.cs`).

**IFC Options tabs (2 tabs):**

- **Basic Settings** — two-column layout:
  - *Left — General:* IFC Version (`IFC2x3CV2`, `IFC2x3`, `IFC4RV`, `IFC4DTV`, `IFC4`, `IFCCOBIE`, `IFC2x3FM`, `IFC2x3BFM`, `IFC2x2`, `IFCBCA`, `IFC4x3`), File Type (`Ifc`, `IfcXML`, `IfcZIP`, `IfcXMLZIP`), Space Boundaries (0/1/2), Split walls/columns/ducts by level checkbox, Include steel elements checkbox
  - *Left — Additional Content:* Linked Files combo (`Do not export linked files` / `Export each linked model as a separate IFC file`), Export 2D plan view elements, Export only elements visible in view, Export rooms in 3D view
  - *Right — Level of Detail:* tessellation slider (0.0–1.0)
  - *Right — Property Sets:* Revit property sets, IFC common property sets, base quantities, schedules as property sets, material property sets, user-defined property sets (+ Browse), parameter mapping table (+ Browse)
- **Advanced** — scrollable list: Export parts as building elements, Allow use of mixed Solid Model representation, Use active view when creating geometry, Use family and type name for reference, Use 2D room boundaries for room volume, Include IFCSITE elevation in site local placement origin, Store IFC GUID in element parameter, Keep tessellated geometry as triangulation, Use type name only for IFCType name, Use visible Revit name as IFCEntity name; Site Placement combo, Exchange Requirement combo

**How it works:** IFC exports one file per unique document (not per sheet). `ExportHelper.ExportIfcDocument` builds an `IFCExportOptions` object via reflection:
1. Loads `Autodesk.IFC.Export.UI.dll` from `<RevitDir>\AddIns\IFCExporterUI\` at runtime
2. Calls `BIM.IFC.Export.UI.IFCExportConfiguration.CreateDefaultConfiguration()` and maps all `IfcExportConfig` properties onto it
3. Calls `config.UpdateOptions(opts, ElementId.InvalidElementId)` to apply settings
4. Falls back to basic `IFCExportOptions` with only version set if the DLL is not found
5. Exports the main document via `Document.Export()`

**Linked file export (Separate mode):**
- Collects all conformally loaded link instances (non-scaled, non-reflected — determinant ≈ 1.0)
- Groups by linked file path to avoid exporting duplicates
- Each linked document is exported in a separate transaction with options: `ExportLinkId`, `ExportLinkInstanceFileName`, `ExportLinkInstanceTransform`
- Origin is pre-scaled to the linked document's display units
- Follows the pattern from `revit-ifc Release_23.x.x`

**IFC GUID generation:**
- Converts .NET `Guid` to the buildingSMART 22-char Base64 IFC format (6 numeric groups)
- Tries `IFC_GUID` parameter on the element first, then derives from `Element.UniqueId`, falls back to random GUID

**Key design choices:**
- No compile-time dependency on the IFC UI DLL — loaded via reflection, so the build never requires it present
- Linked file export (Separate mode) only works when `Autodesk.IFC.Export.UI.dll` is available (bundled with Revit)
- IFC is excluded from per-sheet progress counting; it adds one step per unique document to the total
- Output folder follows the same subfolder logic: `OutputFolder/[ProjectName/]IFC/`
- The output filename is `DocShortTitle` (document name without path or extension)

### NWC Export (Navisworks)
NWC is a fourth format card in the Export Options tab. Like IFC, it is per-document (not per-sheet): one `.nwc` file per unique loaded document.

**How it works:** `ExportHelper.ExportNwcDocument` builds a `NavisworksExportOptions` object and calls `Document.Export(folder, fileName, opts)` (void — no return value; exceptions propagate as errors). `fileName` is `DocShortTitle` without extension; Revit appends `.nwc` automatically.

**Exposed options (NWC panel):**
- **Coordinates** combo: Shared Coordinates / Internal Origin → maps to `NavisworksCoordinates.Shared` / `Internal`
- **Parameters** combo: None / Model Properties / All Properties → maps to `NavisworksParameters` enum (`None`, `ModelProperties`, `AllProperties`)
- **Export linked files** checkbox → `opts.ExportLinks`
- **Convert element properties** checkbox (default on) → `opts.ConvertElementProperties`
- **Divide into levels** checkbox → `opts.DivideFileIntoLevels`

**Key design choices:**
- `ExportScope` is always `NavisworksExportScope.Model` (no view picking — batch workflow)
- NWC is excluded from per-sheet progress counting; adds one step per unique document
- Output folder: `OutputFolder/[ProjectName/]NWC/`
- `NwcExportConfig` + `ExportNwc` / `NwcDocuments` fields mirror the IFC pattern in `ExportSettings`

### Export Settings Profiles

Saves and loads complete export configurations (all format settings + output options) as named profiles.

**What is saved per profile:**
- Active formats (`Formats=PDF,DWG` etc.)
- PDF settings: `PDF.Quality`, `PDF.ColorMode`, `PDF.Combined`, `PDF.HideCrop`, `PDF.HideUnderlay`, etc.
- DWG settings: `DWG.Setup` (setup name), plus all `DwgExportConfig` fields (`DWG.VersionIndex`, `DWG.ColorIndex`, `DWG.TextIndex`, `DWG.SolidsIndex`, `DWG.SharedCoords`, `DWG.LineScalingIndex`, `DWG.BindOle`, all checkbox bools, `DWG.NonplotSuffix`, `DWG.ACAIndex`, `DWG.PropOverridesIndex`, `DWG.MergedViews`, `DWG.ExportingAreas`, `DWG.PreserveCoincidentLines`, `DWG.MarkNonplotLayers`, `DWG.HideReferencePlane`, `DWG.HideScopeBox`, `DWG.HideUnreferenceViewTags`, `DWG.UnitsIndex`)
- IFC settings: all `IfcExportConfig` fields prefixed `IFC.*`
- NWC settings: all `NwcExportConfig` fields prefixed `NWC.*`
- Output options: `Output.UseProjectSubfolders`, `Output.UseFormatSubfolders`
- Naming profile reference: `NamingProfile=<profile name>`
- **Not saved:** output folder path (machine/project-specific)

**Naming profiles** remain a separate concept in `export_profiles.txt`, edited via `NamingProfileEditorDialog`. Export settings profiles only reference a naming profile by name.

**File format:** INI-style flat text. One section per profile, section header `[Profile:Name]`:

```ini
[Profile:Full Export]
NamingProfile=Default
Formats=PDF,DWG
PDF.Quality=2
PDF.ColorMode=0
PDF.Combined=false
DWG.Setup=<In-Session>
DWG.VersionIndex=0
DWG.ColorIndex=0
Output.UseProjectSubfolders=false
Output.UseFormatSubfolders=true

[Profile:PDF Only]
NamingProfile=Default
Formats=PDF
PDF.Quality=2
PDF.ColorMode=0
PDF.Combined=true
Output.UseProjectSubfolders=true
Output.UseFormatSubfolders=false
```

**Storage:** `%APPDATA%\JFTools\export_settings.ini` (separate from `export_profiles.txt`). Seeded from `C:\ProgramData\JFTools\export_settings.ini` on first run, same pattern as naming profiles.

**`ExportSettingsHelper`** (`JFTools/Helpers/ExportSettingsHelper.cs`) — static methods:
- `LoadProfiles(path)` → `Dictionary<string, ExportSettingsProfile>`
- `SaveProfiles(path, profiles)` — writes all sections
- `GetProfilesPath()` — returns `%APPDATA%\JFTools\export_settings.ini`, seeds from ProgramData if missing

**`ExportSettingsProfile`** — plain class with typed fields for all ~80 settings. Serialized/deserialized via `ToLines()` / `SetKey(key, val)` (no external dependencies). `Clone()` helper uses a `ToLines()`/`SetKey()` round-trip.

**UI integration** — profile selector floats over the tab header bar (top-right overlay, `Panel.ZIndex=10`):
- `ComboBox` (`comboExportProfile`, 170 px) — lists saved profiles; selecting one calls `ApplyExportProfile()` immediately
- **💾** button (`btnSaveExportProfile`) — if a profile is selected: `MessageBox.YesNoCancel` "Overwrite `{name}`?" (Yes = overwrite, No = open editor with `openNew:true`, Cancel = abort); if no profile selected: open editor with `openNew:true`
- **✎** button (`btnEditExportProfiles`) — opens `ExportProfileEditorDialog`

**`MainWindow` methods** (not in `ExportSettingsHelper`):
- `ReadCurrentExportProfile()` → `ExportSettingsProfile` — snapshots all ~80 UI controls
- `ApplyExportProfile(p)` — restores all controls from profile; skips `DWG.Setup` in multi-doc mode; always applies `chkBindOle`; calls `UpdateFormatGroups()` + `UpdatePreview()` at end
- `RefreshExportProfileCombo()` — repopulates `comboExportProfile`, preserves selection
- `_applyingExportProfile` bool flag — prevents `OnExportProfileSelectionChanged` from re-firing during `ApplyExportProfile`

**DWG multi-doc note:** When multiple projects are loaded, `DWG.Setup` in the profile is ignored on apply (panel is disabled); `chkBindOle` is always applied regardless.

**`ExportProfileEditorDialog`** — inline `Window` subclass at the end of `MainWindow.xaml.cs`. Fixed size 540×420. Layout:
- **Description** (gray, 11 pt, wrapping) — full-width header text
- **Two-column main area** (`WpfGrid`):
  - *Left column (`*`)* — `ListBox` (`_list`) with custom `ItemContainerStyle` built via `BuildListItemStyle()`: `FrameworkElementFactory` + `ControlTemplate` with triggers for selected (blue `#1A73E8` bg, white text) and hover-not-selected (light blue `#E8F0FE` bg). Each item is a profile name string.
  - *Right column (196 px)* — `StackPanel` with:
    - Info tile (`Border`, `#F8F9FA` bg) — `"Formats: PDF, DWG   ·   Naming: Default"`; updates on `_list.SelectionChanged`
    - `+ New from current` (primary blue, full width) — clones `_currentSettings` under a new name
    - `Duplicate` (standard, full width)
    - `Rename` (standard, full width)
    - `✕ Delete` (red destructive, full width)
- **Divider** — 1 px `Border`
- **Save / Cancel** row — right-aligned
- `openNew: true` → triggers "New from current" click via `Dispatcher.BeginInvoke(DispatcherPriority.Loaded)` after load
- `RefreshList(name)` — repopulates `_list.ItemsSource` and selects the given name
- `AskName(prompt, default)` — inline prompt `Window` (no `Microsoft.VisualBasic` dependency)
