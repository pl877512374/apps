using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace NavigateLaser.Models
{
    class StateObject
    {
        // Client  socket.
        public Socket sckReceive = null;
        // Size of receive buffer.
        public const int BufferSize = 20480;
        // Receive buffer.
        public byte[] ReceiveBuf = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }
}