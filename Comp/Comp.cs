using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperDotNet;
using SuperDotNet.Network;
using System.Reflection;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace SuperDotNet.CComp
{
    public partial class Comp
    {
        public Client client;
        public CSimulation simulation;

        public string ic_ip;
        public int port = 1234;
        public bool connected = false;
        private bool connecting = false;


        public Comp(string ic_ip)
        {
            this.ic_ip = ic_ip;

            connect_wait();
        }

        private void connect_wait()
        {
            if (connecting) return;

            connecting = true;
            Task ct = Task.Factory.StartNew(() =>
            {
                // [TODO] unaccaptable client.connect() repetition call
                while (true)
                {
                    client = new Client(ic_ip, port);
                    if (client.Connect()) break;

                    Thread.Sleep(500);
                }
                await_command();

                connecting = false;
            });
        }

        private void await_command()
        {
            while (true)
            {
                // Waiting loop for command receive
                // [TODO]
                NetworkProtocol proc = NetworkHeader.Await_Header(client);
                interpret_protocol(proc);
            }
        }

        public void Reset()
        {
            simulation.Dispose();

            connect_wait();
        }


    }
}
