using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using NetLib;
using NetLib.WPF.Controls.Select;

namespace AcadAddinManager
{
    public class AddinManagerService
    {
        private string addinFile;
        private string typeFullName;
        private string methodName;

        [CommandMethod("AddinManager", CommandFlags.Modal)]
        public void AddinManager()
        {
            addinFile = SelectAddin();
            if (string.IsNullOrEmpty(addinFile)) return;
            var addinTempFile = GetTempAddin(addinFile);
            var method = SelectMethod(addinTempFile);
            typeFullName = method.DeclaringType.FullName;
            methodName = method.Name;
            Invoke(method);
        }

        [CommandMethod("AddinManagerLast", CommandFlags.Modal)]
        public void AddinManagerLast()
        {
            if (string.IsNullOrEmpty(addinFile)) throw new Exception("Не выбран файл dll.");
            var addinTempFile = GetTempAddin(addinFile);
            var method= GetMethod(addinTempFile);
            Invoke(method);
        }

        private MethodInfo GetMethod(string addinFile)
        {
            var addinAsm = Assembly.LoadFile(addinFile);
            var type = addinAsm.GetTypes().FirstOrDefault(t => t.FullName == typeFullName);
            return type.GetMethod(methodName);
        }

        private static void Invoke(MethodInfo method)
        {
            var instance = Activator.CreateInstance(method.DeclaringType);
            method.Invoke(instance, null);
        }

        private static MethodInfo SelectMethod(string addinFile)
        {
            var addinAsm = Assembly.LoadFile(addinFile);
            var methods = GetCommandMethods(addinAsm);
            return SelectList.Select(methods.Select(s => new SelectListItem<MethodInfo>(s.Name, s)).ToList(),
                "Команда", "Name");
        }

        private static List<MethodInfo> GetCommandMethods(Assembly asm)
        {
            var commandMethods = new List<MethodInfo>();
            foreach (var type in asm.GetTypes())
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance).ToList();
                foreach (var methodInfo in methods)
                {
                    var commandAtr = methodInfo.GetCustomAttributes(typeof(CommandMethodAttribute), false).FirstOrDefault();
                    if (commandAtr != null)
                    {
                        commandMethods.Add(methodInfo);
                    }
                }
            }
            return commandMethods;
        }

        private static string GetTempAddin(string addinFile)
        {
            var dir = Path.GetDirectoryName(addinFile);
            var guid = Guid.NewGuid().ToString();
            var tempDir =Path.Combine(Path.GetTempPath(), "AcadAddinManager", guid);
            Directory.CreateDirectory(tempDir);
            NetLib.IO.Path.CopyDirectory(dir, tempDir);
            var addinDll = Path.Combine(tempDir, Path.GetFileName(addinFile));
            var addinDllNew = Path.Combine(tempDir, $"{Path.GetFileNameWithoutExtension(addinDll)}{guid}.dll");
            File.Copy(addinDll, addinDllNew);
            File.Delete(addinDll);
            return addinDllNew;
        }

        private static string SelectAddin()
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                return dlg.FileName;
            }
            return null;
        }
    }
}
