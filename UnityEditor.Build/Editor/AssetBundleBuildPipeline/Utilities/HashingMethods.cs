using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using UnityEngine;

namespace UnityEditor.Build.Utilities
{
    public static class HashingMethods
    {
        public static Hash128 CalculateMD5Hash(object obj)
        {
            // TODO: Don't use boxing
            byte[] hash;
            var md5 = MD5.Create();
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);
                hash = md5.ComputeHash(stream.ToArray());
            }
            return new Hash128(hash[0], hash[1], hash[2], hash[3]);
        }

        public static Hash128 CalculateMD5Hash(params object[] objects)
        {
            // TODO: Don't use boxing
            byte[] hash;
            var md5 = MD5.Create();
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                foreach (var obj in objects)
                    formatter.Serialize(stream, obj);
                hash = md5.ComputeHash(stream.ToArray());
            }
            return new Hash128(hash[0], hash[1], hash[2], hash[3]);
        }
    }
}