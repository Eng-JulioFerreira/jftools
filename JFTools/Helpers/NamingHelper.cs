using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace JFTools.Helpers
{
    public static class NamingHelper
    {
        public static Dictionary<string, string> LoadProfiles(string path)
        {
            var result = new Dictionary<string, string>();
            if (!File.Exists(path)) return result;
            foreach (var line in File.ReadAllLines(path))
            {
                int idx = line.IndexOf('=');
                if (idx > 0)
                {
                    string name = line.Substring(0, idx).Trim();
                    string pattern = line.Substring(idx + 1).Trim();
                    result[name] = pattern;
                }
            }
            return result;
        }

        public static string GenerateName(ViewSheet sheet, string profileName,
            Dictionary<string, string> profiles, Document doc, string defaultNum = null, string defaultName = null)
        {
            string numStr = defaultNum ?? (sheet != null && sheet.IsValidObject ? sheet.SheetNumber : "");
            string nameStr = defaultName ?? (sheet != null && sheet.IsValidObject ? sheet.Name : "");

            if (!profiles.TryGetValue(profileName, out string pattern) || string.IsNullOrEmpty(pattern))
                return SanitizeFileName($"{numStr}_{nameStr}");

            var sb = new StringBuilder();
            var token = new StringBuilder();
            bool reading = false;

            foreach (char c in pattern)
            {
                if (c == '{') { reading = true; token.Clear(); }
                else if (c == '}')
                {
                    reading = false;
                    string val = GetParamValue(sheet, token.ToString(), doc);
                    if (!string.IsNullOrEmpty(val)) sb.Append(val);
                }
                else if (reading) token.Append(c);
                else sb.Append(c);
            }

            string result = sb.ToString().Trim();
            string name = string.IsNullOrEmpty(result) ? $"{numStr}_{nameStr}" : result;
            return SanitizeFileName(name);
        }

        /// <summary>
        /// Replaces characters that are invalid in file names with a dash.
        /// Prevents export failures caused by slashes or other special characters in Revit parameter values.
        /// </summary>
        public static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '-');
            while (name.Contains("--"))
                name = name.Replace("--", "-");
            name = name.Trim('-').Trim();
            return string.IsNullOrEmpty(name) ? "unnamed" : name;
        }

        // Locale-independent mapping: English display name → BuiltInParameter
        private static readonly Dictionary<string, BuiltInParameter> _bipMap =
            new Dictionary<string, BuiltInParameter>(StringComparer.OrdinalIgnoreCase)
        {
            // Sheet
            { "Sheet Number",                  BuiltInParameter.SHEET_NUMBER },
            { "Sheet Name",                    BuiltInParameter.VIEW_NAME },
            { "Sheet Issue Date",              BuiltInParameter.SHEET_ISSUE_DATE },
            { "Drawn By",                      BuiltInParameter.SHEET_DRAWN_BY },
            { "Checked By",                    BuiltInParameter.SHEET_CHECKED_BY },
            { "Approved By",                   BuiltInParameter.SHEET_APPROVED_BY },
            { "Current Revision",              BuiltInParameter.SHEET_CURRENT_REVISION },
            // Project Information
            { "Project Name",                  BuiltInParameter.PROJECT_NAME },
            { "Project Number",                BuiltInParameter.PROJECT_NUMBER },
            { "Project Address",               BuiltInParameter.PROJECT_ADDRESS },
            { "Client Name",                   BuiltInParameter.CLIENT_NAME },
            { "Author",                        BuiltInParameter.PROJECT_AUTHOR },
            { "Project Status",                BuiltInParameter.PROJECT_STATUS },
            { "Project Issue Date",            BuiltInParameter.PROJECT_ISSUE_DATE },
        };

        public static string GetParamValue(Element element, string paramName, Document doc)
        {
            if (element == null || !element.IsValidObject) return "";
            if (doc == null || !doc.IsValidObject) return "";

            try
            {
                // Special case: revision date / description come from the Revision element
                if (element is ViewSheet vs &&
                    (paramName.Equals("Current Revision Date", StringComparison.OrdinalIgnoreCase) ||
                     paramName.Equals("Current Revision Description", StringComparison.OrdinalIgnoreCase)))
                {
                    var revId = vs.GetCurrentRevision();
                    if (revId != ElementId.InvalidElementId)
                    {
                        var rev = doc.GetElement(revId) as Revision;
                        if (rev != null && rev.IsValidObject)
                        {
                            return paramName.Equals("Current Revision Date", StringComparison.OrdinalIgnoreCase)
                                ? (rev.RevisionDate ?? "")
                                : (rev.Description ?? "");
                        }
                    }
                    return "";
                }

                // Try BuiltInParameter first (locale-independent)
                if (_bipMap.TryGetValue(paramName, out BuiltInParameter bip))
                {
                    foreach (var source in new Element[] { element, doc.ProjectInformation })
                    {
                        if (source == null || !source.IsValidObject) continue;
                        var p = source.get_Parameter(bip);
                        if (p != null)
                        {
                            string val = p.AsString() ?? p.AsValueString();
                            if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
                        }
                    }
                }

                // Fall back to string lookup (custom parameters)
                foreach (var source in new Element[] { element, doc.ProjectInformation })
                {
                    if (source == null || !source.IsValidObject) continue;
                    var p = source.LookupParameter(paramName);
                    if (p != null)
                    {
                        string val = p.AsString() ?? p.AsValueString();
                        if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
                    }
                }
            }
            catch
            {
                // Fallback on exception
            }
            return "";
        }

        // User-writable path: %APPDATA%\JFTools\export_profiles.txt
        public static string GetProfilesPath()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "JFTools");
            Directory.CreateDirectory(appData);
            string userPath = Path.Combine(appData, "export_profiles.txt");

            // First run: seed from the installed default next to the DLL
            if (!File.Exists(userPath))
            {
                string installDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string defaultPath = Path.Combine(installDir, "export_profiles.txt");
                if (File.Exists(defaultPath))
                    File.Copy(defaultPath, userPath);
                else
                    File.WriteAllText(userPath, "Default={Sheet Number}_{Sheet Name}\n", Encoding.UTF8);
            }

            return userPath;
        }

        public static void SaveProfiles(string path, Dictionary<string, string> profiles)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var lines = profiles.Select(kv => $"{kv.Key}={kv.Value}");
            File.WriteAllLines(path, lines, Encoding.UTF8);
        }

        public static string DocShortTitle(Document d)
        {
            if (!string.IsNullOrEmpty(d.PathName))
                return Path.GetFileNameWithoutExtension(d.PathName);
            return d.Title ?? "Untitled";
        }
    }
}
