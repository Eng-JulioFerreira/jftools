using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace JFTools.Helpers
{
    public static class ExcelHelper
    {
        // ── Worksheet discovery ───────────────────────────────────────────────

        public static List<string> GetWorksheetNames(string filePath)
        {
            var result = new List<string>();
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var pkg = new ExcelPackage(fs))
                foreach (var ws in pkg.Workbook.Worksheets)
                    result.Add(ws.Name);
            return result;
        }

        // ── Export: schedule → .xlsx for parameter editing ───────────────────

        /// <summary>
        /// Exports elements visible in <paramref name="schedule"/> to Excel.
        /// Row 1  = column headers (ColumnHeading as shown in Revit).
        /// Row 2  = internal parameter names (hidden — used as import key).
        /// Row 3+ = element data; column 1 = UniqueId (hidden, import key).
        /// Based on the bimone/addins-excelexporterimporter approach.
        /// </summary>
        public static void ExportScheduleForEditing(ViewSchedule schedule, string filePath)
        {
            var doc = schedule.Document;
            var def = schedule.Definition;

            // Collect visible, non-formula fields
            var fields = new List<ScheduleField>();
            for (int i = 0; i < def.GetFieldCount(); i++)
            {
                var f = def.GetField(i);
                if (f.IsHidden) continue;
                // Skip fields with no backing parameter (counts, formulas, etc.)
                if (f.ParameterId == ElementId.InvalidElementId) continue;
                fields.Add(f);
            }

            // Collect elements visible in the schedule
            List<Element> elements;
            try
            {
                elements = new FilteredElementCollector(doc, schedule.Id)
                    .WhereElementIsNotElementType()
                    .ToList();
            }
            catch
            {
                // Fallback for schedule types that don't support view-scoped collector
                elements = new FilteredElementCollector(doc)
                    .OfCategoryId(def.CategoryId)
                    .WhereElementIsNotElementType()
                    .ToList();
            }

            // Sort elements to match the schedule's sort/group order
            elements = SortElements(elements, def);

            using (var pkg = new ExcelPackage())
            {
                var ws = pkg.Workbook.Worksheets.Add(SanitizeName(schedule.Name, 31));

                int totalCols = fields.Count + 1; // +1 for UniqueId

                // ── Row 1: Display headers (what Revit shows) ─────────────────
                ws.Cells[1, 1].Value = "UniqueId";
                for (int c = 0; c < fields.Count; c++)
                    ws.Cells[1, c + 2].Value = fields[c].ColumnHeading;

                using (var hdr = ws.Cells[1, 1, 1, totalCols])
                {
                    hdr.Style.Font.Bold = true;
                    hdr.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    hdr.Style.Fill.BackgroundColor.SetColor(
                        System.Drawing.Color.FromArgb(0xD9, 0xE1, 0xF2));
                    hdr.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                }

                // ── Row 2: Parameter names (hidden — used for import matching) ─
                ws.Cells[2, 1].Value = "_uid_";
                for (int c = 0; c < fields.Count; c++)
                    ws.Cells[2, c + 2].Value = fields[c].GetName();

                ws.Row(2).Hidden = true;

                // ── Row 3+: Data ──────────────────────────────────────────────
                int excelRow = 3;
                foreach (var elem in elements)
                {
                    ws.Cells[excelRow, 1].Value = elem.UniqueId;

                    for (int c = 0; c < fields.Count; c++)
                    {
                        string val = string.Empty;
                        try
                        {
                            Parameter param = GetParam(elem, fields[c]);
                            if (param != null)
                                val = param.AsValueString() ?? param.AsString() ?? string.Empty;
                        }
                        catch { }
                        ws.Cells[excelRow, c + 2].Value = val;
                    }
                    excelRow++;
                }

                // ── Column styling ────────────────────────────────────────────
                // UniqueId column: hidden (key column, not meant for editing)
                ws.Column(1).Hidden = true;

                // Auto-fit data columns
                for (int c = 2; c <= totalCols; c++)
                    ws.Column(c).AutoFit(10, 60);

                // Freeze row 1 + col 1 (header + UniqueId)
                ws.View.FreezePanes(3, 2);

                // Auto-filter on header row
                if (excelRow > 3)
                    ws.Cells[1, 1, 1, totalCols].AutoFilter = true;

                pkg.SaveAs(new FileInfo(filePath));
            }
        }

        // ── Import: .xlsx → update Revit element parameters ──────────────────

        /// <summary>
        /// Reads an exported schedule worksheet and updates element parameters.
        /// Expects row 1 = display headers, row 2 (hidden) = parameter names,
        /// column 1 = UniqueId. Returns a result log.
        /// </summary>
        public static List<string> ImportParametersFromExcel(
            string filePath, string worksheetName, Document doc)
        {
            var log = new List<string>();

            // ── Read Excel ────────────────────────────────────────────────────
            List<string> displayHeaders; // row 1
            List<string> paramNames;     // row 2 (hidden) — actual parameter names
            var dataRows = new List<(string uid, List<string> values)>();

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var pkg = new ExcelPackage(fs))
            {
                var ws = pkg.Workbook.Worksheets[worksheetName];
                if (ws == null || ws.Dimension == null)
                {
                    log.Add("Error: worksheet not found or empty.");
                    return log;
                }

                int lastCol = ws.Dimension.End.Column;
                int lastRow = ws.Dimension.End.Row;

                displayHeaders = ReadRow(ws, 1, lastCol);
                paramNames     = ReadRow(ws, 2, lastCol);

                // If row 2 looks like parameter names (starts with "_uid_"), use it;
                // otherwise fall back to display headers for parameter matching
                bool hasParamRow = paramNames.Count > 0 &&
                                   paramNames[0] == "_uid_";

                // Use paramNames when available, else displayHeaders
                var importKeys = hasParamRow ? paramNames : displayHeaders;

                // Data starts at row 3 (if param row exists) or row 2 (legacy)
                int dataStart = hasParamRow ? 3 : 2;

                int uidCol = importKeys.IndexOf(hasParamRow ? "_uid_" : "UniqueId");
                if (uidCol < 0)
                {
                    log.Add("Error: UniqueId column not found. Export this schedule from JFTools first.");
                    return log;
                }

                for (int r = dataStart; r <= lastRow; r++)
                {
                    var row = ReadRow(ws, r, lastCol);
                    string uid = uidCol < row.Count ? row[uidCol] : string.Empty;
                    if (!string.IsNullOrWhiteSpace(uid))
                        dataRows.Add((uid, row));
                }

                // Store importKeys for use below (need column→paramName mapping)
                // uidCol is already known
                // We'll use importKeys[c] as the parameter name for column c
                if (dataRows.Count == 0)
                {
                    log.Add("No data rows found.");
                    return log;
                }

                // ── Apply in transaction ──────────────────────────────────────
                int updated = 0, skipped = 0, notFound = 0;

                using (var tx = new Transaction(doc, "JFTools - Import Parameters from Excel"))
                {
                    tx.Start();

                    foreach (var (uid, row) in dataRows)
                    {
                        Element elem = doc.GetElement(uid);
                        if (elem == null) { notFound++; log.Add($"Not found: {uid}"); continue; }

                        for (int c = 0; c < importKeys.Count; c++)
                        {
                            // Skip UniqueId column and empty header columns
                            if (c == uidCol) continue;
                            string key = importKeys[c];
                            if (string.IsNullOrEmpty(key) || key == "_uid_") continue;

                            string newVal = c < row.Count ? row[c] : string.Empty;

                            Parameter param = elem.LookupParameter(key);
                            if (param == null || param.IsReadOnly) { skipped++; continue; }

                            try
                            {
                                bool set = false;
                                switch (param.StorageType)
                                {
                                    case StorageType.String:
                                        param.Set(newVal);
                                        set = true;
                                        break;

                                    case StorageType.Integer:
                                        if (newVal.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                                            { param.Set(1); set = true; }
                                        else if (newVal.Equals("No", StringComparison.OrdinalIgnoreCase))
                                            { param.Set(0); set = true; }
                                        else if (int.TryParse(newVal, out int iv))
                                            { param.Set(iv); set = true; }
                                        break;

                                    case StorageType.Double:
                                        // Numeric parameters with units cannot be reliably
                                        // round-tripped from display strings without the
                                        // parameter's unit spec. Skipped intentionally.
                                        skipped++;
                                        break;
                                }
                                if (set) updated++;
                                else if (param.StorageType != StorageType.Double) skipped++;
                            }
                            catch (Exception ex)
                            {
                                log.Add($"'{key}' on {elem.Name}: {ex.Message}");
                                skipped++;
                            }
                        }
                    }

                    tx.Commit();
                }

                log.Insert(0,
                    $"Done: {updated} value(s) updated, {skipped} skipped, {notFound} element(s) not found.");
            }

            return log;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static Parameter GetParam(Element elem, ScheduleField field)
        {
            int pid = (int)field.ParameterId.IdValue();
            if (pid < 0) // BuiltInParameter
            {
                var p = elem.get_Parameter((BuiltInParameter)pid);
                if (p != null) return p;
            }
            // Shared parameter or project parameter — look up by name
            return elem.LookupParameter(field.GetName());
        }

        private static List<Element> SortElements(List<Element> elems, ScheduleDefinition def)
        {
            // Sort ascending by each sort/group field to approximate the schedule's display order.
            // Row order does not affect import correctness (matching is done by UniqueId).
            var sortFields = def.GetSortGroupFields();
            if (sortFields == null || sortFields.Count == 0) return elems;

            IOrderedEnumerable<Element> sorted = null;
            foreach (var sf in sortFields)
            {
                ScheduleField fld;
                try { fld = def.GetField(sf.FieldId); }
                catch { continue; }

                string Key(Element e)
                {
                    try
                    {
                        var p = GetParam(e, fld);
                        return p?.AsValueString() ?? p?.AsString() ?? string.Empty;
                    }
                    catch { return string.Empty; }
                }

                // SortOrder enum unavailable in this RevitAPI version — always sort ascending.
                // The visual order may differ from Revit, but import is unaffected.
                sorted = sorted == null ? elems.OrderBy(Key) : sorted.ThenBy(Key);
            }
            return sorted?.ToList() ?? elems;
        }

        private static List<string> ReadRow(ExcelWorksheet ws, int row, int lastCol)
        {
            var result = new List<string>();
            for (int c = 1; c <= lastCol; c++)
                result.Add(ws.Cells[row, c].Text ?? string.Empty);
            return result;
        }

        internal static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Length > 60 ? name.Substring(0, 60) : name;
        }

        private static string SanitizeName(string name, int maxLen)
        {
            foreach (char c in new[] { '\\', '/', '*', '?', ':', '[', ']' })
                name = name.Replace(c, '_');
            return name.Length > maxLen ? name.Substring(0, maxLen) : name;
        }
    }
}
