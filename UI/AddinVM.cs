namespace AcadAddinManager.UI
{
    using System;
    using System.IO;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Threading;
    using Data;
    using NetLib;
    using NetLib.WPF;
    using ReactiveUI;
    using ReactiveUI.Legacy;

    public class AddinVM : BaseModel
    {
        public AddinVM(Addin addin)
        {
            Addin = addin;
            Name = Path.GetFileName(addin.AddinFile);
            
            this.WhenAnyValue(v => v.Filter).Skip(1).Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(Dispatcher.CurrentDispatcher)
                .Subscribe(s =>
                {
                    Commands?.Reset();
                });
            
            AllCommands = new ReactiveList<CommandMethod>(addin.Commands);
            Commands = AllCommands.CreateDerivedCollection(s => s, filter);
        }

        public Addin Addin { get; set; }
        public string Name { get; set; }
        
        public string Filter { get; set; }
        public ReactiveList<CommandMethod> AllCommands { get; set; }
        public IReactiveDerivedList<CommandMethod> Commands { get; set; }
        
        private bool filter(CommandMethod com)
        {
            return Filter.IsNullOrEmpty() || Regex.IsMatch(com.Command.GlobalName, Filter, RegexOptions.IgnoreCase);
        }

        public void Update()
        {
            Addin = AddinManagerService.GetAddin(Addin.AddinFile);
            using (AllCommands.SuppressChangeNotifications())
            {
                AllCommands.Clear();
                AllCommands.AddRange(Addin.Commands);
            }
        }
    }
}
