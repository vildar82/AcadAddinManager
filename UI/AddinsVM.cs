namespace AcadAddinManager.UI
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Data;
    using DynamicData;
    using Microsoft.Win32;
    using NetLib;
    using NetLib.WPF;
    using ReactiveUI;

    public class AddinsVM : BaseViewModel
    {
        private LocalFileData<AddinsData> fileData;
        private string errors;

        public AddinsVM()
        {
            Init();
        }

        public string Search { get; set; }

        public SourceList<AddinVM> AllAddins { get; set; }

        public ReadOnlyObservableCollection<AddinVM> Addins { get; set; }

        public AddinVM Addin { get; set; }

        public CommandMethod Command { get; set; }

        public ReactiveCommand<Unit, Unit> Start { get; set; }

        public ReactiveCommand<AddinVM, Unit> RemoveAddin { get; set; }

        public ReactiveCommand<Unit, Unit> AddAddin { get; set; }

        public ReactiveCommand<Unit, Unit> UpdateCommands { get; set; }

        private async void Init()
        {
            var addins = await LoadAddins();
            AllAddins = new SourceList<AddinVM>();
            AllAddins.AddRange(addins.Select(s => new AddinVM(s)));

            var searchObs = this.WhenAnyValue(v => v.Search).Skip(1).Throttle(TimeSpan.FromMilliseconds(300)).Select(s => true);
            AllAddins.Connect()
                .Filter(filter)
                .AutoRefreshOnObservable(c => searchObs)
                .Bind(out var data)
                .Subscribe();
            Addins = data;

            if (!fileData.Data.LastAddin.IsNullOrEmpty())
            {
                Addin = AllAddins.Items.FirstOrDefault(a => a.Addin.AddinFile.EqualsIgnoreCase(fileData.Data.LastAddin));
                if (Addin != null && !fileData.Data.LastCommand.IsNullOrEmpty())
                    Command = Addin.Addin.Commands.FirstOrDefault(
                        c => c.Command.GlobalName == fileData.Data.LastCommand);
            }

            var canStart = this.WhenAnyValue(v => v.Command).Select(s => s != null);
            Start = CreateCommand(() =>
            {
                HideMe();
                var addin = Addin;
                var com = Command;
                fileData.Data.AddinFiles.Remove(addin.Addin.AddinFile);
                fileData.Data.AddinFiles.Insert(0, addin.Addin.AddinFile);
                fileData.Data.LastAddin = addin.Addin.AddinFile;
                fileData.Data.LastCommand = Command.Method.Name;
                OnClosing();
                AllAddins.Remove(addin);
                AllAddins.Insert(0, addin);
                Addin = addin;
                Command = com;
                AddinManagerService.Invoke(com);
            }, canStart);
            RemoveAddin = CreateCommand<AddinVM>(a =>
            {
                AllAddins.Remove(a);
                fileData.Data.AddinFiles.Remove(a.Addin.AddinFile);
            });
            AddAddin = CreateCommand(AddAddinExec);
            if (!errors.IsNullOrEmpty())
                ShowMessage(errors, "Ошибка загрузки файлов сборок");

            UpdateCommands = CreateCommand(UpdateCommandsExec);
        }

        private bool filter(AddinVM ad)
        {
            return Search.IsNullOrEmpty() || Regex.IsMatch(ad.Addin.AddinFile, Search, RegexOptions.IgnoreCase);
        }

        private Task<List<Addin>> LoadAddins()
        {
            return Task.Run(() =>
            {
                var file = NetLib.IO.Path.GetUserPluginFile("AcadAddinManager", "AcadAddinManagerData.json");
                fileData = new LocalFileData<AddinsData>(file, false);
                fileData.TryLoad(() => new AddinsData());
                errors = string.Empty;
                return fileData.Data.AddinFiles.Select(s =>
                    {
                        try
                        {
                            return AddinManagerService.GetAddin(s);
                        }
                        catch (Exception ex)
                        {
                            errors += $"{s} - {ex.Message}.\n\n";
                            return null;
                        }
                    })
                    .Where(w => w != null).ToList();
            });
        }

        /// <inheritdoc />
        public override void OnClosing()
        {
            fileData.Data.AddinFiles = AllAddins.Items.Select(s => s.Addin.AddinFile).ToList();
            fileData.TrySave();
        }

        private void AddAddinExec()
        {
            var file = SelectAddin();
            var addinExist = AllAddins.Items.FirstOrDefault(a => a.Addin.AddinFile.EqualsIgnoreCase(file));
            if (addinExist != null)
            {
                ShowMessage("Такая сборка уже есть");
                Addin = addinExist;
                return;
            }

            var addin = AddinManagerService.GetAddin(file);
            AllAddins.Insert(0, new AddinVM(addin));
            fileData.Data.AddinFiles.Insert(0, file);
        }

        private static string SelectAddin()
        {
            var dlg = new OpenFileDialog
            {
                Title = "AddinManager - выбор сборки плагина autocad",
                Filter = "Net assembly files (*.dll) | *.dll;"
            };
            return dlg.ShowDialog() == true ? dlg.FileName : throw new OperationCanceledException();
        }

        private void UpdateCommandsExec()
        {
            Addin?.Update();
        }
    }
}
