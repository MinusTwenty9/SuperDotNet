using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperDotNet.Network;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using SuperDotNet;

namespace SuperDotNet.CComp
{
    // Network
    public partial class Comp
    {
        private bool interpret_protocol(NetworkProtocol proc)
        {
            switch (proc)
            {
                case NetworkProtocol.Get_Performance_Score: return np_ret_performance_score(); break;
                case NetworkProtocol.Create_New_Simulation: return np_create_new_simulation(); break;
                case NetworkProtocol.Run_P: return np_run_p(); break;
                case NetworkProtocol.Run_F: return np_run_f(); break;
                case NetworkProtocol.Load_Sim: return np_load_sim(); break;
                default: return true;       // [TODO] ?
            }
        }

        // Calculates and then sends the Performance score of the current client
        private bool np_ret_performance_score()
        {
            // Some kind of reliable performance score
            // CPU cores, GHz, run speed test, network card speed,
            // GPU speed, Harddrive speed, etc...
            // [TODO]

            double[] p_score = new double[1]{1.0};
            byte[] data = new byte[8];

            Buffer.BlockCopy(p_score,0,data,0,8);

            NetworkHeader.Send_Header(client,NetworkProtocol.Ret_Performance_Score);
            if (!client.Send(data)) return false;
            return true;
        }

        // Discards the current simulation loaded (doesn't save it)
        // Creates new simulation based on data provided by IC
        private bool np_create_new_simulation()
        {
            byte[] r0 = client.Receive();
            if (r0 == null) return false;

            // Receive r0
            // xb simulation_name
            // 1b \0
            // xb InstanceType
            // 1b \0
            // 4b asm_length
            // 16xb ids

            // Receive Files   rf1
            // xb asms[x]

            string sim_name;
            string inst_type;
            int asms_length;
            string[] asms;
            string[] ids;

            using (MemoryStream ms = new MemoryStream(r0))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                Stream stream = ms;
                sim_name = General.read_0_string(ref stream);
                inst_type = General.read_0_string(ref stream);
                asms_length = reader.ReadInt32();

                ids = new string[(ms.Length - ms.Position) / ID.id_length];
                for (int i = 0; i < ids.Length; i++)
                {
                    ids[i] = Encoding.UTF8.GetString(reader.ReadBytes(ID.id_length));
                }
                reader.Close();
                reader.Dispose();
                ms.Close();
                ms.Dispose();
            }

            string sim_dir = CSimulation.Create_Sim_Dir(sim_name);

            // Download Assemblies
            asms = new string[asms_length];
            string asm_dir = sim_dir + "/Assemblies";
            Directory.CreateDirectory(asm_dir);

            for (int i = 0; i < asms_length; i++)
            {
                string asm_file = asm_dir + "/asm_" + i + ".dll";
                client.Receive_File(asm_file);
                asms[i] = asm_file;
            }

            CSimulation sim = new CSimulation(asms, inst_type, ids, sim_name, sim_dir);
            this.simulation = sim;

            return true;
        }

        private bool np_run_p()
        {
            byte[] r0 = client.Receive();
            if (r0 == null) return false;

            // Receive r0
            // xb func_name \0
            // 4b id_length
            // 1b return_file
            // 16xb ids
            // xb parameter_bytes

            string func_name;
            int id_length;
            string[] ids;
            bool return_files;
            object[][] parameters;

            using (MemoryStream ms = new MemoryStream(r0))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                Stream stream = ms;
                func_name = General.read_0_string(ref stream);
                id_length = reader.ReadInt32();
                return_files = reader.ReadBoolean();

                ids = new string[id_length];
                for (int i = 0; i < id_length; i++)
                    ids[i] = Encoding.UTF8.GetString(reader.ReadBytes(ID.id_length));

                byte[] b_param = reader.ReadBytes((int)(ms.Length - ms.Position));
                parameters = (object[][])General.bytearray_2_object(b_param);
            }

            // PP
            if (return_files == false)
            {
                byte[] back = simulation.RunXP(func_name, ids, parameters, false);
                NetworkHeader.Send_Header(client, NetworkProtocol.Run_Return_P);
                client.Send(back);
            }
            // PF
            else
            {
                string[] files = simulation.RunXF(func_name, ids, parameters, false);
                NetworkHeader.Send_Header(client, NetworkProtocol.Run_Return_F);

                for (int i = 0; i < files.Length; i++)
                {
                    if (!File.Exists(files[i])) client.Send(new byte[0]);
                    client.Send_File(files[i]);
                }
            }

            return true;
        }

        private bool np_run_f()
        {
            byte[] r0 = client.Receive();
            if (r0 == null) return false;

            // Receive r0
            // xb func_name \0
            // 4b id_length
            // 1b return_file
            // 16xb ids

            // Receive Files
            // xb for file in files: send file

            string func_name;
            int id_length;
            string[] ids;
            bool return_files;
            object[][] parameters;

            using (MemoryStream ms = new MemoryStream(r0))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                Stream stream = ms;
                func_name = General.read_0_string(ref stream);
                id_length = reader.ReadInt32();
                return_files = reader.ReadBoolean();

                ids = new string[id_length];
                for (int i = 0; i < id_length; i++)
                    ids[i] = Encoding.UTF8.GetString(reader.ReadBytes(ID.id_length));

                // Receive Files
                parameters = new object[ids.Length][];
                for (int i = 0; i < ids.Length; i++)
                {
                    string file_path = simulation.sim_dir + "/" + ids[i] +"/"+ID.Generate_ID()+".cache";
                    parameters[i] = new object[] { (object)file_path };
                    client.Receive_File(file_path);
                }
            }

            // FP
            if (return_files == false)
            {
                byte[] back = simulation.RunXP(func_name, ids, parameters, true);
                NetworkHeader.Send_Header(client, NetworkProtocol.Run_Return_P);
                client.Send(back);
            }
            // FF
            else
            {
                string[] files = simulation.RunXF(func_name, ids, parameters, true);
                NetworkHeader.Send_Header(client, NetworkProtocol.Run_Return_F);

                for (int i = 0; i < files.Length; i++)
                {
                    if (!File.Exists(files[i])) client.Send(new byte[0]);
                    client.Send_File(files[i]);
                }
            }

            return true;
        }

        private bool np_load_sim()
        {
            byte[] r0 = client.Receive();
            string sim_name = Encoding.UTF8.GetString(r0);

            if (!CSimulation.Exists(sim_name))
            {
                NetworkHeader.Send_Header(client,NetworkProtocol.Ret_Not_Part_Of_Sim);
                return true;
            }

            CSimulation sim = CSimulation.Load_CSimulation(sim_name);
            this.simulation = sim;

            NetworkHeader.Send_Header(client, NetworkProtocol.Ret_Sim_IDs);

            byte[] b_ids;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                for (int i = 0; i < sim.node_manager.nodes.Length; i++)
                    writer.Write(Encoding.UTF8.GetBytes(sim.node_manager.nodes[i].id));
                b_ids = ms.ToArray();

                writer.Close();
                writer.Dispose();
                ms.Close();
                ms.Dispose();
            }

            client.Send(b_ids);

            return true;
        }
    }
}
