using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using AcadAddinManager.Data;
using AcadAddinManager.UI;
using Autodesk.AutoCAD.Runtime;
using JetBrains.Annotations;
using NetLib.IO;
using NLog;
using Exception = System.Exception;
using Path = System.IO.Path;

namespace AcadAddinManager
{
    public static class AddinManagerService
    {
        private static CommandMethod lastMethod;
        private static AddinsView addinsView;
        private static ILogger Log => LogManager.GetCurrentClassLogger();

        public static List<DllResolve> Resolvers { get; set; }

        public static void ClearAddins()
        {
            var addinDir = GetTempDir();
            try
            {
                Directory.Delete(addinDir, true);
                $"Очищена папка загрузки плагинов '{addinDir}'.".Write();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                $"Ошибка очистки папки загрузки плагинов '{addinDir}' - {ex.Message}.".Write();
            }
        }

        [CommandMethod("AddinManager", CommandFlags.Session)]
        public static void AddinManager()
        {
            try
            {
                if (addinsView == null)
                {
                    var addinsVM = new AddinsVM();
                    addinsView = new AddinsView(addinsVM);
                }
                Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowModelessWindow(addinsView);
            }
            catch (OperationCanceledException)
            {
                // Отменено пользователем
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }

        [CommandMethod("AddinManagerLast", CommandFlags.Session)]
        public static void AddinManagerLast()
        {
            try
            {
                if (lastMethod == null)
                {
                    AddinManager();
                    return;
                }
                var addin = lastMethod.Addin;
                if (NetLib.IO.Path.IsNewestFile(addin.AddinFile, addin.AddinTempFile))
                {
                    var addinUpdate = GetAddin(addin.AddinTempFile);
                    var method = addinUpdate.Commands.FirstOrDefault(m => m.Method.Name == lastMethod.Method.Name && 
                                                             m.Command.GlobalName == lastMethod.Command.GlobalName);
                    if (method == null)
                    {
                        MessageBox.Show($"Не найдена команда {lastMethod.Command.GlobalName} ({lastMethod.Method.Name}).",
                            "AddinManager", MessageBoxButton.OK, MessageBoxImage.Error);
                        AddinManager();
                        return;
                    }
                    lastMethod.Method = method.Method;
                    lastMethod.Command = method.Command;
                    $"Сборка обновлена - {addin.AddinFile} от {File.GetLastWriteTime(addin.AddinFile):dd.MM.yy HH:mm:ss}.".Write();
                }
                Invoke(lastMethod);
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }
        }

        public static void Invoke(CommandMethod commandMethod)
        {
            try
            {
                lastMethod = commandMethod;
                Resolvers = commandMethod.Addin.Resolvers;
                var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                var method = commandMethod.Method;
                using (doc.LockDocument())
                {
                    if (method.IsStatic)
                    {
                        method.Invoke(null, null);
                    }
                    else
                    {
                        var instance = Activator.CreateInstance(method.DeclaringType);
                        method.Invoke(instance, null);
                    }
                }
            }
            finally 
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }

        [NotNull]
        public static Addin GetAddin(string addinFile)
        {
            var addin = new Addin {AddinFile = addinFile};
            addin.AddinTempFile = GetTempAddin(addin.AddinFile);
            Resolvers = DllResolve.GetDllResolve(Path.GetDirectoryName(addin.AddinTempFile), SearchOption.AllDirectories);
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            var addinAsm = Assembly.LoadFile(addin.AddinTempFile);
            addin.Commands = GetCommandMethods(addinAsm, addin);
            return addin;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Resolvers.FirstOrDefault(r => r.IsResolve(args.Name))?.LoadAssembly();
        }

        [NotNull]
        private static List<CommandMethod> GetCommandMethods(Assembly asm, Addin addin)
        {
            return (from type in asm.GetTypes()
                from methodInfo in type.GetMethods().ToList()
                let commandAtr = methodInfo.GetCustomAttributes(typeof(CommandMethodAttribute), false).FirstOrDefault()
                where commandAtr != null
                select new CommandMethod
                {
                    Command = (CommandMethodAttribute) commandAtr,
                    Method = methodInfo,
                    Addin = addin
                }).OrderBy(o => o.Command.GlobalName).ToList();
        }

        private static string GetTempAddin(string addinFile)
        {
            var dir = Path.GetDirectoryName(addinFile);
            var guid = Guid.NewGuid().ToString();
            var tempDir = Path.Combine(GetTempDir(), guid);
            Directory.CreateDirectory(tempDir);
            NetLib.IO.Path.CopyDirectory(dir, tempDir);
            return Path.Combine(tempDir, Path.GetFileName(addinFile));
        }

        private static string GetTempDir()
        {
            return Path.Combine(Path.GetTempPath(), "AcadAddinManager");
        }
    }
}