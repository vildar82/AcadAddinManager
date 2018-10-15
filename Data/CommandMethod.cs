namespace AcadAddinManager.Data
{
    using System.Reflection;
    using Autodesk.AutoCAD.Runtime;

    public class CommandMethod
    {
        public CommandMethodAttribute Command { get; set; }
        public MethodInfo Method { get; set; }
        public Addin Addin { get; set; }
    }
}
