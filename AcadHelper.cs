namespace AcadAddinManager
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Autodesk.AutoCAD.ApplicationServices;
    using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    public static class AcadHelper
    {
        public static void Write(this string msg)
        {
            Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\n{msg}");
        }
    }
}
