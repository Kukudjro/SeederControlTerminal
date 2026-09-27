using ReactiveUI;
using SeederControlTerminal.Models;
using SeederControlTerminal.Services;
using SeederControlTerminal.Services.Log;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SeederControlTerminal.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IMessageSender _messageSender;
        private readonly TcpReceiverService _receiverService;
        private readonly ILogService _logService; // Наш новый сервис лога

        private string _ipAddress = "127.0.0.1";
        private int _port = 8080;
        private string _messageText = "Hello, Seeder!";
        private bool _isSending;

        private string _receiverIp = "0.0.0.0";
        private int _receiverPort = 8080;
        private bool _isReceiverStarted;
        private string _receiverButtonText = "Включить приемник";

        public string IpAddress { get => _ipAddress; set => this.RaiseAndSetIfChanged(ref _ipAddress, value); }
        public int Port { get => _port; set => this.RaiseAndSetIfChanged(ref _port, value); }
        public string MessageText { get => _messageText; set => this.RaiseAndSetIfChanged(ref _messageText, value); }
        public bool IsSending { get => _isSending; set => this.RaiseAndSetIfChanged(ref _isSending, value); }

        public string ReceiverIp { get => _receiverIp; set => this.RaiseAndSetIfChanged(ref _receiverIp, value); }
        public int ReceiverPort { get => _receiverPort; set => this.RaiseAndSetIfChanged(ref _receiverPort, value); }
        public bool IsReceiverStarted { get => _isReceiverStarted; set => this.RaiseAndSetIfChanged(ref _isReceiverStarted, value); }
        public string ReceiverButtonText { get => _receiverButtonText; set => this.RaiseAndSetIfChanged(ref _receiverButtonText, value); }

        // Вычисляемые геттеры
        public string ConnectionStatus => IsReceiverStarted ? $"Приемник активен. Слушаю порт {ReceiverPort}..." : "Приемник отключен";
        public string StatusColor => IsReceiverStarted ? "#2ed573" : "#718093";
        public bool IsReceiverFieldsEnabled => !IsReceiverStarted;

        // UI теперь биндится прямо к коллекции разноцветных записей
        public ObservableCollection<LogEntry> LogEntries => _logService.Entries;

        public ICommand SendMessageCommand { get; }
        public ICommand ToggleReceiverCommand { get; }

        public MainWindowViewModel(IMessageSender messageSender, ILogService logService)
        {
            _messageSender = messageSender;
            _logService = logService; // Забираем готовый логгер из DI

            if (Avalonia.Controls.Design.IsDesignMode) return;

            _receiverService = new TcpReceiverService();
            _receiverService.OnMessageReceived += HandleIncomingMessage;

            // Передаем системные логи приемника в наш сервис напрямую
            _receiverService.OnLogNeeded += (msg) => _logService.Info(msg);

            _logService.Info("Инженерный пульт инициализирован через DI.");

            var canSend = this.WhenAnyValue(
                x => x.IsSending, x => x.MessageText,
                (isSending, text) => !isSending && !string.IsNullOrWhiteSpace(text)
            );
            SendMessageCommand = ReactiveCommand.CreateFromTask(SendMessageAsync, canSend);
            ToggleReceiverCommand = ReactiveCommand.Create(ToggleReceiver);
        }

        private void ToggleReceiver()
        {
            if (!IsReceiverStarted)
            {
                _receiverService.StartListening(ReceiverIp, ReceiverPort);
                IsReceiverStarted = true;
                ReceiverButtonText = "Остановить приемник";
            }
            else
            {
                _receiverService.Stop();
                IsReceiverStarted = false;
                ReceiverButtonText = "Включить приемник";
            }

            this.RaisePropertyChanged(nameof(ConnectionStatus));
            this.RaisePropertyChanged(nameof(StatusColor));
            this.RaisePropertyChanged(nameof(IsReceiverFieldsEnabled));
        }

        private void HandleIncomingMessage(string jsonMessage)
        {
            // Зеленый лог при успешном приеме JSON!
            _logService.Success($"📥 [ПРИЕМНИК ПОЙМАЛ JSON]: {jsonMessage}");
        }

        private async Task SendMessageAsync()
        {
            IsSending = true;
            _logService.Info($"[ОТПРАВИТЕЛЬ] Подключение по TCP...");

            try
            {
                var options = new TcpConnectionOptions { IpAddress = IpAddress, Port = Port };
                var packet = new SeederCommandPacket
                {
                    CommandType = "TERMINAL_MESSAGE",
                    Payload = MessageText,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                // Красивый информационный синий лог отправки
                _logService.Info($"[ОТПРАВИТЕЛЬ] Шлем JSON на {options.IpAddress}:{options.Port} -> {System.Text.Json.JsonSerializer.Serialize(packet)}");

                string response = await _messageSender.SendMessageAsync(options, packet);

                // Зеленый лог успешного ответа
                _logService.Success($"[ОТВЕТ ОТ СЕРВЕРА] -> \"{response}\"");
            }
            catch (Exception ex)
            {
                // Красный лог ошибки!
                _logService.Error($"❌ [ОТПРАВИТЕЛЬ] Ошибка сети: {ex.Message}");
            }
            finally
            {
                IsSending = false;
            }
        }
    }
}
