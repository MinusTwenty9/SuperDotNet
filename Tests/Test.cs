using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperDotNet.CComp;
using SuperDotNet;
using SuperDotNet.SComp;
using System.Diagnostics;
using System.IO;

namespace SuperDotNet.Tests
{
    public static class Test
    {
        public static void CNodeManagerTest()
        {
            CNode[] nodes = new CNode[2];
            string[] ids = new string[]{ID.Generate_ID(),ID.Generate_ID()};
            nodes[0] = new CNode(typeof(TestInst), ids[0], new object[] { ids[0] }, "CNodeManagerTest");
            nodes[1] = new CNode(typeof(TestInst), ids[1], new object[] { ids[1] }, "CNodeManagerTest");

            CNodeManager manager = new CNodeManager(nodes);

            byte[]test = manager.RunXP("Run",ids,new object[][]{((object[])new string[]{"1","2"}),((object[]) new string[]{"3","4"})},false);
            object[] obj = (object[])General.bytearray_2_object(test);
            string[] back = obj.Select(x=>(string)x).ToArray();

            //test = manager.RunXP("Run2",ids ,new object[]{General.object_2_bytearray(new object[]{new string[]{"1","2"},(object)27}),General.object_2_bytearray(new object[]{ new string[]{"3","4"},(object)13})},false);
            //back = test.Select(x => (string)General.bytearray_2_object(x)).ToArray();
        }

        public static void ICCreateTest()
        {
            IC ic = new IC();
            Console.ReadLine();

            string[] node_ids = null;
            ic.Create_New_Simulation(ref node_ids, 5,"Test_Sim", typeof(TestInst));
        }

        public static void CompCreateTest()
        {
            string ip = "";//= Console.ReadLine();
            ip = (ip == "" ? "192.168.179.1" : ip);

            SuperDotNet.Network.Network.await_delay = false;
            Comp comp = new Comp(ip);
        }

        public static void ICCreateSimulation()
        {
            IC ic = new IC();
            Console.ReadLine();
            string[] node_ids = null;
            ic.Create_New_Simulation(ref node_ids, 4,"ICCreateSimulationTest2",typeof(TestInst));
            object[][] data = new object[4][];
            string[] files = new string[4];

            for (int i = 0; i < files.Length; i++)
                files[i] = "./Files/" + node_ids[i] + ".txt";

            string[] back;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 1; i++)
            {
                Stopwatch sw2 = new Stopwatch();
                sw2.Start();
                back = ic.RunPX<string>(node_ids, "file_test", data,files);
                for (int y = 0; y < back.Length; y++)
                    if (back[y] == null)
                    { }
                sw2.Stop();
                Console.WriteLine("Send Speed: " + sw2.ElapsedMilliseconds);
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        public static void ICFileParameterTest()
        {
            IC ic = new IC();
            Console.ReadLine();

            string[] nodes = null;
            ic.Create_New_Simulation(ref nodes, 12, "ICFileParameterTest",typeof(TestInst));

            if (!Directory.Exists("./Upload_Files/"))
                Directory.CreateDirectory("./Upload_Files");

            Random rand = new Random();
            byte[] data = new byte[32*1024*1024];
            string[] file_paths = new string[nodes.Length];
            string[] ret_file_paths = new string[nodes.Length];

            for (int i = 0; i < file_paths.Length; i++)
            {
                rand.NextBytes(data);//Encoding.UTF8.GetBytes(nodes[i]);
                file_paths[i] = "./Upload_Files/" + nodes[i] + ".up";
                ret_file_paths[i] = "./Download_Files/" + nodes[i] + ".down";
                File.WriteAllBytes(file_paths[i],data);
            }

            ic.RunFX<string>(nodes, "Run_File", file_paths, ret_file_paths);
        }

        public static void ICSaveLoadTest()
        {
            IC ic = new IC();
            Console.ReadLine();

            //string[] nodes = null;
            //ic.Create_New_Simulation(ref nodes, 12, "ICSaveLoadTest", typeof(TestInst));
            //ic.Save("./Sim_Saves/ICSaveLoadTest.sim");

            //ic.Create_New_Simulation(ref nodes, 12, "ICSaveLoadTest2", typeof(TestInst));

            bool load = ic.Try_Load_Simulation("./Sim_Saves/ICSaveLoadTest.sim");
        }
    }
}
