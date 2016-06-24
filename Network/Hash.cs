using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace SuperDotNet.Network
{
    public static class Hash
    {
        private static SHA1CryptoServiceProvider sha1;
        public static int hash_length = 20;

        public static byte[] Get_SHA1_Hash(byte[] array)
        {
            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
            {
                return sha1.ComputeHash(array);
            }
        }

        public static byte[] Get_SHA1_Hash(FileStream stream)
        {
            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
            {
                return sha1.ComputeHash(stream);
            }
        }

        // Compares 2 hashes in bytearray form with each other
        // checks every possible unequality (can pass null or diffrent lengths)
        public static bool Comp_Hash(byte[] hash_0, byte[] hash_1)
        {
            bool same = true;
            if (hash_0 == null || hash_1 == null) return false;
            if (hash_0.Length != hash_1.Length) return false;

            for (int i = 0; i < hash_0.Length; i++)
                if (hash_0[i] != hash_1[i])
                {
                    same = false;
                    break;
                }

            return same;
        }
    }
}
