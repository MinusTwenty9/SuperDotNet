using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Net;

namespace SuperDotNet.Network
{
    public abstract class Network 
    {
        public static bool await_delay = true;

        public bool connected(TcpClient tcp_client)
        {
            try
            {
                if (tcp_client != null && tcp_client.Client != null && tcp_client.Client.Connected)
                {
                    // Detect if client disconnected
                    if (tcp_client.Client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (tcp_client.Client.Receive(buff, SocketFlags.Peek) == 0)
                        {
                            // Client disconnected
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

        }
        public int time_out = 10000;  ///////500
        public int retry_delay = 20;
        public int pack_size = 32 * 1024 * 1024;
        public bool reconnected = false;
        public Stopwatch sw;

        #region Raw

        // Receives raw bytes of a given length
        // Accounts for timeouts and disconnects
        private byte[] raw_receive(int length,TcpClient client, NetworkStream net_stream)
        {
            //if (connected(client) == false) return null;
            if (sw == null)
                sw = new Stopwatch();
            sw.Restart();

            try
            {
                MemoryStream stream = new MemoryStream();
                byte[] r_buffer = new byte[length];

                //int c_time = 0;
                //do
                //{
                    while (sw.ElapsedMilliseconds <= time_out && reconnected == false)
                        if (client.Available >= 1)
                        {
                            int read = net_stream.Read(r_buffer, 0, length);
                            length -= read;
                            stream.Write(r_buffer, 0, read);
                            //c_time = 0;
                            sw.Restart();

                            if (length == 0)
                            {
                                r_buffer = stream.ToArray();
                                stream.Close();
                                stream.Dispose();
                                return r_buffer;
                            }
                        }
                //} while (connected(client));
                    //else if (connected(client))
                    //{//Thread.Sleep(retry_delay);
                    //}
                    //else return null;
                // Disconnect
                return null;
            }
            catch { return null; }
        }


        // Sends raw bytes of a given send_buffer
        // Accounts for disconnects
        private bool raw_send(byte[] s_buffer, TcpClient client,NetworkStream net_stream)
        {
            //if (connected(client) == false) return false;

            try
            {
                if (reconnected == true) return false;
                net_stream.Write(s_buffer, 0, s_buffer.Length);
                return true;
            }
            catch { return false; }
        }

        #endregion

        #region Pack's
        // Computes a hash for the pack data, sends the hash (64bit)
        // then sends the pack (needs to be standatised length)
        // waits for 0/1 (1 byte) from the other client to see if hash matches
        // 1 == return; 0 == repeat from send_hash;
        public bool send_pack(byte[] s_pack, TcpClient client, NetworkStream net_stream)
        {
            byte[] hash = Hash.Get_SHA1_Hash(s_pack);
            byte[] data = new byte[s_pack.Length + hash.Length];

            Array.Copy(hash, 0, data, 0, hash.Length);
            Array.Copy(s_pack, 0, data, hash.Length, s_pack.Length);
            
            //clear_netstream();
            return raw_send(data, client, net_stream);
        }

        // If Hash doesn't match, return empty byte array
        // If Timeout or disconnect return null
        // Else return data
        public byte[] receive_pack(int length, TcpClient client, NetworkStream net_stream)
        {
            byte[] hash = null;
            byte[] r_pack = null;
            bool hash_match;

                hash = raw_receive(20,client,net_stream);
                r_pack = raw_receive(length, client, net_stream);

                if (hash == null || r_pack == null)
                {
                    Console.WriteLine("receive fail");
                    return null;    // Time out or disconnect
                }
                hash_match = Hash.Comp_Hash(hash, Hash.Get_SHA1_Hash(r_pack));

                //clear_netstream();

            return (hash_match ? r_pack : new byte[0] );
        }

        private void clear_netstream(TcpClient client, NetworkStream net_stream)
        {
            // Clean up netstream
            if (client.Available > 0)
            {
                byte[] clr = new byte[client.Available];
                net_stream.Read(clr, 0, clr.Length);
            }
        }
        #endregion

        public bool Send(Stream stream, TcpClient client, NetworkStream net_stream)
        {
            long size = stream.Length;
            byte[] send;

            long[] lost_list;

            if (!send_size(size, client, net_stream)) return false;


            //using (MemoryStream ms = new MemoryStream(s_data))
            {
                #region Initial Packages
                send = new byte[pack_size];
                while (stream.Position + pack_size <= size)
                {
                    stream.Read(send, 0, pack_size);
                    if (!send_pack(send, client, net_stream)) return false;
                }

                if ((int)(size - stream.Position) > 0)
                {
                    send = new byte[(int)(size - stream.Position)];
                    stream.Read(send, 0, send.Length);
                    if (!send_pack(send, client, net_stream)) return false;
                }
                #endregion

                #region Lost Packages

                int lost_size_fails = 0;
                do
                {
                    // Wait for user data, if disconnected return false
                    if (!Await_Data_Disconnect(client)) return false;

                    long lost_size = receive_size(client, net_stream);
                    if (lost_size == 1 )
                        break;
                    else if (lost_size == -1) return false;
                    else if (lost_size == 0)
                    {
                        lost_size_fails++;
                        if (lost_size_fails >= 3) return false;
                        continue;
                    }

                    lost_size_fails = 0;


                    // Receive lost list
                    byte[] b_lost = new byte[lost_size];
                    lost_list = new long[lost_size / 8];

                    b_lost = receive_pack((int)lost_size, client, net_stream);
                    if (b_lost == null) return false;
                    else if (b_lost.Length == 0) continue;
                    Buffer.BlockCopy(b_lost, 0, lost_list, 0, b_lost.Length);

                    clear_netstream(client, net_stream);

                    for (int i = 0; i < lost_list.Length; i++)
                    {
                        stream.Position = lost_list[i];
                        int c_pack_size = (int)Math.Min(size - lost_list[i], pack_size);
                        send = new byte[c_pack_size];
                        stream.Read(send, 0, send.Length);

                        if (client.Available > 0) break;
                        if (!send_pack(send, client, net_stream)) return false;
                    }

                } while (true);

                // Send 255 confirmation byte
                try{
                    net_stream.WriteByte(255);
                }catch{return false;}

                #endregion
            }

            return true;
        }

        public bool Receive(ref Stream stream, TcpClient client, NetworkStream net_stream)
        {
            // Receive size
            int pack_size = this.pack_size;
            long size = receive_size(client, net_stream);
            if (size == -1) return false;

            List<long> n_lost_pack_offsets = new List<long>();
            long[] lost_pack_offsets = new long[0];
            byte[] rec = new byte[pack_size];
            
            //using (MemoryStream ms = new MemoryStream())
            {
                #region Initial Packages
                while (stream.Position + pack_size <= size)
                {
                    rec = receive_pack(pack_size, client, net_stream);

                    // if Hash doesnt match
                    if (rec == null)
                        return false;
                    else if (rec.Length == 0)
                    {
                        n_lost_pack_offsets.Add(stream.Position);
                        stream.Position += pack_size;
                    }

                    stream.Write(rec, 0, rec.Length);
                }

                if ((int)(size - stream.Position) > 0)
                {
                    rec = receive_pack((int)(size - stream.Position), client, net_stream);

                    // if Hash doesnt match
                    if (rec == null)
                        return false;
                    else if (rec.Length == 0)
                    {
                        n_lost_pack_offsets.Add(stream.Position);
                        stream.Position += (int)(size - stream.Position);
                    }

                    stream.Write(rec, 0, rec.Length);
                }
                #endregion

                #region Lost Packages

                // Send a 1 if everything is ok
                // else send offset_list
                while (n_lost_pack_offsets.Count > 0)
                {
                    lost_pack_offsets = (long[])n_lost_pack_offsets.ToArray().Clone();
                    n_lost_pack_offsets.Clear();


                    clear_netstream(client, net_stream);

                    // Create lost_pack_send_data
                    byte[] lost_offsets = new byte[lost_pack_offsets.Length * 8];
                    Buffer.BlockCopy(lost_pack_offsets, 0, lost_offsets, 0, lost_offsets.Length);

                    if (!send(lost_offsets, client, net_stream)) return false;

                    for (int i = 0; i < lost_pack_offsets.Length; i++)
                    {
                        stream.Position = lost_pack_offsets[i];
                        int c_pack_size = (lost_pack_offsets[i] + pack_size >= size ?  (int)(size-lost_pack_offsets[i]) : pack_size);
                        rec = receive_pack(c_pack_size,client,net_stream);

                        if (rec == null)
                            return false;
                        else if (rec.Length == 0)
                        {
                            for (int y = i; y < lost_pack_offsets.Length; y++)
                                n_lost_pack_offsets.Add(lost_pack_offsets[y]);
                            break;
                        }
                        else
                            stream.Write(rec, 0, rec.Length);
                    }
                }

                // All received correctly
                if (!send_size(1, client, net_stream)) return false;
                if (!Await_Data_Disconnect(client)) return false;

                try
                {
                    if (net_stream.ReadByte() != 255) return false;
                }
                catch { return false; }

                //received = stream.ToArray();

                //stream.Close();
                //stream.Dispose();

                #endregion
            }

            return true;
        }

        private bool send(byte[] data, TcpClient client, NetworkStream net_stream)
        {
            if (!send_size(data.Length, client, net_stream)) return false;
            if (!send_pack(data, client, net_stream)) return false;
            return true;
        }

        private long receive_size(TcpClient client, NetworkStream net_stream)
        {
            byte[] b_size = receive_pack(8, client, net_stream);
            if (b_size == null) return -1;
            else if (b_size.Length == 0) return 0;

            long[] a_size = new long[1];
            Buffer.BlockCopy(b_size, 0, a_size, 0, 8);
            return a_size[0];
        }

        private bool send_size(long size, TcpClient client, NetworkStream net_stream)
        {
            byte[] b_size = new byte[8];
            long[] a_size = new long[] { size };

            Buffer.BlockCopy(a_size, 0, b_size, 0, 8);
            send_pack(b_size, client, net_stream);
            return true;
        }

        public bool Write_UDP(byte[] data, IPAddress ip, int port)
        {
            UdpClient udp = new UdpClient();
            udp.Send(data,data.Length, new IPEndPoint(ip,port));
            return true;
        }
        
        private bool Await_Data_Disconnect(TcpClient client)
        {
            while (client.Available == 0)
            {
                if (!connected(client) || reconnected == true)
                    return false;
                if (await_delay)
                    Thread.SpinWait(100);
            }
            return true;
        }
    }
}
