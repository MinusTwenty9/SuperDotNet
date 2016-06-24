using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;

namespace SuperDotNet.CComp
{
    public class CNode
    {
        public string id;
        public object instance;
        public Type type;
        public string local_dir;
        public string simulation_name;
        
        public CNode(Type type, string id, object[] init_parameters, string simulation_name)
        {
            this.id = id;
            this.type = type;
            this.simulation_name = simulation_name;
            this.local_dir = CSimulation.sims_dir + "/"+simulation_name + "/"+id;
            
            if (!Directory.Exists(this.local_dir))
                Directory.CreateDirectory(this.local_dir);

            ConstructorInfo cinfo = type.GetConstructor(Type.GetTypeArray(init_parameters));
            if (cinfo == null) return;

            instance = cinfo.Invoke(init_parameters);
        }

        // Returns a task taht when startet runs the runf function
        public Task<object> RunF(string function_name, object[] parameters)
        {
            return new Task<object>(() => runf(function_name, parameters));
        }

        // Invokes the function_name from the initialized type
        // Returns the object returned (void = null)
        private object runf(string function_name, object[] parameters)
        {
            MethodInfo minfo = type.GetMethod(function_name);
            if (minfo == null) return null;

            object ret = minfo.Invoke(instance,parameters);

            return ret;
        }

        public string Get_Download_File_Path()
        { 
            string path="";

            while (path == "" || File.Exists(path))
                path = local_dir + "/downloads/"+ID.Generate_ID()+".cache";
            return path;
        }
    }
}
