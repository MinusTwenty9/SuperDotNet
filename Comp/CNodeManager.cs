using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using SuperDotNet.Network;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SuperDotNet.CComp
{
    public class CNodeManager
    {
        public CNode[] nodes;
        public Type instance_type;

        public CNodeManager(CNode[] nodes)
        {
            this.nodes = nodes;
        }

        // Need to run the Nodes intelegently (not to mutch at once)
        // Run them in thread (tasks) 
        // Wait until all have finished and then return
        #region RunXX
        // Returns the serialized return parameters from the function's
        private object[] runxx(string function_name, string[] ids, object[][] b_parameters,bool fx)    // object[] = parameters || file_name
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            object[] back = new object[ids.Length];
            List<Task<object>> tasks = new List<Task<object>>();

            for (int i = 0; i < ids.Length; i++)
            {
                CNode node = ID_2_Node(ids[i]);
                if (node == null) continue;

                object[] parameters;

                if (fx == false)
                    parameters = b_parameters[i];
                else
                    parameters = b_parameters[i];

                tasks.Add(node.RunF(function_name, parameters));
                
            }

            // Start tasks parallel
            Parallel.ForEach(tasks, task => task.Start());
            Task.WaitAll(tasks.ToArray());

            // Acquire task object results and covert them directly into byte[]
            for (int i = 0; i < ids.Length; i++)
            {
                back[i] = tasks[i].Result;
                tasks[i].Dispose();
            }
            sw.Stop();
            Console.WriteLine("runxxx in " + sw.ElapsedMilliseconds.ToString());
            return back;
        }

        public string[] RunXF(string function_name, string[] ids, object[][] b_parameters, bool fx)
        {
            object[] obj = runxx(function_name,ids,b_parameters,fx);
            string[] ret_paths = obj.Select(x => (x==null?string.Empty : x.ToString())).ToArray();

            return ret_paths;
        }

        public byte[] RunXP(string function_name, string[] ids, object[][] b_parameters, bool fx)
        {
            object[] obj = runxx(function_name, ids, b_parameters, fx);
            byte[] back = General.object_2_bytearray((object)obj);//obj.Select(x => General.object_2_bytearray(x)).ToArray();

            return back;
        }

        #endregion

        #region Helper Functions
        
        public CNode ID_2_Node(string id)
        {
            CNode[] cn = nodes.Where(n=>n.id == id).ToArray();
            if (cn == null || cn.Length != 1) return null;

            return cn[0];
        }

        #endregion
    }
}
