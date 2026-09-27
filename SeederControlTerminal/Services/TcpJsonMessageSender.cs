using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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

            using var client = new TcpClient();

            await client.ConnectAsync(tcpOptions.IpAddress, tcpOptions.Port).WaitAsync(_timeout);
            using var stream = client.GetStream();

            string json = JsonSerializer.Serialize(message);
            byte[] sendData = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(sendData, 0, sendData.Length);

            byte[] responseBuffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length).WaitAsync(_timeout);

            return bytesRead > 0
                ? Encoding.UTF8.GetString(responseBuffer, 0, bytesRead)
                : "Устройство не ответило";
        }
    }
}
