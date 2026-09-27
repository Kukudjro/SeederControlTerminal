using SeederControlTerminal.Models;
using SeederControlTerminal.Services;
using ReactiveUI;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SeederControlTerminal.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IMessageSender _messageSender;
        private readonly TcpReceiverService _receiverService;

        private string _ipAddress = "127.0.0.1";
        private int _port = 8080;
        private string _messageText = "Hello, Seeder!";
        private bool _isSending;

        private string _receiverIp = "0.0.0.0";
        private int _receiverPort = 8080;
        private bool _isReceiverStarted;
        private string _receiverButtonText = "Включить приемник";

        // Общий лог
        private string _networkLog = "Лог запущен...\n";

        // Свойства ОТПРАВИТЕЛЯ
        public string IpAddress { get => _ipAddress; set => this.RaiseAndSetIfChanged(ref _ipAddress, value); }
        public int Port { get => _port; set => this.RaiseAndSetIfChanged(ref _port, value); }
        public string MessageText { get => _messageText; set => this.RaiseAndSetIfChanged(ref _messageText, value); }
        public bool IsSending { get => _isSending; set => this.RaiseAndSetIfChanged(ref _isSending, value); }

        // Свойства ПРИЕМНИКА
        public string ReceiverIp { get => _receiverIp; set => this.RaiseAndSetIfChanged(ref _receiverIp, value); }
        public int ReceiverPort { get => _receiverPort; set => this.RaiseAndSetIfChanged(ref _receiverPort, value); }
        public bool IsReceiverStarted { get => _isReceiverStarted; set => this.RaiseAndSetIfChanged(ref _isReceiverStarted, value); }
        public string ReceiverButtonText { get => _receiverButtonText; set => this.RaiseAndSetIfChanged(ref _receiverButtonText, value); }

        public string NetworkLog { get => _networkLog; set => this.RaiseAndSetIfChanged(ref _networkLog, value); }

        public bool IsReceiverFieldsEnabled => !IsReceiverStarted;

        public ICommand SendMessageCommand { get; }
        public ICommand ToggleReceiverCommand { get; }

        public MainWindowViewModel(IMessageSender messageSender)
        {
            _messageSender = messageSender;

            if (Avalonia.Controls.Design.IsDesignMode) return;

            _receiverService = new TcpReceiverService();
            _receiverService.OnMessageReceived += HandleIncomingMessage;
            _receiverService.OnLogNeeded += Log;

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

            this.RaisePropertyChanged(nameof(IsReceiverFieldsEnabled));
        }

        private void HandleIncomingMessage(string jsonMessage)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Log($"\n📥 [ПРИЕМНИК ПОЙМАЛ ПАКЕТ]:\n{jsonMessage}\n");
            });
        }

        private async Task SendMessageAsync()
        {
            IsSending = true;
            Log($"[ОТПРАВИТЕЛЬ] Подготовка к отправке...");

            try
            {
                var options = new TcpConnectionOptions { IpAddress = IpAddress, Port = Port };
                var packet = new SeederCommandPacket
                {
                    Payload = MessageText,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                Log($"[ОТПРАВИТЕЛЬ] Шлем JSON на {options.IpAddress}:{options.Port}");
                string response = await _messageSender.SendMessageAsync(options, packet);
                Log($"[ОТВЕТ ОТ СЕРВЕРА] -> \"{response}\"");
            }
            catch (Exception ex)
            {
                Log($"❌ [ОТПРАВИТЕЛЬ] Ошибка: {ex.Message}");
            }
            finally
            {
                IsSending = false;
            }
        }

        private void Log(string message)
        {
            NetworkLog += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        }
    }
}
