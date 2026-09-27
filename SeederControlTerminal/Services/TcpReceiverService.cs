using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeederControlTerminal.Services
{
    public sealed class TcpReceiverService
    {
        private const int BufferSize = 4096;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private Task? _listeningTask;

        public event Action<string>? OnMessageReceived;
        public event Action<string>? OnLogNeeded;

        public void StartListening(string ipAddress, int port)
        {
            if (_listener != null) return; // Защита от повторного запуска

            try
            {
                _cts = new CancellationTokenSource();

                IPAddress localIp = string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "0.0.0.0"
                    ? IPAddress.Any
                    : IPAddress.Parse(ipAddress);

                _listener = new TcpListener(localIp, port);
                _listener.Start();

                OnLogNeeded?.Invoke($"[ПРИЕМНИК] Запущен на {localIp}:{port}. Ожидаю пакеты...");

                // Сохраняем задачу, чтобы корректно дождаться её завершения при остановке
                _listeningTask = ListenAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                OnLogNeeded?.Invoke($"❌ [ПРИЕМНИК] Ошибка запуска: {ex.Message}");
            }
        }

        private async Task ListenAsync(CancellationToken token)
        {
            if (_listener == null) return;

            try
            {
                while (!token.IsCancellationRequested)
                {

                    TcpClient client = await _listener.AcceptTcpClientAsync(token).ConfigureAwait(false);

                    _ = HandleClientAsync(client, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                OnLogNeeded?.Invoke($"❌ [ПРИЕМНИК] Ошибка цикла прослушивания: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            {
                try
                {
                    using NetworkStream stream = client.GetStream();

                    byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                    try
                    {
                        int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), token).ConfigureAwait(false);

                        if (bytesRead > 0)
                        {
                            string jsonReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                            OnMessageReceived?.Invoke(jsonReceived);

                            byte[] responseBytes = Encoding.UTF8.GetBytes("Пакет JSON успешно доставлен на удаленный приемник. ОК.");
                            await stream.WriteAsync(responseBytes.AsMemory(), token).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                _listener?.Stop();

                _listeningTask?.GetAwaiter().GetResult();
            }
            catch (Exception)
            {
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                _listener = null;
                _listeningTask = null;
                OnLogNeeded?.Invoke("[ПРИЕМНИК] Остановлен.");
            }
        }
    }
}
