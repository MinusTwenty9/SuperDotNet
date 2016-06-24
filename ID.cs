using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuperDotNet
{
    public static class ID
    {
        public static int id_length = 16;
        private static Random rand = new Random();
        private static List<string> prev_ids = new List<string>();

        // id_length byte's long, all readable/savable chars
        public static string Generate_ID()
        {
            string id;
            
            do
            {
                id = "";
                for (int i = 0; i < id_length; i++)
                {
                    byte rand_byte = (byte)(rand.Next(62)+48);
                    if (rand_byte >= 58) rand_byte += 7;
                    if (rand_byte >= 91) rand_byte += 6;
                    id += Encoding.ASCII.GetString(new byte[]{rand_byte});
                }
            }while(prev_ids.Where(x => x == id).ToArray().Length > 0);

            prev_ids.Add(id);
            return id;
        }
    }
}
