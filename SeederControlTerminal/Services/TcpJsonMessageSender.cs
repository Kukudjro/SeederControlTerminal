using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SeederControlTerminal.Services
{
    public class TcpJsonMessageSender : IMessageSender
    {
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

        public async Task<string> SendMessageAsync<TOptions, TMessage>(TOptions options, TMessage message)
        {
            if (options is not TcpConnectionOptions tcpOptions)
            {
                throw new ArgumentException($"Этот отправитель ожидает настройки типа {nameof(TcpConnectionOptions)}");
            }

            using var cts = new CancellationTokenSource(_timeout);
            using var client = new TcpClient();

            try
            {
                await client.ConnectAsync(tcpOptions.IpAddress, tcpOptions.Port, cts.Token).ConfigureAwait(false);
                using var stream = client.GetStream();

                string json = JsonSerializer.Serialize(message);
                byte[] sendData = Encoding.UTF8.GetBytes(json);

                await stream.WriteAsync(sendData.AsMemory(), cts.Token).ConfigureAwait(false);

                byte[] responseBuffer = new byte[1024];

                int bytesRead = await stream.ReadAsync(responseBuffer.AsMemory(), cts.Token).ConfigureAwait(false);

                if (bytesRead > 0)
                {
                    return Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
                }
                else
                {
                    return "Устройство не ответило";
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                return "Таймаут соединения или чтения.";
            }
            catch (Exception ex)
            {
                return $"Ошибка при отправке сообщения: {ex.Message}";
            }
        }

    }
}
