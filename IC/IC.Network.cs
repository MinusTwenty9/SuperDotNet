using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using SuperDotNet.Network;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace SuperDotNet.SComp
{
    // Network stuff
    public partial class IC
    {
        private T[] np_parallel<T>(SClient[] clients, Func<SClient, Object[], T> action)
        {
            return np_parallel(clients, action, new object[clients.Length]);
        }

        private T[] np_parallel<T>(SClient[] clients, Func<SClient,Object[], T> action,Object[] parameters)
        {
            Task<T>[] tasks = new Task<T>[clients.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                SClient client = clients[i];
                object[] param = (object[])parameters[i];
                Task<T> t = new Task<T>(()=>action(client,param));        // [TODO] be careful
                t.Start();
                tasks[i] = t;
            }

            Task.WaitAll(tasks);
            T[] back = new T[tasks.Length];
            for (int i = 0; i < back.Length; i++)
            {
                back[i] = tasks[i].Result;
                tasks[i].Dispose();
            }

            return back;
        }

        private SClient client_id_2_client(string client_id)
        {
            SClient[] sc = server.clients.Where(n => n.client_id == client_id).ToArray();
            if (sc == null || sc.Length != 1) return null;

            return sc[0];
        }

        private void np_send_server_ready()
        {
            // [TODO] ?
            server.Write_UDP(new byte[]{1},IPAddress.Any, port);
        }

        // Tells the Comp to return a Performance score 
        private Tuple<string, double> np_get_performance_score(SClient client, object param)
        {
            NetworkHeader.Send_Header(client,NetworkProtocol.Get_Performance_Score);
            NetworkProtocol proc = NetworkHeader.Await_Header(client);

            if (proc != NetworkProtocol.Ret_Performance_Score) return null;

            byte[] data = client.Receive();
            double[] performance = new double[1];

            Buffer.BlockCopy(data,0,performance,0,data.Length);

            return new Tuple<string, double>(client.client_id,performance[0]);
        }

        // Tells the Comp to create a new simulation
        // Simulation_Name
        // Assemblies
        // Instance_Type
        // ID's
        private bool np_create_new_simulation(SClient client, object[] param)
        {
            // param[0] = simulation_name (string)
            // param[1] = InstanceType (Type)
            // param[2] = asms (Assemblies[])
            // param[3] = ids (string[])
            string sim_name = (string)param[0];
            Type inst_type = (Type)param[1];
            Assembly[] asms = (Assembly[])param[2];
            string[] ids = (string[])param[3];

            // Send r0
            // xb simulation_name
            // 1b \0
            // xb InstanceType
            // 1b \0
            // 4b asm_length
            // 16xb ids
            
            // Send Files   rf1
            // xb asms[x]
            byte[] r0;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(Encoding.UTF8.GetBytes(sim_name));
                writer.Write((byte)0);
                writer.Write(Encoding.UTF8.GetBytes(inst_type.FullName));
                writer.Write((byte)0);
                writer.Write(asms.Length);

                for (int i = 0; i < ids.Length; i++)
                {
                    writer.Write(Encoding.UTF8.GetBytes(ids[i]));
                }
                r0 = ms.ToArray();
                writer.Close();
                writer.Dispose();
                ms.Close();
                ms.Dispose();
            }

            NetworkHeader.Send_Header(client, NetworkProtocol.Create_New_Simulation);
            if (!client.Send(r0)) return false;

            for (int i = 0; i < asms.Length; i++)
            {
                // NetworkHeader
                if (!client.Send_File(asms[i].Location)) return false;
            }

                return true;
        }

        private object[] np_run_p(SClient client, object[] param)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            // param[0] = func_name (string)
            // param[1] = ids (string[])
            // param[2] = parameters (object[][])
            // param[3] = file_paths (string[] || null)

            string func_name = (string)param[0];
            string[] ids = (string[])param[1];
            object[][] parameters = (object[][])param[2];
            string[] file_paths = (string[])param[3];
            //int parameter_count = parameters[0].Length;
            int id_count = ids.Length;

            if (file_paths != null && file_paths.Length != ids.Length)
                throw new ArgumentException("There must be equaly many file paths as ids");

            // Send r0
            // xb func_name \0
            // 4b id_length
            // 1b return_file
            // 4b param_count // NOT 
            // 16xb ids
            // xb parameter_bytes

            byte[] b_param = General.object_2_bytearray((object)parameters);
            byte[] r0;

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(Encoding.UTF8.GetBytes(func_name));
                writer.Write((byte)0);
                writer.Write(id_count);
                writer.Write(file_paths != null);

                for (int i = 0; i < ids.Length; i++)
                    writer.Write(Encoding.UTF8.GetBytes(ids[i]));
                writer.Write(b_param);
                r0 = ms.ToArray();

                writer.Close();
                writer.Dispose();
                ms.Close();
                ms.Dispose();
            }

            NetworkHeader.Send_Header(client, NetworkProtocol.Run_P);
            if (!client.Send(r0)) return null;

            object[] back = null;
            NetworkProtocol protocol = NetworkHeader.Await_Header(client);

            // Download Return params or files
            // Return parameters
            if (protocol == NetworkProtocol.Run_Return_P)
            {
                byte[] rec = client.Receive();
                back = (object[])General.bytearray_2_object(rec);
            }
            // Return Files
            else if (protocol == NetworkProtocol.Run_Return_F)
            {
                back = new object[file_paths.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    client.Receive_File(file_paths[i]);
                    back[i] = (object)file_paths[i];
                }
            }

            //sw.Stop();
            //Console.WriteLine("IC run_p: " + sw.ElapsedMilliseconds);
            return back;
        }

        private object[] np_run_f(SClient client, object[] param)
        {
            // param[0] = func_name (string)
            // param[1] = ids (string[])
            // param[2] = files (string[])
            // param[3] = file_paths (string[] || null)

            string func_name = (string)param[0];
            string[] ids = (string[])param[1];
            string[] files = (string[])param[2];
            string[] file_paths = (string[])param[3];
            //int parameter_count = parameters[0].Length;
            int id_count = ids.Length;

            if (file_paths != null && file_paths.Length != ids.Length)
                throw new ArgumentException("There must be equaly many file paths as ids");

            // Send r0
            // xb func_name \0
            // 4b id_length
            // 1b return_file
            // 16xb ids

            // Send Files
            // xb for file in files: send file

            byte[] r0;

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(Encoding.UTF8.GetBytes(func_name));
                writer.Write((byte)0);
                writer.Write(id_count);
                writer.Write(file_paths != null);

                for (int i = 0; i < ids.Length; i++)
                    writer.Write(Encoding.UTF8.GetBytes(ids[i]));

                r0 = ms.ToArray();

                writer.Close();
                writer.Dispose();
                ms.Close();
                ms.Dispose();
            }

            NetworkHeader.Send_Header(client, NetworkProtocol.Run_F);
            if (!client.Send(r0)) return null;

            // Send files
            for (int i = 0; i < files.Length; i++)
            {
                if (!File.Exists(files[i])) client.Send(new byte[0]);
                else client.Send_File(files[i]);
            }


            object[] back = null;
            NetworkProtocol protocol = NetworkHeader.Await_Header(client);

            // Download Return params or files
            // Return parameters
            if (protocol == NetworkProtocol.Run_Return_P)
            {
                byte[] rec = client.Receive();
                back = (object[])General.bytearray_2_object(rec);
            }
            // Return Files
            else if (protocol == NetworkProtocol.Run_Return_F)
            {
                back = new object[file_paths.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    client.Receive_File(file_paths[i]);
                    back[i] = (object)file_paths[i];
                }
            }

            return back;
        }

        // Returns ID's
        private string[] np_load_sim(SClient client, object[] param)
        {
            // Parameters
            // param[0] = sim_name
            string sim_name = (string)param[0];

            NetworkHeader.Send_Header(client,NetworkProtocol.Load_Sim);
            client.Send(Encoding.UTF8.GetBytes(sim_name));

            NetworkProtocol protocol = NetworkHeader.Await_Header(client);
            if (protocol != NetworkProtocol.Ret_Sim_IDs) return new string[0];

            byte[] b_ids = client.Receive();
            string[] ids = new string[b_ids.Length/ID.id_length];

            using (MemoryStream ms = new MemoryStream(b_ids))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = Encoding.UTF8.GetString(reader.ReadBytes(ID.id_length));

                reader.Close();
                reader.Dispose();
                ms.Close();
                ms.Dispose();
            }

            return ids;
        }
    }
}
