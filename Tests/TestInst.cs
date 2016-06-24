using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SuperDotNet.Tests
{
    public class TestInst
    {
        string id;
        string[] str;
        string dir;
        public TestInst(string id, string dir)
        {
            this.id = id;
            this.dir = dir;
            Console.WriteLine(id+": Initializing instance");
            Console.WriteLine(id + ": Assigned Local Dir: "+dir);
        }

        public string Run(string[] str)
        {
            this.str = new string[2];
            this.str[0] = str[1];
            this.str[1] = str[0];

            Console.WriteLine(id + ": Function Run");

            return "HelloWorld!";
        }

        public string[] Run2(string[] str, int int32)
        {
            Console.WriteLine(id + ": Function Run2");
            return new string[]{str[0],int32.ToString()};
        }

        public string file_test()
        {
            Random rand = new Random();
            byte[] buff = new byte[32*1024*1024];
            string path = dir + "/file.txt";

            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                rand.NextBytes(buff);
                stream.Write(buff,0,buff.Length);
                stream.Close();
                stream.Dispose();
            }
            return path;
        }

        public string Run_File(string file_path)
        {
            return file_test();
        }
    }
}
