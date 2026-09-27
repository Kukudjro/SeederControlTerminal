using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeederControlTerminal.Services
{
    public class TcpConnectionOptions
    {
        public string IpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 8080;
    }

}
