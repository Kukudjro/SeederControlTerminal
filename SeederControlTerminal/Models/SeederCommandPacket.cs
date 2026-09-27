using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeederControlTerminal.Models
{
    public class SeederCommandPacket
    {
        public string CommandType { get; set; } = "TERMINAL_MESSAGE";
        public string Payload { get; set; } = "";
        public string Timestamp { get; set; } = "";
    }
}
