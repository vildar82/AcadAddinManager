namespace AcadAddinManager
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using Data;
    using UI;
    using Autodesk.AutoCAD.Runtime;
    using JetBrains.Annotations;
    using NetLib.IO;
    using NLog;
    using Application = Autodesk.AutoCAD.ApplicationServices.Application;
    using Exception = System.Exception;
    using Path = System.IO.Path;

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
                
                if (NetLib.IO.Path.IsNewestFile(lastMethod.Addin.AddinFile, lastMethod.Addin.AddinTempFile))
                {
                    lastMethod.Addin = GetAddin(lastMethod.Addin.AddinFile);
                    var method = lastMethod.Addin.Commands.FirstOrDefault(m => m.Method.Name == lastMethod.Method.Name && 
                                                             m.Command.GlobalName == lastMethod.Command.GlobalName);
                    if (method == null)
                    {
                        MessageBox.Show($"Не найдена команда {lastMethod.Command.GlobalName} ({lastMethod.Method.Name}).",
                            "AddinManager", MessageBoxButton.OK, MessageBoxImage.Error);
                        AddinManager();
                        return;
                    }

                    lastMethod = method;
                    $"Сборка обновлена - {lastMethod.Addin.AddinFile} от {File.GetLastWriteTime(lastMethod.Addin.AddinFile):dd.MM.yy HH:mm:ss}.".Write();
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
            lastMethod = commandMethod;
            Resolvers = commandMethod.Addin.Resolvers;
            Application.Idle += InvokeOnIdle;
        }

        private static void InvokeOnIdle(object sender, EventArgs e)
        {
            Application.Idle -= InvokeOnIdle;
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                LoadResolvers(Resolvers);
                var method = lastMethod.Method;
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
            addin.Resolvers = DllResolve.GetDllResolve(Path.GetDirectoryName(addin.AddinTempFile),
                SearchOption.AllDirectories, ".dll", ".exe");
            Resolvers = addin.Resolvers;
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            var addinAsm = Assembly.LoadFile(addin.AddinTempFile);
            addin.Commands = GetCommandMethods(addinAsm, addin);
            return addin;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                return Resolvers.FirstOrDefault(r => r.IsResolve(args.Name))?.LoadAssembly();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                ex.Message.Write();
                return null;
            }
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
        
        private static void LoadResolvers(List<DllResolve> resolvers)
        {
            foreach (var dllResolve in resolvers)
            {
                try
                {
                    Assembly.LoadFrom(dllResolve.DllFile);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    ex.Message.Write();
                }
            }
        }
    }
}