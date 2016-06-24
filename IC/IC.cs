using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperDotNet.Network;
using SuperDotNet;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace SuperDotNet.SComp
{
    public partial class IC
    {
        private Server server;
        private ISimulation simulation;

        public int port = 1234;

        #region Constructor

        public IC()
        {
            server = new Server(port);
            server.ServerNewClient += new server_new_client(server_ServerNewClient);
            //server.UDP_Ready();
        }

        private void server_ServerNewClient(SClient client)
        {
            client.client_id = ID.Generate_ID();
        }

        #endregion

        public void Create_New_Simulation(ref string[] node_ids, int node_count, string sim_name, Type type)
        {
            List<string> l_node_ids = new List<string>();
            node_ids = new string[node_count];
            int node_id_index = 0;

            #region Assign nodes

            List<NodeInfo[]> nodes = new List<NodeInfo[]>();

            // Get Performance Score
            Tuple<string, double>[] p_scores = np_parallel(server.clients.ToArray(), np_get_performance_score);
            double t_score = 0;
            int assigned_nodes = 0;

            // Get total p_scores
            for (int i = 0; i < p_scores.Length; i++)
                t_score += p_scores[i].Item2;
            // Distribute nodes accordingly
            for (int i = 0; i < p_scores.Length; i++)
            { 
                int c_nodes = (int)(Math.Ceiling((p_scores[i].Item2/t_score) * node_count));
                
                // if due to inaccuracy in the floating point the equation doesn't assign perfectly
                if (assigned_nodes + c_nodes > node_count) 
                    c_nodes = node_count - assigned_nodes;
                assigned_nodes += c_nodes;

                NodeInfo[] client_nodes = new NodeInfo[c_nodes];
                for (int n = 0; n < c_nodes; n++)
                {
                    string id = ID.Generate_ID();
                    
                    node_ids[node_id_index] = id;
                    node_id_index++;

                    l_node_ids.Add(id);
                    client_nodes[n] = new NodeInfo(id, p_scores[i].Item1);
                }

                nodes.Add(client_nodes);
            }
            #endregion

            Assembly[] asms = get_all_referenced_assemblies(type.Assembly);
            object[] parameters = new object[nodes.Count];

            for (int i = 0; i < nodes.Count; i++)
            {
                string[] client_ids = nodes[i].Select(node => node.id).ToArray();
                parameters[i] = new object[] { sim_name, type, asms, client_ids};
            }

            bool[] back = np_parallel<bool>(get_node_clients(nodes), np_create_new_simulation, parameters);
            simulation = new ISimulation(nodes,sim_name,type);
        }

        public T[] RunPX<T>(string[] ids, string func_name, object[][] parameters, string[] ret_file_paths)
        {
            return run<T>(ids, func_name, parameters, ret_file_paths,true);
        }

        public T[] RunFX<T>(string[] ids, string func_name, string[] file_paths, string[] ret_file_paths)
        { 
            object[][] parameters = new object[file_paths.Length][];

            for (int i  = 0; i < parameters.Length; i++)
                parameters[i] = new object[1]{file_paths[i]};

            return run<T>(ids,func_name,parameters, ret_file_paths, false);
        }

        private T[] run<T>(string[] ids, string func_name, object[][] parameters, string[] file_paths, bool px)
        {
            if (ids.Length != parameters.Length) return null;

            SClient[] clients;
            List<List<NodeInfo>> clients_info = new List<List<NodeInfo>>();
            List<List<object[]>> param_info = new List<List<object[]>>();
            List<List<int>> index_info = new List<List<int>>();
            List<List<string>> file_paths_info = new List<List<string>>();

            #region Assign provided ids to SClients

            for (int i = 0; i < ids.Length; i++)
            {
                for (int n = 0; n < simulation.nodes.Count; n++)
                {
                    // Find client_id
                    NodeInfo[] found_nodes = simulation.nodes[n].Where(s => s.id == ids[i]).ToArray();
                    if (found_nodes.Length == 0) continue;

                    // Check if client already exists
                    bool c_found = false;
                    for (int c = 0; c < clients_info.Count; c++)
                    {
                        if (clients_info[c][0].client_id == found_nodes[0].client_id)
                        {
                            clients_info[c].Add(found_nodes[0]);
                            param_info[c].Add(parameters[i]);
                            index_info[c].Add(i);
                            if (file_paths != null) file_paths_info[c].Add(file_paths[i]);
                            c_found = true;
                            break;
                        }
                    }
                    if (!c_found)
                    {
                        clients_info.Add(new List<NodeInfo>() { found_nodes[0] });
                        param_info.Add(new List<object[]>() { parameters[i] });
                        index_info.Add(new List<int>() { i });
                        if (file_paths != null) file_paths_info.Add(new List<string>() { file_paths[i] });
                    }
                    break;
                }
            }

            clients = get_node_clients(clients_info);
            #endregion

            #region Prepear the parameters
            object[] p = new object[param_info.Count];
            for (int i = 0; i < p.Length; i++)
            {
                object[] c_p = new object[4];
                c_p[0] = func_name;
                c_p[1] = clients_info[i].Select(client => client.id).ToArray();
                
                // String for the files to send
                if (px==false) c_p[2] = param_info[i].Select(f=> (f[0] == null ? string.Empty : f[0].ToString())).ToArray();
                // Object based parameter data
                else c_p[2] = param_info[i].ToArray();

                if (file_paths != null) c_p[3] = file_paths_info[i].ToArray();
                else c_p[3] = null;
                p[i] = (object)c_p;
            }
            #endregion

            #region Send and Wait
            object[][] rec;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            if (px) rec = np_parallel(clients, np_run_p, p);
            else rec = np_parallel(clients, np_run_f, p);
            sw.Stop();
            //Console.WriteLine("IC run send: " + sw.ElapsedMilliseconds);

            if (rec == null) return null;

            #endregion


            // Prepear for return
            T[] back = new T[ids.Length];
            for (int i = 0; i < rec.Length; i++)
            {
                for (int y = 0; y < clients_info[i].Count; y++)
                    back[index_info[i][y]] = (T)rec[i][y];
            }

            return back;
        }

        public bool Try_Load_Simulation(string save_file)
        {
            if (!File.Exists(save_file)) return false;
            ISimulation sim = new ISimulation(save_file);

            List<string> req_ids = new List<string>();
            List<NodeInfo[]> nodes = new List<NodeInfo[]>();
            SClient[] clients = server.clients.ToArray();
            object[][] param = new object[clients.Length][];

            // Set parameters to sim_name
            for (int i = 0; i < param.Length; i++)
                param[i] = new object[]{sim.simulation_name};

            string[][] c_ids = np_parallel<string[]>(clients, np_load_sim, param);

            // Calculate Required ids
            req_ids = sim.ids.ToList();

            // Creates a new node to comp map (nodes)
            for (int i = 0; i < c_ids.Length; i++)
            {
                if (c_ids[i].Length == 0) continue;

                string client_id = ID.Generate_ID();
                NodeInfo[] ni = new NodeInfo[c_ids[i].Length];
                for (int y = 0; y < ni.Length; y++)
                {
                    ni[y] = new NodeInfo(c_ids[i][y], client_id);
                    req_ids.Remove(ni[y].id);
                }
                nodes.Add(ni);
            }

            if (req_ids.Count != 0)
            {
                this.simulation = null;
                return false;
            }

            sim.nodes = nodes;
            this.simulation = sim;

            return true;
        }

        public void Save(string save_file)
        {
            if (simulation == null) return;
            simulation.Save_Sim(save_file);
        }

        #region HelperFunctions
        private Assembly[] get_all_referenced_assemblies(Assembly b_asm)
        {
            Assembly asm = b_asm;
            List<Assembly> refs = new List<Assembly>();
            List<Assembly> n_refs = new List<Assembly>();
            n_refs.Add(asm);

            do
            {
                List<Assembly> c_refs = new List<Assembly>();
                foreach (Assembly a in n_refs)
                    foreach (AssemblyName an in a.GetReferencedAssemblies())
                    {
                        Assembly c_a = Assembly.Load(an);
                        string token = an.FullName.Split(new string[] { "PublicKeyToken=" }, StringSplitOptions.None)[1];
                        if (token == "b77a5c561934e089" || token == "b03f5f7f11d50a3a")
                            continue;

                        if (refs.Where(r => r.GetName().Name == an.Name).ToArray().Length == 0 &&
                            n_refs.Where(r => r.GetName().Name == an.Name).ToArray().Length == 0 &&
                            c_refs.Where(r => r.GetName().Name == an.Name).ToArray().Length == 0)
                        {
                            c_refs.Add(Assembly.Load(an));
                        }
                    }

                foreach (Assembly a in n_refs)
                    refs.Add(a);

                n_refs = ((Assembly[])c_refs.ToArray().Clone()).ToList();
            }
            while (n_refs.Count > 0);
            return refs.ToArray();
        }

        private SClient[] get_node_clients(List<NodeInfo[]> nodes)
        {
            return nodes.Select(node => client_id_2_client(node[0].client_id)).ToArray(); ;
        }
        private SClient[] get_node_clients(List<List<NodeInfo>> nodes)
        {
            return nodes.Select(node => client_id_2_client(node[0].client_id)).ToArray(); ;
        }
        #endregion
    }
}
