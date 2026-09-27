using System;
using System.Buffers;
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
            if (_listener != null) return; // защита от повторного запуска

            try
            {
                IPAddress localIp = string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "0.0.0.0"
                    ? IPAddress.Any
                    : IPAddress.Parse(ipAddress);

                _cts = new CancellationTokenSource();
                _listener = new TcpListener(localIp, port);
                _listener.Start();

                OnLogNeeded?.Invoke($"[ПРИЕМНИК] Запущен на {localIp}:{port}. Ожидаю пакеты...");

                _listeningTask = ListenAsync(_listener, _cts.Token);
            }
            catch (Exception ex)
            {
                OnLogNeeded?.Invoke($"❌ [ПРИЕМНИК] Ошибка запуска: {ex.Message}");
            }
        }

        private async Task ListenAsync(TcpListener listener, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                    _ = HandleClientAsync(client, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                OnLogNeeded?.Invoke($"❌ [ПРИЕМНИК] Ошибка цикла прослушивания: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                try
                {
                    using NetworkStream stream = client.GetStream();
                    int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), token).ConfigureAwait(false);
                    if (bytesRead <= 0) return;

                    string jsonReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    OnMessageReceived?.Invoke(jsonReceived);

                    byte[] responseBytes = Encoding.UTF8.GetBytes("Пакет JSON успешно доставлен на удаленный приемник. ОК.");
                    await stream.WriteAsync(responseBytes.AsMemory(), token).ConfigureAwait(false);
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    OnLogNeeded?.Invoke($"⚠️ [ПРИЕМНИК] Ошибка обработки клиента: {ex.Message}");
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        public void Stop()
        {
            if (_listener == null) return;

            try
            {
                _cts?.Cancel();
                _listener?.Stop();
            }
            catch (Exception ex)
            {
                OnLogNeeded?.Invoke($"❌ [ПРИЕМНИК] Ошибка остановки: {ex.Message}");
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