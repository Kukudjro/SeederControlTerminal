using SeederControlTerminal.Models;
using System.Collections.ObjectModel;

namespace SeederControlTerminal.Services.Log
{
    public class LogService : ILogService
    {
        // Специальная коллекция, при изменении которой UI сам добавляет строчки на экран
        public ObservableCollection<LogEntry> Entries { get; } = new();

        public void Info(string message) => Log(message, "#1e90ff");    // Синий (События/Отправка)
        public void Success(string message) => Log(message, "#2ed573"); // Зеленый (Успешный прием)
        public void Warning(string message) => Log(message, "#ffa500"); // Оранжевый (Предупреждения)
        public void Error(string message) => Log(message, "#ff4757");   // Красный (Ошибки сети)

        private void Log(string message, string color)
        {
            // Потокобезопасно добавляем запись в коллекцию силами главного потока UI
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Entries.Add(new LogEntry { Message = message, Color = color });
            });
        }
    }
}
