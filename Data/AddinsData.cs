namespace AcadAddinManager.Data
{
    using System.Collections.Generic;

    public class AddinsData
    {
        public List<string> AddinFiles { get; set; } = new List<string>();

        public string LastAddin { get; set; }

        public string LastCommand { get; set; }
    }
}
