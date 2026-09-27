using SeederControlTerminal.Models;
using System.Collections.ObjectModel;

namespace SeederControlTerminal.Services.Log
{
    public interface ILogService
    {
        ObservableCollection<LogEntry> Entries { get; }
        void Info(string message);
        void Success(string message);
        void Warning(string message);
        void Error(string message);
    }
}
