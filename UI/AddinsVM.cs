using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
        private string errors;

        public AddinsVM()
        {
            Init();
        }

        public ReactiveList<AddinVM> Addins { get; set; }
        public AddinVM Addin { get; set; }
        public CommandMethod Command { get; set; }
        public ReactiveCommand Start { get; set; }
        public ReactiveCommand RemoveAddin { get; set; }
        public ReactiveCommand AddAddin { get; set; }

        private async void Init()
        {
            var addins = await LoadAddins();
            Addins = new ReactiveList<AddinVM>(addins);
            var canStart = this.WhenAnyValue(v => v.Command).Select(s => s != null);
            Start = CreateCommand(() =>
            {
                HideMe();
                var addin = Addin;
                var com = Command;
                fileData.Data.AddinFiles.Remove(addin.Addin.AddinFile);
                fileData.Data.AddinFiles.Insert(0, addin.Addin.AddinFile);
                Addins.Remove(addin);
                Addins.Insert(0, addin);
                Addin = addin;
                Command = com;
                AddinManagerService.Invoke(com);
            }, canStart);
            RemoveAddin = CreateCommand<AddinVM>(a =>
            {
                Addins.Remove(a);
                fileData.Data.AddinFiles.Remove(a.Addin.AddinFile);
            });
            AddAddin = CreateCommand(AddAddinExec);
            if (!errors.IsNullOrEmpty())
                ShowMessage(errors, "Ошибка загрузки файлов сборок");
        }

        private Task<List<AddinVM>> LoadAddins()
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
                            return new AddinVM(AddinManagerService.GetAddin(s));
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
            fileData.Data.AddinFiles = Addins.Select(s => s.Addin.AddinFile).ToList();
            fileData.TrySave();
        }

        private void AddAddinExec()
        {
            var file = SelectAddin();
            var addinExist = Addins.FirstOrDefault(a => a.Addin.AddinFile.EqualsIgnoreCase(file));
            if (addinExist != null)
            {
                ShowMessage("Такая сборка уже есть");
                Addin = addinExist;
                return;
            }

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
