using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeederControlTerminal.Services
{
    public class TcpReceiverService
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        public event Action<string>? OnMessageReceived;
        public event Action<string>? OnLogNeeded;

        public void StartListening(string ipAddress, int port)
        {
            try
            {
                _cts = new CancellationTokenSource();

                IPAddress localIp = string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "0.0.0.0"
                    ? IPAddress.Any
                    : IPAddress.Parse(ipAddress);

                _listener = new TcpListener(localIp, port);
                _listener.Start();

                OnLogNeeded?.Invoke($"[ПРИЕМНИК] Запущен на {localIp}:{port}. Ожидаю пакеты...");

                Task.Run(() => ListenAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                OnLogNeeded?.Invoke($"❌ [ПРИЕМНИК] Ошибка запуска: {ex.Message}");
            }
        }

        private async Task ListenAsync(CancellationToken token)
        {
            if (_listener == null) return;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    using TcpClient client = await _listener.AcceptTcpClientAsync(token);
                    using NetworkStream stream = client.GetStream();

                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);

                    if (bytesRead > 0)
                    {
                        // Чистая конвертация байт в строку UTF-8 без лишних перекодирований
                        string jsonReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        // Передаем JSON во ViewModel
                        OnMessageReceived?.Invoke(jsonReceived);

                        // Отвечаем отправителю
                        byte[] responseBytes = Encoding.UTF8.GetBytes("Пакет JSON успешно доставлен на удаленный приемник. ОК.");
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length, token);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            OnLogNeeded?.Invoke("[ПРИЕМНИК] Остановлен.");
        }
    }
}
