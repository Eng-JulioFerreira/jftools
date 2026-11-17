using System.ComponentModel;
using Autodesk.Revit.DB;

namespace JFTools.Models
{
    public class SheetItem : INotifyPropertyChanged
    {
        private bool _isChecked;

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(nameof(IsChecked)); }
        }

        public string Number { get; set; }
        public string Name { get; set; }
        public string DocTitle { get; set; }
        public string Revision { get; set; }
        public string RevisionDate { get; set; }
        public string RevisionDescription { get; set; }

        public ViewSheet Sheet { get; set; }
        public Document DocRef { get; set; }

        public SheetItem(ViewSheet sheet, Document docRef, string docTitle)
        {
            Sheet = sheet;
            DocRef = docRef;
            DocTitle = docTitle;
            IsChecked = false;

            if (sheet != null && sheet.IsValidObject)
            {
                Number = sheet.SheetNumber;
                Name = sheet.Name;
            }
            else
            {
                Number = "";
                Name = "";
            }

            if (sheet != null && sheet.IsValidObject && docRef != null && docRef.IsValidObject)
            {
                try
                {
                    // Use built-in parameter to avoid locale-dependent string lookup
                    var revParam = sheet.get_Parameter(BuiltInParameter.SHEET_CURRENT_REVISION);
                    Revision = revParam?.AsString() ?? "";

                    var revId = sheet.GetCurrentRevision();
                    if (revId != ElementId.InvalidElementId)
                    {
                        var revElem = docRef.GetElement(revId) as Revision;
                        if (revElem != null && revElem.IsValidObject)
                        {
                            RevisionDate        = revElem.RevisionDate  ?? "";
                            RevisionDescription = revElem.Description   ?? "";
                        }
                        else
                        {
                            RevisionDate = "";
                            RevisionDescription = "";
                        }
                    }
                    else
                    {
                        RevisionDate = "";
                        RevisionDescription = "";
                    }
                }
                catch
                {
                    Revision = "";
                    RevisionDate = "";
                    RevisionDescription = "";
                }
            }
            else
            {
                Revision = "";
                RevisionDate = "";
                RevisionDescription = "";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
