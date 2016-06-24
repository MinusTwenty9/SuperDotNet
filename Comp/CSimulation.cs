using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace SuperDotNet.CComp
{
    // Static CSimulation
    public partial class CSimulation
    {
        public static string Create_Sim_Dir(string sim_name)
        {
            string sim_dir = sims_dir + "/" + sim_name;
            for (int i = 0; i < 3; i++ )
                try
                {
                    if (Directory.Exists(sim_dir)) Directory.Delete(sim_dir, true);
                    if (!Directory.Exists(sim_dir)) break;     ////// [TODO]
                }
                catch { }

            if (Directory.Exists(sim_dir)) return "";

            Directory.CreateDirectory(sim_dir);
            while (!Directory.Exists(sim_dir)) ;    // [TODO]

            return sim_dir;
        }

        public static CSimulation Load_CSimulation(string sim_name)
        {         
            // 4b asm_path_count
            // 4b node_count
            // xb instance_type \0
            // xb sim_name \0
            // xb sim_dir \0
            // xb node_ids
            // xb for asm in asms: asm.path \0
            string save_file = sims_dir + "/" + sim_name + "/" + sim_name + ".save";
            string instance_type;
            string _sim_name;
            string sim_dir;
            string[] node_ids;
            string[] assembly_paths;
            
            using (FileStream fs = new FileStream(save_file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                Stream stream = fs;

                int asm_count = reader.ReadInt32();
                int node_count = reader.ReadInt32();
                instance_type = General.read_0_string(ref stream);
                _sim_name = General.read_0_string(ref stream);
                sim_dir = General.read_0_string(ref stream);

                
                // Node ID's
                node_ids = new string[node_count];
                for (int i = 0; i < node_ids.Length; i++)
                    node_ids[i] = Encoding.UTF8.GetString(reader.ReadBytes(ID.id_length));

                // Assemblie paths
                assembly_paths = new string[asm_count];
                for (int i = 0; i < assembly_paths.Length; i++)
                {
                    assembly_paths[i] = General.read_0_string(ref stream);
                }

                reader.Close();
                reader.Dispose();
                fs.Close();
                fs.Dispose();
            }

            CSimulation sim = new CSimulation(assembly_paths,instance_type,node_ids, _sim_name,sim_dir);
            return sim;
        }

        public static bool Exists(string sim_name)
        { 
            string sim_dir = sims_dir + "/" + sim_name;
            string save_file = sim_dir + "/"+ sim_name+".save";

            if (!File.Exists(save_file)) return false;

            return true;
        }
    }

    public partial class CSimulation
    {
        public static string sims_dir = "./Simulations";

        private Assembly[] assemblies;
        private string[] assembly_paths;
        public CNodeManager node_manager;
        public Type instance_type;
        public string sim_name;
        public string sim_dir;
        
        public CSimulation(string[] asm_paths, string instance_type, string[] ids, string sim_name, string sim_dir)
        {
            this.assembly_paths = asm_paths;
            this.assemblies = new Assembly[asm_paths.Length];
            this.sim_name = sim_name;
            this.sim_dir = sim_dir;

            // Add Assembly resolver
            load_assemblies(asm_paths);

            AppDomain domain = AppDomain.CurrentDomain;
            domain.AssemblyResolve += new ResolveEventHandler(domain_AssemblyResolve);

            this.instance_type = assemblies[0].GetType(instance_type);
            
            CNode[] nodes = new CNode[ids.Length];
            for (int i = 0; i < ids.Length; i++)
                nodes[i] = new CNode(this.instance_type, ids[i], new object[]{ids[i],sim_dir +"/"+ ids[i]},sim_name);

            this.node_manager = new CNodeManager(nodes);

            Save();
        }
        
        public string[] RunXF(string function_name, string[] ids, object[][] b_parameters, bool fx)
        {
            return node_manager.RunXF(function_name, ids, b_parameters, fx);
        }

        public byte[] RunXP(string function_name, string[] ids, object[][] b_parameters, bool fx)
        {
            return node_manager.RunXP(function_name, ids, b_parameters, fx);
        }

        /// <summary>
        /// Saves on creation for now (data doesn't change over time (yet))
        /// </summary>
        public void Save()
        {
            // 4b asm_path_count
            // 4b node_count
            // xb instance_type \0
            // xb sim_name \0
            // xb sim_dir \0
            // xb node_ids
            // xb for asm in asms: asm.path \0
            
            string save_file = sim_dir + "/"+ sim_name+".save";
            using (FileStream fs = new FileStream(save_file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            {
                fs.SetLength(0);
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    writer.Write(assemblies.Length);
                    writer.Write(node_manager.nodes.Length);
                    writer.Write(Encoding.UTF8.GetBytes(instance_type.FullName));
                    writer.Write((byte)0);
                    writer.Write(Encoding.UTF8.GetBytes(sim_name));
                    writer.Write((byte)0);
                    writer.Write(Encoding.UTF8.GetBytes(sim_dir));
                    writer.Write((byte)0);

                    // Node ID's
                    for (int i = 0; i < node_manager.nodes.Length; i++)
                        writer.Write(Encoding.UTF8.GetBytes(node_manager.nodes[i].id));

                    // Assemblie paths
                    for (int i = 0; i < assembly_paths.Length; i++)
                    {
                        writer.Write(Encoding.UTF8.GetBytes(assembly_paths[i]));
                        writer.Write((byte)0);
                    }

                    writer.Close();
                    writer.Dispose();
                    fs.Close();
                    fs.Dispose();
                }
            }
        }
        
        private Assembly domain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly[] asm = assemblies.Where(d => d.FullName == args.Name).ToArray();
            return (asm == null ? null : asm[0]);
        }

        private void load_assemblies(string[] asm_paths)
        {
            this.assemblies = new Assembly[asm_paths.Length];
            for (int i = 0; i < asm_paths.Length; i++)
            {
                using (FileStream asm_s = new FileStream(Path.GetFullPath(asm_paths[i]), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] raw_asm = new byte[asm_s.Length];
                    asm_s.Read(raw_asm, 0, raw_asm.Length);

                    Assembly asm = Assembly.Load(raw_asm);
                    this.assemblies[i] = asm;
                }
            }
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= domain_AssemblyResolve;

            assemblies = null;
            node_manager = null;
            instance_type = null;
            sim_name = "";
            sim_dir = null;
        }
    }
}
