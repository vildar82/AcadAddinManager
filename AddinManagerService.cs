using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using NetLib.IO;
using NetLib.WPF.Controls.Select;
using NLog;
using Exception = System.Exception;
using Path = System.IO.Path;

namespace AcadAddinManager
{
    public static class AddinManagerService
    {
        private static string addinFile;
        private static string addinTempFile;
        private static List<DllResolve> resolvers = new List<DllResolve>();
        private static MethodInfo method;
        private static ILogger Log => LogManager.GetCurrentClassLogger();

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

        [CommandMethod("AddinManager", CommandFlags.Modal)]
        public static void AddinManager()
        {
            try
            {
                addinFile = SelectAddin();
                if (string.IsNullOrEmpty(addinFile)) return;
                addinTempFile = GetTempAddin();
                method = SelectMethod(addinTempFile, null);
                Invoke(method);
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

        [CommandMethod("AddinManagerLast", CommandFlags.Modal)]
        public static void AddinManagerLast()
        {
            try
            {
                if (string.IsNullOrEmpty(addinFile))
                {
                    AddinManager();
                    return;
                }
                if (NetLib.IO.Path.IsNewestFile(addinFile, addinTempFile))
                {
                    addinTempFile = GetTempAddin();
                    method = SelectMethod(addinTempFile, method?.Name);
                    $"Сборка обновлена - {addinFile} от {File.GetLastWriteTime(addinFile):dd.MM.yy HH:mm:ss}.".Write();
                }
                Invoke(method);
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

        private static void Invoke(MethodInfo method)
        {
            try
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
            finally 
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }

        private static MethodInfo SelectMethod(string addinFile, string methodName)
        {
            resolvers = DllResolve.GetDllResolve(Path.GetDirectoryName(addinFile), SearchOption.AllDirectories);
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            var addinAsm = Assembly.LoadFile(addinFile);
            var methods = GetCommandMethods(addinAsm);
            if (methodName != null)
            {
                var method = methods.FirstOrDefault(m => m.Name == methodName);
                if (method != null)
                    return method;
            }
            var items = methods.Select(s => new SelectListItem<MethodInfo>(s.Name, s)).ToList();
            return SelectList.Select(items, "AddinManager", "Запуск команды:");
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return resolvers.FirstOrDefault(r => r.IsResolve(args.Name))?.LoadAssembly();
        }

        private static List<MethodInfo> GetCommandMethods(Assembly asm)
        {
            return (from type in asm.GetTypes()
                from methodInfo in type.GetMethods().ToList()
                let commandAtr = methodInfo.GetCustomAttributes(typeof(CommandMethodAttribute), false).FirstOrDefault()
                where commandAtr != null
                select methodInfo).ToList();
        }

        private static string GetTempAddin()
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

        private static string SelectAddin()
        {
            var dlg = new OpenFileDialog
            {
                Title = "AddinManager - выбор сборки плагина autocad"
            };
            return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : null;
        }
    }
}