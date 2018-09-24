using System.IO;
using AcadAddinManager.Data;

namespace AcadAddinManager.UI
{
    public class AddinVM
    {
        public AddinVM(Addin addin)
        {
            Addin = addin;
            Name = Path.GetFileName(addin.AddinFile);
        }

        public Addin Addin { get; }
        public string Name { get; set; }
    }
}
