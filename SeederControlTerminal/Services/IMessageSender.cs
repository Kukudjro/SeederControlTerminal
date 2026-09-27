using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeederControlTerminal.Services
{
    public interface IMessageSender
    {
        // TOptions — тип настроек, TMessage — тип отправляемых данных
        Task<string> SendMessageAsync<TOptions, TMessage>(TOptions options, TMessage message);
    }
}
