using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Autodesk.Revit.DB;

namespace JFTools.Helpers
{
    /// <summary>
    /// Settings for the DWG "In-Session" export option (editable directly in the plugin UI).
    /// Maps 1:1 to <see cref="DWGExportOptions"/> properties.
    /// Index fields use the same order as the combo items in L.cs (DwgVersionItems, etc.).
    /// </summary>
    /// <summary>
    /// Full DWG export configuration shown in the plugin UI.
    /// Applies to any setup (In-Session or named). Index fields match
    /// the combo item order defined in L.cs (DwgVersionItems, etc.).
    /// </summary>
    public class DwgExportConfig
    {
        // ── General ─────────────────────────────────────────────────────────
        // 0=R2018  1=R2013(default)  2=R2010  3=R2007
        public int  VersionIndex            { get; set; } = 1;
        public bool MergedViews             { get; set; } = false;
        public bool ExportingAreas          { get; set; } = false;
        public bool PreserveCoincidentLines { get; set; } = true;
        public bool MarkNonplotLayers       { get; set; } = false;
        public bool HideReferencePlane      { get; set; } = true;
        public bool HideScopeBox            { get; set; } = true;
        public bool HideUnreferenceViewTags { get; set; } = true;
        public string NonplotSuffix         { get; set; } = "-NP";
        // 0=Object(default)  1=Geometry
        public int  ACAIndex                { get; set; } = 0;
        // 0=ByEntity(default)  1=ByLayer  2=NewLayer
        public int  PropOverridesIndex      { get; set; } = 0;

        // ── Colors ───────────────────────────────────────────────────────────
        // 0=TrueColor(default)  1=IndexColors  2=TrueColorPerView
        public int ColorIndex               { get; set; } = 0;

        // ── Text and Fonts ───────────────────────────────────────────────────
        // 0=Exact(default)  1=Approximate
        public int TextIndex                { get; set; } = 0;

        // ── Solids ───────────────────────────────────────────────────────────
        // 0=Polymesh(default)  1=ACIS
        public int SolidsIndex              { get; set; } = 0;

        // ── Units and Coordinates ────────────────────────────────────────────
        // 0=Default  1=Foot  2=Inch  3=Millimeter  4=Centimeter  5=Meter
        public int  UnitsIndex              { get; set; } = 0;
        // false=ProjectInternal(default)  true=Shared
        public bool SharedCoords            { get; set; } = false;

        // ── Lines ────────────────────────────────────────────────────────────
        // 0=ViewScale(default)  1=ModelSpace  2=PaperSpace
        public int LineScalingIndex         { get; set; } = 0;

        // ── Factory: load from an existing DWGExportOptions ─────────────────
        public static DwgExportConfig FromOptions(DWGExportOptions o) => new DwgExportConfig
        {
            VersionIndex = o.FileVersion switch {
                ACADVersion.R2018 => 0, ACADVersion.R2013 => 1,
                ACADVersion.R2010 => 2, _ => 3 },
            MergedViews             = o.MergedViews,
            ExportingAreas          = o.ExportingAreas,
            PreserveCoincidentLines = o.PreserveCoincidentLines,
            MarkNonplotLayers       = o.MarkNonplotLayers,
            HideReferencePlane      = o.HideReferencePlane,
            HideScopeBox            = o.HideScopeBox,
            HideUnreferenceViewTags = o.HideUnreferenceViewTags,
            NonplotSuffix           = o.NonplotSuffix ?? "",
            ACAIndex = o.ACAPreference == ACAObjectPreference.Geometry ? 1 : 0,
            PropOverridesIndex = o.PropOverrides switch {
                PropOverrideMode.ByLayer => 1, PropOverrideMode.NewLayer => 2, _ => 0 },
            ColorIndex = o.Colors switch {
                ExportColorMode.TrueColor => 0, ExportColorMode.IndexColors => 1,
                ExportColorMode.TrueColorPerView => 2, _ => 0 },
            TextIndex    = o.TextTreatment == TextTreatment.Approximate ? 1 : 0,
            SolidsIndex  = o.ExportOfSolids == SolidGeometry.ACIS ? 1 : 0,
            UnitsIndex = o.TargetUnit switch {
                ExportUnit.Foot => 1, ExportUnit.Inch => 2,
                ExportUnit.Millimeter => 3, ExportUnit.Centimeter => 4,
                ExportUnit.Meter => 5, _ => 0 },
            SharedCoords = o.SharedCoords,
            LineScalingIndex = o.LineScaling switch {
                LineScaling.ModelSpace => 1, LineScaling.PaperSpace => 2, _ => 0 },
        };

        // ── Apply to an existing options object (preserves layer/linetype mappings) ──
        public void ApplyToOptions(DWGExportOptions o)
        {
            o.FileVersion = VersionIndex switch {
                0 => ACADVersion.R2018, 1 => ACADVersion.R2013,
                2 => ACADVersion.R2010, _ => ACADVersion.R2007 };
            o.MergedViews             = MergedViews;
            o.ExportingAreas          = ExportingAreas;
            o.PreserveCoincidentLines = PreserveCoincidentLines;
            o.MarkNonplotLayers       = MarkNonplotLayers;
            o.HideReferencePlane      = HideReferencePlane;
            o.HideScopeBox            = HideScopeBox;
            o.HideUnreferenceViewTags = HideUnreferenceViewTags;
            o.NonplotSuffix           = NonplotSuffix ?? "";
            o.ACAPreference   = ACAIndex == 1 ? ACAObjectPreference.Geometry : ACAObjectPreference.Object;
            o.PropOverrides   = PropOverridesIndex switch {
                1 => PropOverrideMode.ByLayer, 2 => PropOverrideMode.NewLayer,
                _ => PropOverrideMode.ByEntity };
            o.Colors = ColorIndex switch {
                0 => ExportColorMode.TrueColor, 1 => ExportColorMode.IndexColors,
                2 => ExportColorMode.TrueColorPerView, _ => ExportColorMode.TrueColor };
            o.TextTreatment   = TextIndex == 1 ? TextTreatment.Approximate : TextTreatment.Exact;
            o.ExportOfSolids  = SolidsIndex == 1 ? SolidGeometry.ACIS : SolidGeometry.Polymesh;
            o.TargetUnit = UnitsIndex switch {
                1 => ExportUnit.Foot, 2 => ExportUnit.Inch,
                3 => ExportUnit.Millimeter, 4 => ExportUnit.Centimeter,
                5 => ExportUnit.Meter, _ => ExportUnit.Default };
            o.SharedCoords    = SharedCoords;
            o.LineScaling = LineScalingIndex switch {
                1 => LineScaling.ModelSpace, 2 => LineScaling.PaperSpace,
                _ => LineScaling.ViewScale };
        }

        public DWGExportOptions ToExportOptions()
        {
            var o = new DWGExportOptions();
            ApplyToOptions(o);
            return o;
        }
    }

    // Backward-compat alias so any external code referencing the old name still compiles
    [System.Obsolete("Use DwgExportConfig")]
    public class DwgInSessionConfig : DwgExportConfig { }

    public class IfcExportConfig
    {
        // General
        public string IfcVersion { get; set; } = "IFC2x3CV2";
        public string FileType { get; set; } = "Ifc";   // Ifc | IfcXML | IfcZIP | IfcXMLZIP
        public int SpaceBoundaries { get; set; } = 0;

        // Additional Content
        public bool Export2DElements { get; set; } = false;
        public bool ExportRoomsInView { get; set; } = false;
        public bool ExportPartsAsBuildingElements { get; set; } = false;
        public bool ExportSolidModelRep { get; set; } = false;
        public bool IncludeSiteElevation { get; set; } = false;
        public bool IncludeSteelElements { get; set; } = true;
        // "None" | "Embedded" (merged into host IFC) | "Separate" (one IFC file per linked model)
        public string LinkedFilesMode { get; set; } = "None";
        public bool VisibleElementsOfCurrentView { get; set; } = false;
        public bool UseActiveViewGeometry { get; set; } = false;

        // Property Sets
        public bool ExportBaseQuantities { get; set; } = false;
        public bool ExportIFCCommonPropertySets { get; set; } = true;
        public bool ExportInternalRevitPropertySets { get; set; } = false;
        public bool ExportSchedulesAsPsets { get; set; } = false;
        public bool ExportMaterialPsets { get; set; } = false;
        public bool ExportUserDefinedPsets { get; set; } = false;
        public string ExportUserDefinedPsetsFileName { get; set; } = "";
        public bool ExportUserDefinedParameterMapping { get; set; } = false;
        public string ExportUserDefinedParameterMappingFileName { get; set; } = "";

        // Level of Detail
        public double TessellationLevelOfDetail { get; set; } = 0.5;

        // Advanced
        public bool SplitWallsAndColumns { get; set; } = false;
        public bool StoreIFCGUID { get; set; } = false;
        public bool UseOnlyTriangulation { get; set; } = false;
        public bool Use2DRoomBoundaryForVolume { get; set; } = false;
        public bool UseFamilyAndTypeNameForReference { get; set; } = false;
        public bool UseTypeNameOnlyForIfcType { get; set; } = false;
        public bool UseVisibleRevitNameAsEntityName { get; set; } = false;
        public string SitePlacement { get; set; } = "Shared";          // SiteTransformBasis enum name
        public string ExchangeRequirement { get; set; } = "NotDefined"; // KnownERNames enum name

        // ── Settings file I/O ─────────────────────────────────────

        /// <summary>Loads IFC config from a JFTools .txt or a DiRoots One .xml profile.</summary>
        public static IfcExportConfig LoadFromFile(string path)
        {
            string ext = Path.GetExtension(path) ?? "";
            if (ext.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                return LoadFromXml(path);
            return LoadFromTxt(path);
        }

        private static IfcExportConfig LoadFromTxt(string path)
        {
            var cfg = new IfcExportConfig();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.StartsWith("#") || !line.Contains("=")) continue;
                int eq = line.IndexOf('=');
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                switch (key)
                {
                    case "IfcVersion":  cfg.IfcVersion  = val; break;
                    case "FileType":    cfg.FileType     = val; break;
                    case "SpaceBoundaries": int.TryParse(val, out var sb); cfg.SpaceBoundaries = sb; break;
                    case "LinkedFilesMode": cfg.LinkedFilesMode = val; break;
                    case "TessellationLevelOfDetail": double.TryParse(val, System.Globalization.NumberStyles.Float, inv, out var t); cfg.TessellationLevelOfDetail = t; break;
                    case "SitePlacement":   cfg.SitePlacement   = val; break;
                    case "ExchangeRequirement": cfg.ExchangeRequirement = val; break;
                    case "Export2DElements":             bool.TryParse(val, out var v1); cfg.Export2DElements = v1; break;
                    case "ExportRoomsInView":            bool.TryParse(val, out var v2); cfg.ExportRoomsInView = v2; break;
                    case "ExportPartsAsBuildingElements":bool.TryParse(val, out var v3); cfg.ExportPartsAsBuildingElements = v3; break;
                    case "ExportSolidModelRep":          bool.TryParse(val, out var v4); cfg.ExportSolidModelRep = v4; break;
                    case "IncludeSiteElevation":         bool.TryParse(val, out var v5); cfg.IncludeSiteElevation = v5; break;
                    case "IncludeSteelElements":         bool.TryParse(val, out var v6); cfg.IncludeSteelElements = v6; break;
                    case "VisibleElementsOfCurrentView": bool.TryParse(val, out var v7); cfg.VisibleElementsOfCurrentView = v7; break;
                    case "UseActiveViewGeometry":        bool.TryParse(val, out var v8); cfg.UseActiveViewGeometry = v8; break;
                    case "ExportBaseQuantities":         bool.TryParse(val, out var v9); cfg.ExportBaseQuantities = v9; break;
                    case "ExportIFCCommonPropertySets":  bool.TryParse(val, out var va); cfg.ExportIFCCommonPropertySets = va; break;
                    case "ExportInternalRevitPropertySets": bool.TryParse(val, out var vb); cfg.ExportInternalRevitPropertySets = vb; break;
                    case "ExportSchedulesAsPsets":       bool.TryParse(val, out var vc); cfg.ExportSchedulesAsPsets = vc; break;
                    case "ExportMaterialPsets":          bool.TryParse(val, out var vd); cfg.ExportMaterialPsets = vd; break;
                    case "ExportUserDefinedPsets":       bool.TryParse(val, out var ve); cfg.ExportUserDefinedPsets = ve; break;
                    case "ExportUserDefinedPsetsFileName": cfg.ExportUserDefinedPsetsFileName = val; break;
                    case "ExportUserDefinedParameterMapping": bool.TryParse(val, out var vf); cfg.ExportUserDefinedParameterMapping = vf; break;
                    case "ExportUserDefinedParameterMappingFileName": cfg.ExportUserDefinedParameterMappingFileName = val; break;
                    case "SplitWallsAndColumns":         bool.TryParse(val, out var vg); cfg.SplitWallsAndColumns = vg; break;
                    case "StoreIFCGUID":                 bool.TryParse(val, out var vh); cfg.StoreIFCGUID = vh; break;
                    case "UseOnlyTriangulation":         bool.TryParse(val, out var vi); cfg.UseOnlyTriangulation = vi; break;
                    case "Use2DRoomBoundaryForVolume":   bool.TryParse(val, out var vj); cfg.Use2DRoomBoundaryForVolume = vj; break;
                    case "UseFamilyAndTypeNameForReference": bool.TryParse(val, out var vk); cfg.UseFamilyAndTypeNameForReference = vk; break;
                    case "UseTypeNameOnlyForIfcType":    bool.TryParse(val, out var vl); cfg.UseTypeNameOnlyForIfcType = vl; break;
                    case "UseVisibleRevitNameAsEntityName": bool.TryParse(val, out var vm); cfg.UseVisibleRevitNameAsEntityName = vm; break;
                }
            }
            return cfg;
        }

        /// <summary>Loads IFC config from a DiRoots One XML profile (root element: Profiles).</summary>
        private static IfcExportConfig LoadFromXml(string path)
        {
            var cfg = new IfcExportConfig();
            var doc = XDocument.Load(path);
            if (doc.Root?.Name.LocalName != "Profiles") return cfg;

            var ifc = doc.Descendants("IFC").FirstOrDefault();
            if (ifc == null) return cfg;

            string Str(string tag)  => ifc.Element(tag)?.Value.Trim() ?? "";
            bool   Bool(string tag) { bool.TryParse(Str(tag), out var v); return v; }
            var inv = System.Globalization.CultureInfo.InvariantCulture;

            // Version / type
            cfg.IfcVersion = Str("FileVersion").Length > 0 ? Str("FileVersion") : cfg.IfcVersion;
            cfg.FileType   = Str("IFCFileType").Length  > 0 ? Str("IFCFileType")  : cfg.FileType;

            // Space boundaries: "None"→0, "First"/"1"→1, "Second"/"2"→2
            switch (Str("SpaceBoundaries").ToLowerInvariant())
            {
                case "none": case "0": cfg.SpaceBoundaries = 0; break;
                case "first": case "1": cfg.SpaceBoundaries = 1; break;
                case "second": case "2": cfg.SpaceBoundaries = 2; break;
            }

            // Linked files: boolean → None / Embedded
            string lf = Str("ExportLinkedFiles");
            if (lf.Length > 0)
                cfg.LinkedFilesMode = lf.Equals("true", StringComparison.OrdinalIgnoreCase) ? "Embedded" : "None";

            // Tessellation: named level → 0.0 / 0.25 / 0.5 / 0.75 / 1.0
            switch (Str("TessellationLevelOfDetail").ToLowerInvariant())
            {
                case "extralow":  cfg.TessellationLevelOfDetail = 0.0;  break;
                case "low":       cfg.TessellationLevelOfDetail = 0.25; break;
                case "medium":    cfg.TessellationLevelOfDetail = 0.5;  break;
                case "high":      cfg.TessellationLevelOfDetail = 0.75; break;
                case "extrahigh": cfg.TessellationLevelOfDetail = 1.0;  break;
            }

            // Site placement: DiRoots display name → SiteTransformBasis enum name
            switch (Str("SitePlacement").ToLowerInvariant())
            {
                case "current shared coordinates": case "shared": cfg.SitePlacement = "Shared"; break;
                case "site survey point":           case "site":   cfg.SitePlacement = "Site";   break;
                case "project base point":          case "project":cfg.SitePlacement = "Project"; break;
                case "internal origin":             case "internal":cfg.SitePlacement = "Internal"; break;
                case "project base point (true north)": cfg.SitePlacement = "ProjectInTN"; break;
                case "internal origin (true north)":    cfg.SitePlacement = "InternalInTN"; break;
            }

            // Booleans (field names match exactly)
            cfg.SplitWallsAndColumns         = Bool("WallAndColumnSplitting");
            cfg.IncludeSteelElements         = Bool("IncludeSteelElements");
            cfg.Export2DElements             = Bool("Export2DElements");
            cfg.ExportRoomsInView            = Bool("ExportRoomsInView");
            cfg.ExportInternalRevitPropertySets = Bool("ExportInternalRevitPropertySets");
            cfg.ExportIFCCommonPropertySets  = Bool("ExportIFCCommonPropertySets");
            cfg.ExportBaseQuantities         = Bool("ExportBaseQuantities");
            cfg.ExportSchedulesAsPsets       = Bool("ExportSchedulesAsPsets");
            cfg.ExportUserDefinedPsets       = Bool("ExportUserDefinedPsets");
            cfg.ExportUserDefinedPsetsFileName          = Str("ExportUserDefinedPsetsFileName");
            cfg.ExportUserDefinedParameterMapping       = Bool("ExportUserDefinedParameterMapping");
            cfg.ExportUserDefinedParameterMappingFileName = Str("ExportUserDefinedParameterMappingFileName");
            cfg.ExportPartsAsBuildingElements = Bool("ExportPartsAsBuildingElements");
            cfg.ExportSolidModelRep          = Bool("ExportSolidModelRep");
            cfg.UseFamilyAndTypeNameForReference = Bool("UseFamilyAndTypeNameForReference");
            cfg.UseActiveViewGeometry        = Bool("UseActiveViewCreatingGeometry");
            cfg.Use2DRoomBoundaryForVolume   = Bool("Use2DRoomBoundaryForVolume");
            cfg.IncludeSiteElevation         = Bool("IncludeSiteElevation");
            cfg.StoreIFCGUID                 = Bool("StoreIFCGUID");
            cfg.UseOnlyTriangulation         = Bool("UseOnlyTriangulation");
            cfg.VisibleElementsOfCurrentView = Bool("VisibleElementsOfCurrentView");
            cfg.UseTypeNameOnlyForIfcType    = Bool("UseTypeNameOnlyForIfcType");
            cfg.UseVisibleRevitNameAsEntityName = Bool("UseVisibleRevitNameAsEntityName");

            return cfg;
        }

        public void SaveToFile(string path)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("# JFTools IFC Export Settings");
            sb.AppendLine($"IfcVersion={IfcVersion}");
            sb.AppendLine($"FileType={FileType}");
            sb.AppendLine($"SpaceBoundaries={SpaceBoundaries}");
            sb.AppendLine($"LinkedFilesMode={LinkedFilesMode}");
            sb.AppendLine($"TessellationLevelOfDetail={TessellationLevelOfDetail.ToString(inv)}");
            sb.AppendLine($"SitePlacement={SitePlacement}");
            sb.AppendLine($"ExchangeRequirement={ExchangeRequirement}");
            sb.AppendLine($"Export2DElements={Export2DElements}");
            sb.AppendLine($"ExportRoomsInView={ExportRoomsInView}");
            sb.AppendLine($"ExportPartsAsBuildingElements={ExportPartsAsBuildingElements}");
            sb.AppendLine($"ExportSolidModelRep={ExportSolidModelRep}");
            sb.AppendLine($"IncludeSiteElevation={IncludeSiteElevation}");
            sb.AppendLine($"IncludeSteelElements={IncludeSteelElements}");
            sb.AppendLine($"VisibleElementsOfCurrentView={VisibleElementsOfCurrentView}");
            sb.AppendLine($"UseActiveViewGeometry={UseActiveViewGeometry}");
            sb.AppendLine($"ExportBaseQuantities={ExportBaseQuantities}");
            sb.AppendLine($"ExportIFCCommonPropertySets={ExportIFCCommonPropertySets}");
            sb.AppendLine($"ExportInternalRevitPropertySets={ExportInternalRevitPropertySets}");
            sb.AppendLine($"ExportSchedulesAsPsets={ExportSchedulesAsPsets}");
            sb.AppendLine($"ExportMaterialPsets={ExportMaterialPsets}");
            sb.AppendLine($"ExportUserDefinedPsets={ExportUserDefinedPsets}");
            sb.AppendLine($"ExportUserDefinedPsetsFileName={ExportUserDefinedPsetsFileName}");
            sb.AppendLine($"ExportUserDefinedParameterMapping={ExportUserDefinedParameterMapping}");
            sb.AppendLine($"ExportUserDefinedParameterMappingFileName={ExportUserDefinedParameterMappingFileName}");
            sb.AppendLine($"SplitWallsAndColumns={SplitWallsAndColumns}");
            sb.AppendLine($"StoreIFCGUID={StoreIFCGUID}");
            sb.AppendLine($"UseOnlyTriangulation={UseOnlyTriangulation}");
            sb.AppendLine($"Use2DRoomBoundaryForVolume={Use2DRoomBoundaryForVolume}");
            sb.AppendLine($"UseFamilyAndTypeNameForReference={UseFamilyAndTypeNameForReference}");
            sb.AppendLine($"UseTypeNameOnlyForIfcType={UseTypeNameOnlyForIfcType}");
            sb.AppendLine($"UseVisibleRevitNameAsEntityName={UseVisibleRevitNameAsEntityName}");
            File.WriteAllText(path, sb.ToString());
        }
    }

    public class NwcExportConfig
    {
        // Basic
        public string Coordinates             { get; set; } = "Shared"; // "Shared" | "Internal"
        public bool   ExportLinks             { get; set; } = false;
        public bool   ConvertElementProperties{ get; set; } = true;
        public string Parameters              { get; set; } = "None";   // "None" | "ModelProperties" | "AllProperties"
        public bool   DivideFileIntoLevels    { get; set; } = false;

        // Advanced
        public double FacetingFactor          { get; set; } = 1.0;
        public bool   ExportUrls              { get; set; } = false;
        public bool   FindMissingMaterials    { get; set; } = false;
        public bool   ExportRoomAsAttribute   { get; set; } = false;
        public bool   ExportRoomGeometry      { get; set; } = false;

        // ── Settings file I/O ──────────────────────────────────────
        private static readonly string[] _keys =
        {
            "Coordinates","Parameters","ExportLinks","ConvertElementProperties",
            "DivideFileIntoLevels","FacetingFactor","ExportUrls",
            "FindMissingMaterials","ExportRoomAsAttribute","ExportRoomGeometry"
        };

        public static NwcExportConfig LoadFromFile(string path)
        {
            string ext = Path.GetExtension(path) ?? "";
            if (ext.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                return LoadFromXml(path);
            return LoadFromTxt(path);
        }

        private static NwcExportConfig LoadFromTxt(string path)
        {
            var cfg = new NwcExportConfig();
            var lines = File.ReadAllLines(path);
            foreach (var raw in lines)
            {
                string line = raw.Trim();
                if (line.StartsWith("#") || !line.Contains("=")) continue;
                int eq = line.IndexOf('=');
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                switch (key)
                {
                    case "Coordinates":              cfg.Coordinates              = val; break;
                    case "Parameters":               cfg.Parameters               = val; break;
                    case "ExportLinks":              bool.TryParse(val, out var el);   cfg.ExportLinks              = el; break;
                    case "ConvertElementProperties": bool.TryParse(val, out var cp);   cfg.ConvertElementProperties = cp; break;
                    case "DivideFileIntoLevels":     bool.TryParse(val, out var dl);   cfg.DivideFileIntoLevels     = dl; break;
                    case "FacetingFactor":           double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ff); cfg.FacetingFactor = ff; break;
                    case "ExportUrls":               bool.TryParse(val, out var eu);   cfg.ExportUrls               = eu; break;
                    case "FindMissingMaterials":     bool.TryParse(val, out var fm);   cfg.FindMissingMaterials     = fm; break;
                    case "ExportRoomAsAttribute":    bool.TryParse(val, out var ra);   cfg.ExportRoomAsAttribute    = ra; break;
                    case "ExportRoomGeometry":       bool.TryParse(val, out var rg);   cfg.ExportRoomGeometry       = rg; break;
                }
            }
            return cfg;
        }

        /// <summary>
        /// Imports NWC settings from a DiRoots One XML profile or a Navisworks option exchange XML file.
        /// Settings not exposed by NavisworksExportOptions are silently ignored.
        /// </summary>
        private static NwcExportConfig LoadFromXml(string path)
        {
            var cfg = new NwcExportConfig();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var doc = XDocument.Load(path);

            // DiRoots One profile format: root element is <Profiles>
            if (doc.Root?.Name.LocalName == "Profiles")
                return LoadFromDiRootsXml(doc);

            // Navisworks option exchange format: find <optionset name="nwexportrevit">
            var revitSet = doc.Descendants("optionset")
                              .FirstOrDefault(e => (string)e.Attribute("name") == "nwexportrevit");
            if (revitSet == null) return cfg;

            // Build a flat dictionary: option-name → display text / bool / float string
            var opts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var option in revitSet.Elements("option"))
            {
                string name = (string)option.Attribute("name");
                if (string.IsNullOrEmpty(name)) continue;
                var data = option.Element("data");
                if (data == null) continue;
                string type = (string)data.Attribute("type") ?? "";
                string val;
                if (type == "name")
                {
                    // Value is the text of the child <name> element
                    var nameEl = data.Element("name");
                    val = nameEl != null ? nameEl.Value.Trim() : data.Value.Trim();
                }
                else
                {
                    val = data.Value.Trim();
                }
                opts[name] = val;
            }

            // ── Coordinates ──────────────────────────────────────
            if (opts.TryGetValue("nwexportrevit_coordinates", out var coords))
                cfg.Coordinates = coords == "Internal" ? "Internal" : "Shared";

            // ── Parameters ───────────────────────────────────────
            // XML values: "None" | "Elements" (= ModelProperties) | "All" (= AllProperties)
            if (opts.TryGetValue("nwexportrevit_element_params", out var prm))
            {
                if      (prm == "All")      cfg.Parameters = "AllProperties";
                else if (prm == "Elements") cfg.Parameters = "ModelProperties";
                else                        cfg.Parameters = "None";
            }

            // ── Booleans ─────────────────────────────────────────
            bool B(string key) { bool v; bool.TryParse(opts.TryGetValue(key, out var s) ? s : "", out v); return v; }

            cfg.ExportLinks              = B("nwexportrevit_linked_files");
            cfg.ConvertElementProperties = B("nwexportrevit_element_generic_properties");
            cfg.DivideFileIntoLevels     = B("nwexportrevit_divide_file_into_levels");
            cfg.ExportUrls               = B("nwexportrevit_urls");
            cfg.FindMissingMaterials     = B("nwexportrevit_element_find_missing_materials");
            cfg.ExportRoomAsAttribute    = B("nwexportrevit_room");
            cfg.ExportRoomGeometry       = B("nwexportrevit_room_geometry");

            // ── FacetingFactor ───────────────────────────────────
            if (opts.TryGetValue("nwexportrevit_param_faceting_factor", out var ff))
                if (double.TryParse(ff, System.Globalization.NumberStyles.Float, inv, out double ffv))
                    cfg.FacetingFactor = ffv;

            return cfg;
        }

        /// <summary>Loads NWC config from a DiRoots One XML profile (root: Profiles).</summary>
        private static NwcExportConfig LoadFromDiRootsXml(XDocument doc)
        {
            var cfg = new NwcExportConfig();
            var inv = System.Globalization.CultureInfo.InvariantCulture;

            var nwc = doc.Descendants("NWC").FirstOrDefault();
            if (nwc == null) return cfg;

            string Str(string tag)  => nwc.Element(tag)?.Value.Trim() ?? "";
            bool   Bool(string tag) { bool.TryParse(Str(tag), out var v); return v; }

            // Coordinates: "Shared" | "Internal" (same values as our config)
            string coords = Str("Coordinates");
            if (coords.Length > 0)
                cfg.Coordinates = coords == "Internal" ? "Internal" : "Shared";

            // Parameters: "None" | "Elements" → "ModelProperties" | "All" → "AllProperties"
            switch (Str("Parameters").ToLowerInvariant())
            {
                case "all":      cfg.Parameters = "AllProperties";   break;
                case "elements": cfg.Parameters = "ModelProperties"; break;
                case "none":     cfg.Parameters = "None";            break;
            }

            // FacetingFactor (stored as plain number in DiRoots)
            string facStr = Str("FacetingFactor");
            if (double.TryParse(facStr, System.Globalization.NumberStyles.Float, inv, out double fac))
                cfg.FacetingFactor = fac;

            // Booleans (field names match our config exactly)
            cfg.ExportLinks              = Bool("ExportLinks");
            cfg.ConvertElementProperties = Bool("ConvertElementProperties");
            cfg.DivideFileIntoLevels     = Bool("DivideFileIntoLevels");
            cfg.ExportUrls               = Bool("ExportUrls");
            cfg.FindMissingMaterials     = Bool("FindMissingMaterials");
            cfg.ExportRoomAsAttribute    = Bool("ExportRoomAsAttribute");
            cfg.ExportRoomGeometry       = Bool("ExportRoomGeometry");

            return cfg;
        }

        public void SaveToFile(string path)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("# JFTools NWC Export Settings");
            sb.AppendLine($"Coordinates={Coordinates}");
            sb.AppendLine($"Parameters={Parameters}");
            sb.AppendLine($"ExportLinks={ExportLinks}");
            sb.AppendLine($"ConvertElementProperties={ConvertElementProperties}");
            sb.AppendLine($"DivideFileIntoLevels={DivideFileIntoLevels}");
            sb.AppendLine($"FacetingFactor={FacetingFactor.ToString(inv)}");
            sb.AppendLine($"ExportUrls={ExportUrls}");
            sb.AppendLine($"FindMissingMaterials={FindMissingMaterials}");
            sb.AppendLine($"ExportRoomAsAttribute={ExportRoomAsAttribute}");
            sb.AppendLine($"ExportRoomGeometry={ExportRoomGeometry}");
            File.WriteAllText(path, sb.ToString());
        }
    }

    public class ExportSettings
    {
        public bool ExportPdf { get; set; }
        public bool ExportDwg { get; set; }
        public string OutputFolder { get; set; }
        public bool UseSubfolders { get; set; }
        public bool UseProjectSubfolders { get; set; }
        public string ProfileName { get; set; }
        public Dictionary<string, string> Profiles { get; set; }

        // PDF
        public bool UseRaster { get; set; }
        public RasterQualityType RasterQuality { get; set; } = RasterQualityType.High;
        public ColorDepthType ColorMode { get; set; } = ColorDepthType.Color;
        public bool HideRef { get; set; }
        public bool HideTags { get; set; }
        public bool HideScope { get; set; }
        public bool HideCrop { get; set; }
        public bool PdfSingle { get; set; }
        public string PdfSingleName { get; set; } = "Pranchas";

        // DWG
        public string DwgSetup { get; set; }
        public Dictionary<int, string> DwgSetupPerDoc { get; set; } = new Dictionary<int, string>();
        public bool BindImagesAsOle { get; set; }
        public DwgExportConfig DwgConfig { get; set; } = new DwgExportConfig();

        // IFC
        public bool ExportIfc { get; set; }
        public IfcExportConfig IfcConfig { get; set; } = new IfcExportConfig();
        // Documents to export as IFC (per-doc, not per-sheet); if null, derived from items
        public List<Document> IfcDocuments { get; set; }

        // NWC
        public bool ExportNwc { get; set; }
        public NwcExportConfig NwcConfig { get; set; } = new NwcExportConfig();
        public List<Document> NwcDocuments { get; set; }
    }

    public class ExportResult
    {
        public int Processed { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public bool Cancelled { get; set; }
    }

    public static class ExportHelper
    {
        public static ExportResult RunExport(
            List<Models.SheetItem> items,
            ExportSettings s,
            Action<int, int, string> onProgress,
            Func<bool> isCancelled)
        {
            var result = new ExportResult();
            int uniqueDocCount = items.GroupBy(i => i.DocRef.GetHashCode()).Count();
            int ifcDocCount = s.ExportIfc
                ? (s.IfcDocuments != null ? s.IfcDocuments.Count : uniqueDocCount)
                : 0;
            int nwcDocCount = s.ExportNwc
                ? (s.NwcDocuments != null ? s.NwcDocuments.Count : uniqueDocCount)
                : 0;
            int formatCount = (s.ExportPdf ? 1 : 0) + (s.ExportDwg ? 1 : 0);
            int total = items.Count * formatCount + ifcDocCount + nwcDocCount;
            int step = 0;

            string GetFolder(string formatSub, string projectName = null)
            {
                string p = s.OutputFolder;
                if (s.UseProjectSubfolders && !string.IsNullOrEmpty(projectName))
                    p = Path.Combine(p, projectName);
                if (s.UseSubfolders) p = Path.Combine(p, formatSub);
                Directory.CreateDirectory(p);
                return p;
            }

            // Single combined PDF
            if (s.ExportPdf && s.PdfSingle)
            {
                onProgress(step, total, "PDF: combining...");
                try
                {
                    var validItems = items.Where(i => i.Sheet != null && i.Sheet.IsValidObject && i.DocRef != null && i.DocRef.IsValidObject).ToList();
                    var byDoc = validItems.GroupBy(i => i.DocRef.GetHashCode())
                                          .Select(g => new { Doc = g.First().DocRef, Sheets = g.ToList() });
                    foreach (var group in byDoc)
                    {
                        string projName = NamingHelper.DocShortTitle(group.Doc);
                        string pdfFolder = GetFolder("PDF", projName);
                        var ids = group.Sheets.Select(si => si.Sheet.Id).ToList();
                        if (ids.Count == 0) continue;
                        string fname = byDoc.Count() == 1 ? s.PdfSingleName
                            : s.PdfSingleName + "_" + projName;

                        var opts = CreatePdfOptions(s);
                        opts.FileName = fname;
                        try { opts.Combine = true; } catch { }
                        bool ok = group.Doc.Export(pdfFolder, ids, opts);
                        if (!ok)
                            result.Errors.Add($"[PDF combined] {projName}: " + L.ErrFileNotCreated);
                    }
                }
                catch (Exception ex) { result.Errors.Add("[PDF combined]: " + ex.Message); }
                step += items.Count;
                onProgress(step, total, "PDF: combined");
            }

            // Per-sheet export
            foreach (var item in items)
            {
                if (isCancelled()) { result.Cancelled = true; break; }

                var sheet = item.Sheet;
                var doc = item.DocRef;

                string num = item.Number ?? (sheet != null && sheet.IsValidObject ? sheet.SheetNumber : "??");
                string name = item.Name ?? (sheet != null && sheet.IsValidObject ? sheet.Name : "??");
                string sheetLabel = $"{num} \"{name}\"";

                if (sheet == null || !sheet.IsValidObject || doc == null || !doc.IsValidObject)
                {
                    result.Errors.Add("[Export] " + L.ErrSheetInvalid(sheetLabel));
                    step += (s.ExportPdf && !s.PdfSingle ? 1 : 0) + (s.ExportDwg ? 1 : 0);
                    continue;
                }

                var ids = new List<ElementId> { sheet.Id };
                string fileName = NamingHelper.GenerateName(sheet, s.ProfileName, s.Profiles, doc, num, name);

                if (s.ExportPdf && !s.PdfSingle)
                {
                    onProgress(step, total, "PDF: " + num);
                    try
                    {
                        string pdfFolder = GetFolder("PDF", item.DocTitle);
                        var opts = CreatePdfOptions(s);
                        opts.FileName = fileName;
                        bool ok = doc.Export(pdfFolder, ids, opts);
                        if (!ok)
                            result.Errors.Add($"[PDF] {sheetLabel}: " + L.ErrFileNotCreated);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"[PDF] {sheetLabel}: {ex.Message}");
                    }
                    step++;
                    onProgress(step, total, "PDF: " + num);
                }

                if (s.ExportDwg)
                {
                    onProgress(step, total, "DWG: " + num);
                    try
                    {
                        string dwgFolder = GetFolder("DWG", item.DocTitle);
                        string setup = s.DwgSetupPerDoc.ContainsKey(doc.GetHashCode())
                            ? s.DwgSetupPerDoc[doc.GetHashCode()] : s.DwgSetup;

                        DWGExportOptions opt;
                        if (setup == InSessionSetupName)
                        {
                            opt = s.DwgConfig.ToExportOptions();
                        }
                        else
                        {
                            opt = null;
                            try
                            {
                                var setups = BaseExportOptions.GetPredefinedSetupNames(doc).ToList();
                                string setupName = setups.Contains(setup) ? setup
                                            : setups.Count > 0 ? setups[0] : null;
                                if (setupName != null)
                                    opt = DWGExportOptions.GetPredefinedOptions(doc, setupName);
                            }
                            catch { }
                            if (opt == null) opt = new DWGExportOptions();
                            // Overlay the panel values on top of the named setup
                            s.DwgConfig.ApplyToOptions(opt);
                        }

                        bool ok = doc.Export(dwgFolder, fileName, ids, opt);
                        if (!ok)
                        {
                            result.Errors.Add($"[DWG] {sheetLabel}: " + L.ErrDwgNotCreated);
                        }
                        else if (s.BindImagesAsOle)
                        {
                            string fullDwg = System.IO.Path.Combine(dwgFolder, fileName + ".dwg");
                            string oleErr = OleImageBinder.BindImages(fullDwg);
                            if (oleErr != null)
                                result.Errors.Add($"[OLE] {sheetLabel}: {oleErr}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"[DWG] {sheetLabel}: {ex.Message}");
                    }
                    step++;
                    onProgress(step, total, "DWG: " + num);
                }

                result.Processed++;
            }

            // IFC export: one file per unique document
            if (s.ExportIfc && !result.Cancelled)
            {
                List<(Document Doc, string Title)> docGroups;
                if (s.IfcDocuments != null && s.IfcDocuments.Count > 0)
                    docGroups = s.IfcDocuments.Select(d => (d, NamingHelper.DocShortTitle(d))).ToList();
                else
                    docGroups = items
                        .GroupBy(i => i.DocRef.GetHashCode())
                        .Select(g => (g.First().DocRef, g.First().DocTitle))
                        .ToList();

                foreach (var (doc, title) in docGroups)
                {
                    if (isCancelled()) { result.Cancelled = true; break; }
                    string projName = NamingHelper.DocShortTitle(doc);
                    onProgress(step, total, "IFC: " + projName);
                    try
                    {
                        string ifcFolder = GetFolder("IFC", title);
                        ExportIfcDocument(doc, ifcFolder, projName, s);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"[IFC] {projName}: {ex.Message}");
                    }
                    step++;
                    onProgress(step, total, "IFC: " + projName);
                }
            }

            // NWC export: one file per unique document
            if (s.ExportNwc && !result.Cancelled)
            {
                List<(Document Doc, string Title)> nwcGroups;
                if (s.NwcDocuments != null && s.NwcDocuments.Count > 0)
                    nwcGroups = s.NwcDocuments.Select(d => (d, NamingHelper.DocShortTitle(d))).ToList();
                else
                    nwcGroups = items
                        .GroupBy(i => i.DocRef.GetHashCode())
                        .Select(g => (g.First().DocRef, g.First().DocTitle))
                        .ToList();

                foreach (var (doc, title) in nwcGroups)
                {
                    if (isCancelled()) { result.Cancelled = true; break; }
                    string projName = NamingHelper.DocShortTitle(doc);
                    onProgress(step, total, "NWC: " + projName);
                    try
                    {
                        string nwcFolder = GetFolder("NWC", title);
                        ExportNwcDocument(doc, nwcFolder, projName, s);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"[NWC] {projName}: {ex.Message}");
                    }
                    step++;
                    onProgress(step, total, "NWC: " + projName);
                }
            }

            return result;
        }

        // ── NWC export ───────────────────────────────────────────────
        private static void ExportNwcDocument(Document doc, string folder, string fileName, ExportSettings s)
        {
            var cfg = s.NwcConfig ?? new NwcExportConfig();
            var opts = new NavisworksExportOptions();
            opts.ExportScope = NavisworksExportScope.Model;
            opts.Coordinates = cfg.Coordinates == "Internal"
                ? NavisworksCoordinates.Internal
                : NavisworksCoordinates.Shared;
            opts.ExportLinks = cfg.ExportLinks;
            opts.ConvertElementProperties = cfg.ConvertElementProperties;
            opts.DivideFileIntoLevels = cfg.DivideFileIntoLevels;
            try { opts.Parameters = (NavisworksParameters)Enum.Parse(typeof(NavisworksParameters), cfg.Parameters); }
            catch { opts.Parameters = NavisworksParameters.None; }

            // Advanced
            opts.FacetingFactor       = cfg.FacetingFactor;
            opts.ExportUrls           = cfg.ExportUrls;
            opts.FindMissingMaterials = cfg.FindMissingMaterials;
            opts.ExportRoomAsAttribute = cfg.ExportRoomAsAttribute;
            opts.ExportRoomGeometry   = cfg.ExportRoomGeometry;

            doc.Export(folder, fileName, opts);
        }

        // ── IFC link info ────────────────────────────────────────────
        private class LinkInfo
        {
            public RevitLinkInstance Instance;
            public Document LinkedDoc;
            public Transform Trf;
            public string IfcGuid;
        }

        private static void ExportIfcDocument(Document doc, string folder, string fileName, ExportSettings s)
        {
            var cfg = s.IfcConfig ?? new IfcExportConfig();
            bool linkedEmbedded = cfg.LinkedFilesMode == "Embedded";
            bool linkedSeparate = cfg.LinkedFilesMode == "Separate";

            // Collect valid loaded link instances (conformal, not reflected, not scaled)
            var links = (linkedEmbedded || linkedSeparate) ? CollectValidLinks(doc) : null;

            var opts = BuildIfcOptions(cfg, linkedEmbedded, links);

            // IFC export may write IFC GUIDs to elements — requires an open transaction
            using (var tx = new Transaction(doc, "IFC Export"))
            {
                tx.Start();
                doc.Export(folder, fileName, opts);
                tx.Commit();
            }

            // Separate mode: export each linked document as its own IFC file
            if (linkedSeparate && links != null && links.Count > 0)
            {
                var linkErrors = ExportLinkedDocumentsSeparately(doc, folder, fileName, links, cfg);
                if (linkErrors.Count > 0)
                    throw new Exception("Linked file errors:\n" + string.Join("\n", linkErrors));
            }
        }

        // Collect all loaded, valid link instances from the host document
        private static List<LinkInfo> CollectValidLinks(Document doc)
        {
            var result = new List<LinkInfo>();
            var instances = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>();

            foreach (var link in instances)
            {
                Document linkedDoc;
                try { linkedDoc = link.GetLinkDocument(); } catch { continue; }
                if (linkedDoc == null) continue; // unloaded or missing

                Transform trf;
                try { trf = link.GetTransform(); } catch { continue; }

                // Skip non-conformal, reflected, or scaled transforms (same filters as native exporter)
                if (!trf.IsConformal) continue;
                if (Math.Abs(trf.Determinant - 1.0) > 1e-6) continue; // scaled

                result.Add(new LinkInfo
                {
                    Instance  = link,
                    LinkedDoc = linkedDoc,
                    Trf       = trf,
                    IfcGuid   = GetIfcGuid(link),
                });
            }
            return result;
        }

        private static IFCExportOptions BuildIfcOptions(IfcExportConfig cfg, bool exportLinkedEmbedded,
                                                        List<LinkInfo> links = null)
        {
            var opts = new IFCExportOptions();
            opts.FileVersion          = ParseIfcVersion(cfg.IfcVersion);
            opts.SpaceBoundaryLevel   = cfg.SpaceBoundaries;
            opts.ExportBaseQuantities = cfg.ExportBaseQuantities;
            opts.WallAndColumnSplitting = cfg.SplitWallsAndColumns;

            try
            {
                string revitDir  = Path.GetDirectoryName(typeof(Document).Assembly.Location);
                string commonDll = Path.Combine(revitDir, "Revit.IFC.Common.dll");
                if (File.Exists(commonDll)) try { System.Reflection.Assembly.LoadFrom(commonDll); } catch { }

                string uiDll = Path.Combine(revitDir, "AddIns", "IFCExporterUI", "Autodesk.IFC.Export.UI.dll");
                if (File.Exists(uiDll))
                {
                    var asm = System.Reflection.Assembly.LoadFrom(uiDll);
                    var t   = asm.GetType("BIM.IFC.Export.UI.IFCExportConfiguration");
                    if (t != null)
                    {
                        var config = t.GetMethod("CreateDefaultConfiguration").Invoke(null, null);

                        SetProp(t, config, "IFCVersion",             opts.FileVersion);
                        SetEnumProp(t, config, "IFCFileType",        cfg.FileType);
                        SetEnumProp(t, config, "SitePlacement",      cfg.SitePlacement);
                        SetEnumProp(t, config, "ExchangeRequirement",cfg.ExchangeRequirement);

                        SetProp(t, config, "SpaceBoundaries",                 cfg.SpaceBoundaries);
                        SetProp(t, config, "Export2DElements",                cfg.Export2DElements);
                        SetProp(t, config, "ExportRoomsInView",               cfg.ExportRoomsInView);
                        SetProp(t, config, "ExportPartsAsBuildingElements",   cfg.ExportPartsAsBuildingElements);
                        SetProp(t, config, "ExportSolidModelRep",             cfg.ExportSolidModelRep);
                        SetProp(t, config, "IncludeSiteElevation",            cfg.IncludeSiteElevation);
                        SetProp(t, config, "IncludeSteelElements",            cfg.IncludeSteelElements);
                        SetProp(t, config, "ExportLinkedFiles",               exportLinkedEmbedded);
                        SetProp(t, config, "VisibleElementsOfCurrentView",    cfg.VisibleElementsOfCurrentView);
                        SetProp(t, config, "UseActiveViewGeometry",           cfg.UseActiveViewGeometry);
                        SetProp(t, config, "ExportBaseQuantities",            cfg.ExportBaseQuantities);
                        SetProp(t, config, "ExportIFCCommonPropertySets",     cfg.ExportIFCCommonPropertySets);
                        SetProp(t, config, "ExportInternalRevitPropertySets", cfg.ExportInternalRevitPropertySets);
                        SetProp(t, config, "ExportSchedulesAsPsets",          cfg.ExportSchedulesAsPsets);
                        SetProp(t, config, "ExportMaterialPsets",             cfg.ExportMaterialPsets);
                        SetProp(t, config, "ExportUserDefinedPsets",          cfg.ExportUserDefinedPsets);
                        SetProp(t, config, "ExportUserDefinedPsetsFileName",  cfg.ExportUserDefinedPsetsFileName ?? "");
                        SetProp(t, config, "ExportUserDefinedParameterMapping",         cfg.ExportUserDefinedParameterMapping);
                        SetProp(t, config, "ExportUserDefinedParameterMappingFileName", cfg.ExportUserDefinedParameterMappingFileName ?? "");
                        SetProp(t, config, "TessellationLevelOfDetail",       cfg.TessellationLevelOfDetail);
                        SetProp(t, config, "SplitWallsAndColumns",            cfg.SplitWallsAndColumns);
                        SetProp(t, config, "StoreIFCGUID",                    cfg.StoreIFCGUID);
                        SetProp(t, config, "UseOnlyTriangulation",            cfg.UseOnlyTriangulation);
                        SetProp(t, config, "Use2DRoomBoundaryForVolume",      cfg.Use2DRoomBoundaryForVolume);
                        SetProp(t, config, "UseFamilyAndTypeNameForReference",cfg.UseFamilyAndTypeNameForReference);
                        SetProp(t, config, "UseTypeNameOnlyForIfcType",       cfg.UseTypeNameOnlyForIfcType);
                        SetProp(t, config, "UseVisibleRevitNameAsEntityName", cfg.UseVisibleRevitNameAsEntityName);

                        // UpdateOptions populates all IFCExportOptions.AddOption() entries
                        t.GetMethod("UpdateOptions").Invoke(config, new object[] { opts, ElementId.InvalidElementId });
                    }
                }
            }
            catch { }

            // ── Federated (embedded) linked file export ───────────────────────────────
            // Matches IFCCommandOverrideApplication.OnIFCExport logic from revit-ifc source.
            // The core IFC exporter reads "ExportingLinks" + "FederatedLinkInfo" to include
            // linked elements in the host IFC output.
            if (exportLinkedEmbedded && links != null && links.Count > 0)
            {
                var seenGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var sb        = new System.Text.StringBuilder();
                foreach (var lk in links)
                {
                    string guid = lk.IfcGuid;
                    while (seenGuids.Contains(guid.ToLower())) guid += "-";
                    seenGuids.Add(guid.ToLower());
                    sb.Append(lk.Instance.Id.ToString()).Append(',').Append(guid).Append(';');
                }
                opts.AddOption("ExportingLinks",   "ExportAsFederated");
                opts.AddOption("FederatedLinkInfo", sb.ToString());
                opts.AddOption("ExportLinkedFiles", "true"); // belt-and-suspenders
            }

            return opts;
        }

        // ── Separate linked file export ───────────────────────────────────────────────
        // Revit 2023 pattern (from revit-ifc Release_23.x.x):
        //   Use linkDoc = link.GetLinkDocument() — already loaded in Revit memory.
        //   Open a Transaction on linkDoc, export, then RollBack.
        //   Do NOT use Application.OpenDocumentFile() — Revit rejects transactions on those.
        private static List<string> ExportLinkedDocumentsSeparately(
            Document hostDoc, string folder, string hostFileName,
            List<LinkInfo> links, IfcExportConfig cfg)
        {
            var errors = new List<string>();

            // Group by underlying linked file path (one export per unique .rvt, even if placed multiple times)
            var byPath = links
                .GroupBy(l => string.IsNullOrEmpty(l.LinkedDoc.PathName)
                              ? l.Instance.UniqueId
                              : l.LinkedDoc.PathName)
                .ToList();

            string hostBase = Path.GetFileNameWithoutExtension(hostFileName);

            foreach (var group in byPath)
            {
                var instances = group.ToList();
                var linkedDoc = instances[0].LinkedDoc; // GetLinkDocument() — already in Revit memory
                string baseName = GetLinkBaseName(linkedDoc);
                bool multiInst  = instances.Count > 1;

                // Scale factor: convert Origin from Revit internal units (feet) to the link
                // document's own display length unit — matches revit-ifc Release_23.x.x exactly.
                double lengthScaleFactorLink = UnitUtils.ConvertFromInternalUnits(
                    1.0,
                    linkedDoc.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId());

                // Build export options for this linked file
                var linkOpts = BuildIfcOptions(cfg, exportLinkedEmbedded: false);
                linkOpts.AddOption("ExportingLinks", "True"); // Revit 2023: bool string, not enum name

                int numInstances = instances.Count;
                linkOpts.AddOption("NumberOfExportedLinkInstances", numInstances.ToString());

                var fileNames = new List<string>();
                for (int i = 0; i < numInstances; i++)
                {
                    var    info = instances[i];
                    string name = hostBase + "-" + baseName;
                    if (multiInst) name += "-" + (i + 1).ToString();
                    name += ".ifc";
                    fileNames.Add(name);

                    string suffix = i == 0 ? "" : (i + 1).ToString();
                    linkOpts.AddOption("ExportLinkId" + suffix,
                                       info.Instance.Id.IntegerValue.ToString());

                    // Scale Origin to link document units; BasisX/Y/Z are pure direction vectors — not scaled
                    linkOpts.AddOption("ExportLinkInstanceTransform" + suffix,
                                       SerializeTransformScaled(info.Trf, lengthScaleFactorLink));

                    // Index 0 filename is passed as the Export() fileName arg; subsequent ones go in options
                    if (i > 0)
                        linkOpts.AddOption("ExportLinkInstanceFileName" + (i + 1).ToString(),
                                           Path.Combine(folder, fileNames[i]));
                }

                try
                {
                    var tx = new Transaction(linkedDoc, "IFC Export Linked");
                    tx.Start();
                    // Matches revit-ifc: prevent failure dialogs from interrupting the export
                    var failOpts = tx.GetFailureHandlingOptions();
                    failOpts.SetClearAfterRollback(false);
                    tx.SetFailureHandlingOptions(failOpts);
                    try
                    {
                        linkedDoc.Export(folder, fileNames[0], linkOpts);
                    }
                    finally
                    {
                        tx.RollBack();
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{baseName}: {ex.Message}");
                }
            }

            return errors;
        }

        private static string GetLinkBaseName(Document linkedDoc)
        {
            string path = linkedDoc.PathName;
            return string.IsNullOrEmpty(path) ? "LinkedModel" : Path.GetFileNameWithoutExtension(path);
        }

        // Serialize transform with Origin pre-scaled to the link document's display units.
        // Matches revit-ifc Release_23.x.x: tr.Origin *= lengthScaleFactorLink before SerializeXYZ.
        // BasisX/Y/Z are pure direction vectors — NOT scaled.
        // Parser (ExportOptionsCache.ParseTransform) splits on ";" and parses each XYZ
        // using separators: "," "(" ")" " " with InvariantCulture — compatible with XYZ.ToString().
        private static string SerializeTransformScaled(Transform tr, double originScale)
        {
            XYZ scaledOrigin = tr.Origin * originScale;
            return scaledOrigin.ToString() + ";" + tr.BasisX.ToString() + ";"
                 + tr.BasisY.ToString()    + ";" + tr.BasisZ.ToString() + ";";
        }

        // ── IFC GUID helpers ─────────────────────────────────────────────────────────
        // buildingSMART base-64 character set (IFC specification, section 2.13.3)
        private static readonly char[] s_IfcBase64 =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$".ToCharArray();

        // Convert a .NET Guid to the 22-character IFC GUID format.
        // Algorithm: buildingSMART reference implementation (cv_to_64).
        private static string ConvertToIfcGuid(Guid guid)
        {
            var b = guid.ToByteArray(); // .NET byte order: d1(4,LE) d2(2,LE) d3(2,LE) d4(8,BE)
            uint  d1 = (uint)(b[0] | (b[1] << 8) | (b[2] << 16) | (b[3] << 24));
            ushort d2 = (ushort)(b[4] | (b[5] << 8));
            ushort d3 = (ushort)(b[6] | (b[7] << 8));

            // Six numeric groups matching the buildingSMART reference
            ulong[] num = new ulong[6];
            num[0] = d1 >> 24;                                           // top 8 bits of Data1
            num[1] = d1 & 0xFFFFFF;                                      // lower 24 bits of Data1
            num[2] = (ulong)d2 * 256 + (uint)(d3 >> 8);                 // Data2 + upper byte Data3
            num[3] = (ulong)(d3 & 0xFF) * 65536 + b[8] * 256ul + b[9]; // lower byte Data3 + Data4[0-1]
            num[4] = b[10] * 65536ul + b[11] * 256ul + b[12];           // Data4[2-4]
            num[5] = b[13] * 65536ul + b[14] * 256ul + b[15];           // Data4[5-7]

            int[] lengths = { 2, 4, 4, 4, 4, 4 };
            var result = new char[22];
            int pos = 0;
            for (int g = 0; g < 6; g++)
            {
                ulong val = num[g];
                for (int j = lengths[g] - 1; j >= 0; j--)
                {
                    result[pos + j] = s_IfcBase64[val % 64];
                    val /= 64;
                }
                pos += lengths[g];
            }
            return new string(result);
        }

        // Get/derive IFC GUID for a Revit element.
        // Priority: IFC_GUID built-in parameter → derived from UniqueId.
        private static string GetIfcGuid(Element element)
        {
            try
            {
                var param = element.get_Parameter(BuiltInParameter.IFC_GUID);
                if (param != null && param.HasValue)
                {
                    string v = param.AsString();
                    if (!string.IsNullOrEmpty(v) && v.Length == 22) return v;
                }
            }
            catch { }

            try
            {
                // Revit UniqueId: "<36-char-UUID>-<8-char-elementId-hex>"
                var guid = new Guid(element.UniqueId.Substring(0, 36));
                return ConvertToIfcGuid(guid);
            }
            catch { }

            return ConvertToIfcGuid(Guid.NewGuid());
        }

        private static void SetProp(Type t, object obj, string name, object value)
        {
            try { t.GetProperty(name)?.SetValue(obj, value); } catch { }
        }

        private static void SetEnumProp(Type t, object obj, string name, string enumValue)
        {
            try
            {
                var prop = t.GetProperty(name);
                if (prop == null) return;
                prop.SetValue(obj, Enum.Parse(prop.PropertyType, enumValue));
            }
            catch { }
        }

        private static IFCVersion ParseIfcVersion(string name)
        {
            switch (name)
            {
                case "IFC2x3CV2":  return IFCVersion.IFC2x3CV2;
                case "IFC2x3":     return IFCVersion.IFC2x3;
                case "IFC4RV":     return IFCVersion.IFC4RV;
                case "IFC4DTV":    return IFCVersion.IFC4DTV;
                case "IFC4":       return IFCVersion.IFC4;
                case "IFCCOBIE":   return IFCVersion.IFCCOBIE;
                case "IFC2x3FM":   return IFCVersion.IFC2x3FM;
                case "IFC2x3BFM":  return IFCVersion.IFC2x3BFM;
                case "IFC2x2":     return IFCVersion.IFC2x2;
                case "IFCBCA":     return IFCVersion.IFCBCA;
                case "IFC4x3":     return IFCVersion.IFC4x3;
                default:           return IFCVersion.IFC2x3CV2;
            }
        }

        private static PDFExportOptions CreatePdfOptions(ExportSettings s)
        {
            var opts = new PDFExportOptions();
            opts.AlwaysUseRaster = s.UseRaster;
            opts.RasterQuality = s.RasterQuality;
            opts.ColorDepth = s.ColorMode;
            opts.HideReferencePlane = s.HideRef;
            opts.HideUnreferencedViewTags = s.HideTags;
            opts.HideScopeBoxes = s.HideScope;
            opts.HideCropBoundaries = s.HideCrop;
            return opts;
        }

        /// <summary>Sentinel value that identifies the "In-Session" option in the setup combo.</summary>
        public const string InSessionSetupName = "<In-Session>";

        public static List<string> GetDwgSetups(Document doc)
        {
            var list = new List<string> { InSessionSetupName };
            try { list.AddRange(BaseExportOptions.GetPredefinedSetupNames(doc)); }
            catch { }
            return list;
        }

        public static List<string> GetDwgSetups(IEnumerable<Document> docs)
        {
            var seen = new List<string> { InSessionSetupName };
            foreach (var d in docs)
            {
                foreach (var s in GetDwgSetups(d))
                {
                    if (s != InSessionSetupName && !seen.Contains(s)) seen.Add(s);
                }
            }
            return seen;
        }
    }
}
