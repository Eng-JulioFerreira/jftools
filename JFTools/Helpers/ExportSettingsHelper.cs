using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JFTools.Helpers
{
    // ── Model ────────────────────────────────────────────────────────────────
    public class ExportSettingsProfile
    {
        public string Name { get; set; } = "";

        // Naming
        public string NamingProfile { get; set; } = "";

        // Active formats
        public bool FormatPDF { get; set; } = true;
        public bool FormatDWG { get; set; }
        public bool FormatIFC { get; set; }
        public bool FormatNWC { get; set; }

        // PDF
        public bool   PdfUseRaster  { get; set; }
        public int    PdfQuality     { get; set; } = 2;
        public int    PdfColorMode   { get; set; }
        public bool   PdfHideRef     { get; set; }
        public bool   PdfHideTags    { get; set; }
        public bool   PdfHideScope   { get; set; }
        public bool   PdfHideCrop    { get; set; }
        public bool   PdfCombined    { get; set; }
        public string PdfSingleName  { get; set; } = "";

        // DWG
        public string DwgSetup                  { get; set; } = ExportHelper.InSessionSetupName;
        public bool   DwgBindOle                { get; set; }
        public int    DwgVersionIndex           { get; set; }
        public bool   DwgMergedViews            { get; set; } = true;
        public bool   DwgExportingAreas         { get; set; }
        public bool   DwgPreserveCoincident     { get; set; }
        public bool   DwgMarkNonplot            { get; set; }
        public bool   DwgHideReferencePlane     { get; set; }
        public bool   DwgHideScopeBox          { get; set; }
        public bool   DwgHideUnrefTags          { get; set; }
        public string DwgNonplotSuffix          { get; set; } = "";
        public int    DwgACAIndex               { get; set; }
        public int    DwgPropOverridesIndex     { get; set; }
        public int    DwgColorIndex             { get; set; }
        public int    DwgTextIndex              { get; set; }
        public int    DwgSolidsIndex            { get; set; }
        public bool   DwgSharedCoords           { get; set; }
        public int    DwgUnitsIndex             { get; set; }
        public int    DwgLineScalingIndex       { get; set; }

        // IFC
        public int    IfcVersionIndex           { get; set; }
        public int    IfcFileTypeIndex          { get; set; }
        public int    IfcSpaceBoundaries        { get; set; }
        public int    IfcLinkedFilesIndex       { get; set; }
        public bool   IfcSplitWalls             { get; set; }
        public bool   IfcIncludeSteel           { get; set; }
        public bool   IfcExport2D               { get; set; }
        public bool   IfcVisibleOnly            { get; set; }
        public bool   IfcExportRooms            { get; set; }
        public int    IfcTessellationIndex      { get; set; } = 2;
        public bool   IfcRevitPsets             { get; set; }
        public bool   IfcCommonPsets            { get; set; } = true;
        public bool   IfcBaseQty                { get; set; }
        public bool   IfcSchedulesPsets         { get; set; }
        public bool   IfcMaterialPsets          { get; set; }
        public bool   IfcUserPsets              { get; set; }
        public string IfcUserPsetsFile          { get; set; } = "";
        public bool   IfcParamMapping           { get; set; }
        public string IfcParamMappingFile       { get; set; } = "";
        public bool   IfcParts                  { get; set; }
        public bool   IfcSolidModel             { get; set; }
        public bool   IfcActiveView             { get; set; }
        public bool   IfcFamilyTypeName         { get; set; }
        public bool   Ifc2DRoomBoundary         { get; set; }
        public bool   IfcSiteElevation          { get; set; }
        public bool   IfcStoreGUID              { get; set; }
        public bool   IfcTriangulationOnly      { get; set; }
        public bool   IfcTypeNameOnly           { get; set; }
        public bool   IfcVisibleName            { get; set; }
        public int    IfcSitePlacementIndex     { get; set; }
        public int    IfcExchangeReqIndex       { get; set; }

        // NWC
        public int    NwcCoordsIndex            { get; set; }
        public int    NwcParamsIndex            { get; set; }
        public bool   NwcExportLinks            { get; set; }
        public bool   NwcConvertProps           { get; set; } = true;
        public bool   NwcDivide                 { get; set; }
        public string NwcFaceting               { get; set; } = "1.0";
        public bool   NwcExportUrls             { get; set; }
        public bool   NwcMissingMaterials       { get; set; }
        public bool   NwcRoomAsAttribute        { get; set; }
        public bool   NwcRoomGeometry           { get; set; }

        // Output
        public bool OutputUseProjectSubfolders  { get; set; }
        public bool OutputUseFormatSubfolders   { get; set; }

        // ── Serialization ────────────────────────────────────────────────────
        internal void SetKey(string key, string val)
        {
            bool B(string v) => v.Equals("true", StringComparison.OrdinalIgnoreCase);
            int  I(string v) => int.TryParse(v, out int n) ? n : 0;

            switch (key)
            {
                case "NamingProfile":              NamingProfile             = val; break;
                case "Formats":
                    FormatPDF = val.Contains("PDF");
                    FormatDWG = val.Contains("DWG");
                    FormatIFC = val.Contains("IFC");
                    FormatNWC = val.Contains("NWC");
                    break;
                // PDF
                case "PDF.UseRaster":              PdfUseRaster              = B(val); break;
                case "PDF.Quality":                PdfQuality                = I(val); break;
                case "PDF.ColorMode":              PdfColorMode              = I(val); break;
                case "PDF.HideRef":                PdfHideRef                = B(val); break;
                case "PDF.HideTags":               PdfHideTags               = B(val); break;
                case "PDF.HideScope":              PdfHideScope              = B(val); break;
                case "PDF.HideCrop":               PdfHideCrop               = B(val); break;
                case "PDF.Combined":               PdfCombined               = B(val); break;
                case "PDF.SingleName":             PdfSingleName             = val;    break;
                // DWG
                case "DWG.Setup":                  DwgSetup                  = val;    break;
                case "DWG.BindOle":                DwgBindOle                = B(val); break;
                case "DWG.VersionIndex":           DwgVersionIndex           = I(val); break;
                case "DWG.MergedViews":            DwgMergedViews            = B(val); break;
                case "DWG.ExportingAreas":         DwgExportingAreas         = B(val); break;
                case "DWG.PreserveCoincident":     DwgPreserveCoincident     = B(val); break;
                case "DWG.MarkNonplot":            DwgMarkNonplot            = B(val); break;
                case "DWG.HideReferencePlane":     DwgHideReferencePlane     = B(val); break;
                case "DWG.HideScopeBox":           DwgHideScopeBox          = B(val); break;
                case "DWG.HideUnrefTags":          DwgHideUnrefTags          = B(val); break;
                case "DWG.NonplotSuffix":          DwgNonplotSuffix          = val;    break;
                case "DWG.ACAIndex":               DwgACAIndex               = I(val); break;
                case "DWG.PropOverridesIndex":     DwgPropOverridesIndex     = I(val); break;
                case "DWG.ColorIndex":             DwgColorIndex             = I(val); break;
                case "DWG.TextIndex":              DwgTextIndex              = I(val); break;
                case "DWG.SolidsIndex":            DwgSolidsIndex            = I(val); break;
                case "DWG.SharedCoords":           DwgSharedCoords           = B(val); break;
                case "DWG.UnitsIndex":             DwgUnitsIndex             = I(val); break;
                case "DWG.LineScalingIndex":       DwgLineScalingIndex       = I(val); break;
                // IFC
                case "IFC.VersionIndex":           IfcVersionIndex           = I(val); break;
                case "IFC.FileTypeIndex":          IfcFileTypeIndex          = I(val); break;
                case "IFC.SpaceBoundaries":        IfcSpaceBoundaries        = I(val); break;
                case "IFC.LinkedFilesIndex":       IfcLinkedFilesIndex       = I(val); break;
                case "IFC.SplitWalls":             IfcSplitWalls             = B(val); break;
                case "IFC.IncludeSteel":           IfcIncludeSteel           = B(val); break;
                case "IFC.Export2D":               IfcExport2D               = B(val); break;
                case "IFC.VisibleOnly":            IfcVisibleOnly            = B(val); break;
                case "IFC.ExportRooms":            IfcExportRooms            = B(val); break;
                case "IFC.TessellationIndex":      IfcTessellationIndex      = I(val); break;
                case "IFC.RevitPsets":             IfcRevitPsets             = B(val); break;
                case "IFC.CommonPsets":            IfcCommonPsets            = B(val); break;
                case "IFC.BaseQty":                IfcBaseQty                = B(val); break;
                case "IFC.SchedulesPsets":         IfcSchedulesPsets         = B(val); break;
                case "IFC.MaterialPsets":          IfcMaterialPsets          = B(val); break;
                case "IFC.UserPsets":              IfcUserPsets              = B(val); break;
                case "IFC.UserPsetsFile":          IfcUserPsetsFile          = val;    break;
                case "IFC.ParamMapping":           IfcParamMapping           = B(val); break;
                case "IFC.ParamMappingFile":       IfcParamMappingFile       = val;    break;
                case "IFC.Parts":                  IfcParts                  = B(val); break;
                case "IFC.SolidModel":             IfcSolidModel             = B(val); break;
                case "IFC.ActiveView":             IfcActiveView             = B(val); break;
                case "IFC.FamilyTypeName":         IfcFamilyTypeName         = B(val); break;
                case "IFC.2DRoomBoundary":         Ifc2DRoomBoundary         = B(val); break;
                case "IFC.SiteElevation":          IfcSiteElevation          = B(val); break;
                case "IFC.StoreGUID":              IfcStoreGUID              = B(val); break;
                case "IFC.TriangulationOnly":      IfcTriangulationOnly      = B(val); break;
                case "IFC.TypeNameOnly":           IfcTypeNameOnly           = B(val); break;
                case "IFC.VisibleName":            IfcVisibleName            = B(val); break;
                case "IFC.SitePlacementIndex":     IfcSitePlacementIndex     = I(val); break;
                case "IFC.ExchangeReqIndex":       IfcExchangeReqIndex       = I(val); break;
                // NWC
                case "NWC.CoordsIndex":            NwcCoordsIndex            = I(val); break;
                case "NWC.ParamsIndex":            NwcParamsIndex            = I(val); break;
                case "NWC.ExportLinks":            NwcExportLinks            = B(val); break;
                case "NWC.ConvertProps":           NwcConvertProps           = B(val); break;
                case "NWC.Divide":                 NwcDivide                 = B(val); break;
                case "NWC.Faceting":               NwcFaceting               = val;    break;
                case "NWC.ExportUrls":             NwcExportUrls             = B(val); break;
                case "NWC.MissingMaterials":       NwcMissingMaterials       = B(val); break;
                case "NWC.RoomAsAttribute":        NwcRoomAsAttribute        = B(val); break;
                case "NWC.RoomGeometry":           NwcRoomGeometry           = B(val); break;
                // Output
                case "Output.UseProjectSubfolders": OutputUseProjectSubfolders = B(val); break;
                case "Output.UseFormatSubfolders":  OutputUseFormatSubfolders  = B(val); break;
            }
        }

        internal IEnumerable<string> ToLines()
        {
            string B(bool v) => v ? "true" : "false";

            // Formats
            var fmts = new List<string>();
            if (FormatPDF) fmts.Add("PDF");
            if (FormatDWG) fmts.Add("DWG");
            if (FormatIFC) fmts.Add("IFC");
            if (FormatNWC) fmts.Add("NWC");
            yield return $"NamingProfile={NamingProfile}";
            yield return $"Formats={string.Join(",", fmts)}";
            // PDF
            yield return $"PDF.UseRaster={B(PdfUseRaster)}";
            yield return $"PDF.Quality={PdfQuality}";
            yield return $"PDF.ColorMode={PdfColorMode}";
            yield return $"PDF.HideRef={B(PdfHideRef)}";
            yield return $"PDF.HideTags={B(PdfHideTags)}";
            yield return $"PDF.HideScope={B(PdfHideScope)}";
            yield return $"PDF.HideCrop={B(PdfHideCrop)}";
            yield return $"PDF.Combined={B(PdfCombined)}";
            yield return $"PDF.SingleName={PdfSingleName}";
            // DWG
            yield return $"DWG.Setup={DwgSetup}";
            yield return $"DWG.BindOle={B(DwgBindOle)}";
            yield return $"DWG.VersionIndex={DwgVersionIndex}";
            yield return $"DWG.MergedViews={B(DwgMergedViews)}";
            yield return $"DWG.ExportingAreas={B(DwgExportingAreas)}";
            yield return $"DWG.PreserveCoincident={B(DwgPreserveCoincident)}";
            yield return $"DWG.MarkNonplot={B(DwgMarkNonplot)}";
            yield return $"DWG.HideReferencePlane={B(DwgHideReferencePlane)}";
            yield return $"DWG.HideScopeBox={B(DwgHideScopeBox)}";
            yield return $"DWG.HideUnrefTags={B(DwgHideUnrefTags)}";
            yield return $"DWG.NonplotSuffix={DwgNonplotSuffix}";
            yield return $"DWG.ACAIndex={DwgACAIndex}";
            yield return $"DWG.PropOverridesIndex={DwgPropOverridesIndex}";
            yield return $"DWG.ColorIndex={DwgColorIndex}";
            yield return $"DWG.TextIndex={DwgTextIndex}";
            yield return $"DWG.SolidsIndex={DwgSolidsIndex}";
            yield return $"DWG.SharedCoords={B(DwgSharedCoords)}";
            yield return $"DWG.UnitsIndex={DwgUnitsIndex}";
            yield return $"DWG.LineScalingIndex={DwgLineScalingIndex}";
            // IFC
            yield return $"IFC.VersionIndex={IfcVersionIndex}";
            yield return $"IFC.FileTypeIndex={IfcFileTypeIndex}";
            yield return $"IFC.SpaceBoundaries={IfcSpaceBoundaries}";
            yield return $"IFC.LinkedFilesIndex={IfcLinkedFilesIndex}";
            yield return $"IFC.SplitWalls={B(IfcSplitWalls)}";
            yield return $"IFC.IncludeSteel={B(IfcIncludeSteel)}";
            yield return $"IFC.Export2D={B(IfcExport2D)}";
            yield return $"IFC.VisibleOnly={B(IfcVisibleOnly)}";
            yield return $"IFC.ExportRooms={B(IfcExportRooms)}";
            yield return $"IFC.TessellationIndex={IfcTessellationIndex}";
            yield return $"IFC.RevitPsets={B(IfcRevitPsets)}";
            yield return $"IFC.CommonPsets={B(IfcCommonPsets)}";
            yield return $"IFC.BaseQty={B(IfcBaseQty)}";
            yield return $"IFC.SchedulesPsets={B(IfcSchedulesPsets)}";
            yield return $"IFC.MaterialPsets={B(IfcMaterialPsets)}";
            yield return $"IFC.UserPsets={B(IfcUserPsets)}";
            yield return $"IFC.UserPsetsFile={IfcUserPsetsFile}";
            yield return $"IFC.ParamMapping={B(IfcParamMapping)}";
            yield return $"IFC.ParamMappingFile={IfcParamMappingFile}";
            yield return $"IFC.Parts={B(IfcParts)}";
            yield return $"IFC.SolidModel={B(IfcSolidModel)}";
            yield return $"IFC.ActiveView={B(IfcActiveView)}";
            yield return $"IFC.FamilyTypeName={B(IfcFamilyTypeName)}";
            yield return $"IFC.2DRoomBoundary={B(Ifc2DRoomBoundary)}";
            yield return $"IFC.SiteElevation={B(IfcSiteElevation)}";
            yield return $"IFC.StoreGUID={B(IfcStoreGUID)}";
            yield return $"IFC.TriangulationOnly={B(IfcTriangulationOnly)}";
            yield return $"IFC.TypeNameOnly={B(IfcTypeNameOnly)}";
            yield return $"IFC.VisibleName={B(IfcVisibleName)}";
            yield return $"IFC.SitePlacementIndex={IfcSitePlacementIndex}";
            yield return $"IFC.ExchangeReqIndex={IfcExchangeReqIndex}";
            // NWC
            yield return $"NWC.CoordsIndex={NwcCoordsIndex}";
            yield return $"NWC.ParamsIndex={NwcParamsIndex}";
            yield return $"NWC.ExportLinks={B(NwcExportLinks)}";
            yield return $"NWC.ConvertProps={B(NwcConvertProps)}";
            yield return $"NWC.Divide={B(NwcDivide)}";
            yield return $"NWC.Faceting={NwcFaceting}";
            yield return $"NWC.ExportUrls={B(NwcExportUrls)}";
            yield return $"NWC.MissingMaterials={B(NwcMissingMaterials)}";
            yield return $"NWC.RoomAsAttribute={B(NwcRoomAsAttribute)}";
            yield return $"NWC.RoomGeometry={B(NwcRoomGeometry)}";
            // Output
            yield return $"Output.UseProjectSubfolders={B(OutputUseProjectSubfolders)}";
            yield return $"Output.UseFormatSubfolders={B(OutputUseFormatSubfolders)}";
        }
    }

    // ── Helper ───────────────────────────────────────────────────────────────
    public static class ExportSettingsHelper
    {
        private const string FileName = "export_settings.ini";

        public static string GetProfilesPath()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "JFTools", FileName);

            if (!File.Exists(appData))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(appData));
                string programData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "JFTools", FileName);
                if (File.Exists(programData))
                    File.Copy(programData, appData, overwrite: false);
            }
            return appData;
        }

        public static Dictionary<string, ExportSettingsProfile> LoadProfiles(string path)
        {
            var result = new Dictionary<string, ExportSettingsProfile>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path)) return result;

            ExportSettingsProfile current = null;
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                if (line.StartsWith("[Profile:") && line.EndsWith("]"))
                {
                    string name = line.Substring(9, line.Length - 10).Trim();
                    if (string.IsNullOrEmpty(name)) continue;
                    current = new ExportSettingsProfile { Name = name };
                    result[name] = current;
                }
                else if (current != null)
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1); // preserve value as-is (paths may contain '=')
                    current.SetKey(key, val);
                }
            }
            return result;
        }

        public static void SaveProfiles(string path, Dictionary<string, ExportSettingsProfile> profiles)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var sb = new StringBuilder();
            foreach (var kvp in profiles)
            {
                sb.AppendLine($"[Profile:{kvp.Key}]");
                foreach (string line in kvp.Value.ToLines())
                    sb.AppendLine(line);
                sb.AppendLine();
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
    }
}
