using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace SuperDotNet.Network
{
    public class Client : BClient
    {
        // Server
        public Client(TcpClient client)
        {
            this.client = client;
            this.net_stream = client.GetStream();

            client.NoDelay = true;
        }

        // Client
        public Client(string ip, int port)
        {
            this.ip = IPAddress.Parse(ip);
            this.port = port;
        }

        public override bool Connect()
        {
            try {
                client = new TcpClient();
                //client.SendTimeout = 1000;      ////////
                client.Connect(new IPEndPoint(ip,port));
                net_stream = client.GetStream();

                while (connected(client) && client.Available == 0)
                    Thread.Sleep(10);

                client.NoDelay = true;

                net_stream.ReadByte();
                reconnected = false;

                return true;
            }
            catch { return false; }
        }

    }
}
