using Autodesk.AutoCAD.Runtime;

namespace AcadAddinManager
{
    class App : IExtensionApplication
    {
        public void Initialize()
        {
            "Загружен AddinManager. Команды:\nAddinManager - выбор плагина и запуск команды,\nAddinManagerLast - запуск последней команды.".Write();
            AddinManagerService.ClearAddins();
        }

        public void Terminate()
        {
            AddinManagerService.ClearAddins();
        }
    }
}