namespace AcadAddinManager.Data
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Threading;
    using NetLib;
    using NetLib.IO;
    using NetLib.WPF;
    using ReactiveUI;
    using ReactiveUI.Legacy;

    public class Addin
    {
        public string AddinFile { get; set; }
        public string AddinTempFile { get; set; }
        public List<DllResolve> Resolvers { get; set; }
        public List<CommandMethod> Commands { get; set; }
    }
}