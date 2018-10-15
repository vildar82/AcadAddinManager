namespace AcadAddinManager.UI
{
    using System.IO;
    using Data;

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
