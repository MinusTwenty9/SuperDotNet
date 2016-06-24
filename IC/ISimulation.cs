using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SuperDotNet.SComp
{
    public class ISimulation
    {
        public List<NodeInfo[]> nodes;
        public Type instance_type;
        public string simulation_name;
        public string[] ids;

        public ISimulation(List<NodeInfo[]> nodes, string simulation_name, Type instance_type)
        {
            this.nodes = ((NodeInfo[][])nodes.ToArray().Clone()).ToList();
            this.simulation_name = simulation_name;
            this.instance_type = instance_type;
            
            int id_index = 0;
            int id_count = 0;

            // Calculate id_count
            for (int i = 0; i < nodes.Count; i++)
                id_count += nodes[i].Length;

            ids = new string[id_count];
                // Set ids
                for (int i = 0; i < nodes.Count; i++)
                    for (int y = 0; y < nodes[i].Length; y++)
                    {
                        ids[id_index] = nodes[i][y].id;
                        id_index++;
                    }

        }

        public ISimulation(string save_path)
        {
            if (!File.Exists(save_path)) return;

            load_sim(save_path);
        }

        private void load_sim(string save_path)
        {
            // Save File Structure
            // 4b node_count
            // xb simulation_name \0
            // xb instance_type \0
            // for all nodes
            // xb node (16b string UTF8)

            int node_count;

            using (FileStream fs = new FileStream(save_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                Stream stream = fs;
                node_count = reader.ReadInt32();
                simulation_name = General.read_0_string(ref stream);
                instance_type = Type.GetType(General.read_0_string(ref stream));

                ids = new string[node_count];
                for (int i = 0; i < node_count; i++)
                    ids[i] = Encoding.UTF8.GetString(reader.ReadBytes(ID.id_length));

                reader.Close();
                reader.Dispose();
                fs.Close();
                fs.Dispose();
            }
        }

        public void Save_Sim(string save_path)
        {
            // Save File Structure
            // 4b node_count
            // xb simulation_name \0
            // xb instance_type \0
            // for all nodes
            // xb node (16b string UTF8)

            int node_count = ids.Length;

            if (!Directory.Exists(Path.GetDirectoryName(save_path)))
                Directory.CreateDirectory(Path.GetDirectoryName(save_path));

            using (FileStream stream = new FileStream(save_path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                stream.SetLength(0);
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((Int32)node_count);
                    writer.Write(Encoding.UTF8.GetBytes(simulation_name));
                    writer.Write((byte)0);
                    writer.Write(Encoding.UTF8.GetBytes(instance_type.FullName));
                    writer.Write((byte)0);

                    for (int i = 0; i < ids.Length; i++)
                        writer.Write(Encoding.UTF8.GetBytes(ids[i]));

                    writer.Close();
                    writer.Dispose();
                    stream.Close();
                    stream.Dispose();
                }
            }
        }
    }
}
