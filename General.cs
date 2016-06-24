using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace SuperDotNet
{
    public static class General
    {
        public static object[] get_params(byte[] data)
        {
            object obj = bytearray_2_object(data);
            if (obj.GetType() == typeof(object[]))
                return (object[])obj;
            else 
                return new object[] { obj };
        }

        public static byte[] object_2_bytearray(object obj)
        {
            if (obj == null) return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static object bytearray_2_object(byte[] data)
        {
            if (data == null) return null;
            using (var ms = new MemoryStream())
            {
                var bf = new BinaryFormatter();
                ms.Write(data, 0, data.Length);
                ms.Seek(0, SeekOrigin.Begin);
                var obj = bf.Deserialize(ms);
                return obj;
            }
        }

        public static string read_0_string(ref Stream stream)
        {
            string back = "";

            byte b;
            while (true)
            {
                b = (byte)stream.ReadByte();
                if (b == 0||b==255) break;
                back += Encoding.UTF8.GetString(new byte[]{b});
            }
            return back;
        }
    }
}
