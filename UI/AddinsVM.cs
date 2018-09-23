using System.Linq;
using System.Reactive.Linq;
using AcadAddinManager.Data;
using Microsoft.Win32;
using NetLib;
using NetLib.WPF;
using ReactiveUI;

namespace AcadAddinManager.UI
{
    public class AddinsVM : BaseViewModel
    {
        private LocalFileData<AddinsData> fileData;

        public AddinsVM()
        {
            var file = NetLib.IO.Path.GetUserPluginFile("AcadAddinManager", "AcadAddinManagerData.json");
            fileData = new LocalFileData<AddinsData>(file, false);
            fileData.TryLoad(() => new AddinsData());
            var addins = fileData.Data.AddinFiles.Select(s => new AddinVM(AddinManagerService.GetAddin(file))).ToList();
            Addins = new ReactiveList<AddinVM>(addins);
            var canStart = this.WhenAnyValue(v => v.Command).Select(s => s != null);
            Start = CreateCommand(() => AddinManagerService.Invoke(Command), canStart);
            RemoveAddin = CreateCommand<AddinVM>(a =>
            {
                Addins.Remove(a);
                fileData.Data.AddinFiles.Remove(a.Addin.AddinFile);
            });
            AddAddin = CreateCommand(AddAddinExec);
        }

        public ReactiveList<AddinVM> Addins { get; set; }
        public AddinVM Addin { get; set; }
        public CommandMethod Command { get; set; }
        public ReactiveCommand Start { get; set; }
        public ReactiveCommand RemoveAddin { get; set; }
        public ReactiveCommand AddAddin { get; set; }

        /// <inheritdoc />
        public override void OnClosing()
        {
            fileData.TrySave();
        }

        private void AddAddinExec()
        {
            var file = SelectAddin();
            var addin = AddinManagerService.GetAddin(file);
            Addins.Insert(0, new AddinVM(addin));
            fileData.Data.AddinFiles.Insert(0, file);
        }

        private static string SelectAddin()
        {
            var dlg = new OpenFileDialog
            {
                Title = "AddinManager - выбор сборки плагина autocad",
                Filter = "Net assembly files (*.dll) | *.dll;"
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }
    }
}
