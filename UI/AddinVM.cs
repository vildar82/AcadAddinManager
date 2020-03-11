namespace AcadAddinManager.UI
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;
    using Data;
    using DynamicData;
    using NetLib;
    using NetLib.WPF;
    using ReactiveUI;

    public class AddinVM : BaseModel
    {
        public AddinVM(Addin addin)
        {
            Addin = addin;
            Name = Path.GetFileName(addin.AddinFile);

            AllCommands = new SourceList<CommandMethod>();
            AllCommands.AddRange(addin.Commands);

            var searchObs = this.WhenAnyValue(v => v.Filter).Skip(1)
                .Throttle(TimeSpan.FromMilliseconds(300)).Select(s => true);
            AllCommands.Connect()
                .Filter(filter)
                .AutoRefreshOnObservable(c => searchObs)
                .Bind(out var data)
                .Subscribe();
            Commands = data;
        }

        public Addin Addin { get; set; }
        public string Name { get; set; }

        public string Filter { get; set; }
        public SourceList<CommandMethod> AllCommands { get; set; }
        public ReadOnlyObservableCollection<CommandMethod> Commands { get; set; }

        private bool filter(CommandMethod com)
        {
            return Filter.IsNullOrEmpty() || Regex.IsMatch(com.Command.GlobalName, Filter, RegexOptions.IgnoreCase);
        }

        public void Update()
        {
            Addin = AddinManagerService.GetAddin(Addin.AddinFile);
            AllCommands.Clear();
            AllCommands.AddRange(Addin.Commands);
        }
    }
}
