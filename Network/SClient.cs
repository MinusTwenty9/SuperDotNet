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
    public class SClient : BClient
    {
        public string client_id;

        // Server
        public SClient(TcpClient client)
        {
            this.client = client;
            this.net_stream = client.GetStream();
            client.NoDelay = true;
            //client.SendTimeout = 1000;  /////////
            
            IPEndPoint ipe = (IPEndPoint)client.Client.RemoteEndPoint;
            this.ip = ipe.Address;
            this.port = ipe.Port;

            net_stream.WriteByte(1);
        }

        public void Reconnect(TcpClient client)
        {
            //if (reconnect == false && connected(this.client)) return;
            client.NoDelay = true;
            this.client = client;
            this.net_stream = client.GetStream();
            net_stream.WriteByte(1);
            //client.SendTimeout = 1000;  /////////
            reconnected = true;
        }

        public override bool Connect()
        {
            reconnected = false;
            while (reconnected == false && !connected(client))
                Thread.Sleep(50);

            client.NoDelay = true;
            reconnected = false;
            return connected(client);
        }

    }
}
