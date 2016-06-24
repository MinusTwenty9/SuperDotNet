using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SuperDotNet.Network
{
    public static class NetworkHeader
    {
        public static NetworkProtocol Await_Header(BClient client)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            client.Await_Data();
            //sw.Stop();
            //Console.WriteLine("NetworkHeader Await_Header: " + sw.ElapsedMilliseconds);

            return Receive_Header(client);
        }

        public static NetworkProtocol Receive_Header(BClient client)
        {
            byte[] head = client.Receive();
            return Receive_Header(head);
        }
        public static NetworkProtocol Receive_Header(byte[] head)
        {
            int[] i_head = new int[1];

            Buffer.BlockCopy(head, 0, i_head, 0, 4);
            return (NetworkProtocol)i_head[0];
        }

        public static void Send_Header(BClient client, NetworkProtocol protocol)
        { 
            int[] i_head = new int[]{(int)protocol};
            byte[] data = new byte[4];

            Buffer.BlockCopy(i_head,0,data,0,4);
            client.Send(data);
        }
    }

    public enum NetworkProtocol
    { 
        Error = 0,
        Get_Performance_Score = 1,
        Ret_Performance_Score = 2,
        Create_New_Simulation = 3,
        Run_P = 4,
        Run_F = 5,
        Run_Return_P = 6,
        Run_Return_F = 7,
        Load_Sim = 8,
        Ret_Sim_IDs = 9,
        Ret_Not_Part_Of_Sim = 10
    }
}
