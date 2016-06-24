using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace SuperDotNet.Network
{
    public abstract class BClient : Network
    {
        public static bool await_delay = true;
        public TcpClient client;
        public NetworkStream net_stream;
        public IPAddress ip;
        public int port;

        public abstract bool Connect();


        public bool Send(byte[] data)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            bool send = false;
            using (MemoryStream ms = new MemoryStream(data))
            {
                while (send == false)
                {
                    ms.Position = 0;
                    send = Send(ms, client, net_stream);
                    if (send == false)
                        reconnect();
                }
                ms.Close();
            }

            //sw.Stop();
            //Console.WriteLine("Send: " + sw.ElapsedMilliseconds);

            return send;
        }

        public bool Send_File(string path)
        {
            bool send = false;
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read,FileShare.Read))
            {
                while (send == false)
                {
                    stream.Position = 0;
                    send = Send(stream, client, net_stream);
                    if (send == false)
                        reconnect();
                }
            }
            return send;
        }

        public byte[] Receive()
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            byte[] back = null;
            MemoryStream ms = new MemoryStream();
            Stream stream = ms;
            {
                bool rec = false;

                while (rec == false)
                {
                    stream.SetLength(0);
                    rec = Receive(ref stream, client, net_stream);
                    if (rec == false)
                        reconnect();
                }
                back = ms.ToArray();
                ms.Close();
                stream.Close();
                ms.Dispose();
                stream.Dispose();
            }

            //sw.Stop();
            //Console.WriteLine("Send: " + sw.ElapsedMilliseconds);
            return back;
        }

        public bool Receive_File(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            Stream stream = fs;
            {
                bool rec = false;

                while (rec == false)
                {
                    fs.SetLength(0);
                    rec = Receive(ref stream, client, net_stream);
                    if (rec == false)
                        reconnect();
                }
                fs.Close();
                fs.Dispose();
                return true;
            }
        }


        //public void Await_Data()
        //{
        //    //try
        //    //{
        //        byte[] buff = new byte[0];
        //        AsyncCallback ns_read = null;

        //        ns_read = new AsyncCallback((IAsyncResult res) =>
        //        {
        //            try
        //            {
        //                NetworkStream ns = (NetworkStream)res.AsyncState;
        //                int length = ns.EndRead(res);
        //            }
        //            catch { }
        //            return;
        //            //if (length >= 4) return;

        //            //net_stream.BeginRead(buff, 0, buff.Length, ns_read, net_stream);
        //        });

        //        WaitHandle wh = net_stream.BeginRead(buff, 0, 0, ns_read, net_stream).AsyncWaitHandle;
        //        wh.WaitOne();
        //        //return true;
        //    //}
        //    //catch { return false; }
        //}

        public void Await_Data()
        {
            while (client.Available == 0)
            {
                if (!connected(client))
                    reconnect();
                if (await_delay)
                    Thread.SpinWait(100);
            }
        }

        public void Await_UDP()
        {
            UdpClient client = new UdpClient();

            while (client.Available == 0)
                Thread.Sleep(50);

            IPEndPoint ipe = new IPEndPoint(ip,port+1);
            byte[] data = client.Receive(ref ipe);
        }

        private void reconnect()
        {
            if (connected(client) && reconnected == true)
            {
                reconnected = false;
                return;
            }
            //if (connected(client)) return;

            if (client != null)
                client.Close();
            if (net_stream != null)
            {
                net_stream.Close();
                net_stream.Dispose();
            }

            Console.WriteLine("Reconnecting...");
            while (!Connect()) Console.WriteLine("Recon failed.");
            Console.WriteLine("Reconnected.");
        }
    }
}
