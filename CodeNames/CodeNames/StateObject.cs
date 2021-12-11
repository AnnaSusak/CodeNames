using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeNames
{
    class StateObject
    {
        public StringBuilder textBuilder = new StringBuilder();
        public Socket socket = null;
        public const int BufferSize = 256;
        public byte[] buffer = new byte[BufferSize];
    }
}
