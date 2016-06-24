using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Reflection;
using System.Net;

namespace SuperDotNet.Network
{
    public class Server : Network
    {
        TcpListener tcp_listener;
        public List<SClient> clients;
        public event server_new_client ServerNewClient;

        public int port;

        public Server(int port)
        {
            this.port = port;
            clients = new List<SClient>();

            listener_start();
        }

        #region Client Connect

        private void listener_start()
        {
            tcp_listener = new TcpListener(port);
            tcp_listener.Start();
            listener_start_accept();
        }

        private void listener_start_accept()
        {
            tcp_listener.BeginAcceptTcpClient(accept_tcp_client, tcp_listener);
        }

        private void accept_tcp_client(IAsyncResult res)
        {
            TcpClient tcp_client = tcp_listener.EndAcceptTcpClient(res);
            listener_start_accept();

            new_client(tcp_client);
        }

        private void new_client(TcpClient tcp_client)
        {
            // Check for reconnection requests
            SClient[] cs = clients.Where(s => s.ip.Equals(((IPEndPoint)tcp_client.Client.RemoteEndPoint).Address)).ToArray();
            if (cs.Length > 0)
                cs[0].Reconnect(tcp_client);
            else
            {
                SClient c = new SClient(tcp_client);
                clients.Add(c);
                if (ServerNewClient != null)
                    ServerNewClient.Invoke(c);
            }
        }

        #endregion

        public void UDP_Ready()
        {
            Write_UDP(new byte[]{1},IPAddress.Any, port+1);
        }

    }
    public delegate void server_new_client(SClient client);
}
