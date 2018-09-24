using System.Reflection;
using Autodesk.AutoCAD.Runtime;

namespace AcadAddinManager.Data
{
    public class CommandMethod
    {
        public CommandMethod()
        {
            
        }

        public CommandMethodAttribute Command { get; set; }
        public MethodInfo Method { get; set; }
        public Addin Addin { get; set; }
    }
}
