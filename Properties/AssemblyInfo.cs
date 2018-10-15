using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AcadAddinManager;
using Autodesk.AutoCAD.Runtime;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("AcadAddinManager")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("AcadAddinManager")]
[assembly: AssemblyCopyright("Copyright ©  2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: CommandClass(typeof(AddinManagerService))]
[assembly: ExtensionApplication(typeof(App))]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d64c4a0a-6f50-4bf2-9252-043256b9f8cc")]