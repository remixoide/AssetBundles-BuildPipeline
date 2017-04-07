using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor.Build.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Cache
{
    public static class BuildCache
    {
        private const string kCachePath = "Library/BuildCache";

        private static string GetPathForCachedResults(Hash128 hash)
        {
            var file = hash.ToString();
            return string.Format("{0}/{1}/{2}/Results", kCachePath, file.Substring(0, 2), file);
        }

        private static string GetPathForCachedArtifacts(Hash128 hash)
        {
            var file = hash.ToString();
            return string.Format("{0}/{1}/{2}/Artifacts", kCachePath, file.Substring(0, 2), file);
        }

        public static bool TryLoadCachedResults<T>(Hash128 hash, out T results)
        {
            var path = GetPathForCachedResults(hash);
            var filePath = string.Format("{0}/{1}", path, typeof(T).Name);
            if (!File.Exists(filePath))
            {
                results = default(T);
                return false;
            }

            try
            {
                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    results = (T) formatter.Deserialize(stream);
                return true;
            }
            catch (Exception)
            {
                results = default(T);
                return false;
            }
        }

        public static bool TryLoadCachedArtifacts(Hash128 hash, out string[] artifactPaths, out string rootCachePath)
        {
            rootCachePath = GetPathForCachedArtifacts(hash);
            if (!Directory.Exists(rootCachePath))
            {
                artifactPaths = null;
                return false;
            }

            artifactPaths = Directory.GetFiles(rootCachePath, "*", SearchOption.AllDirectories);
            return true;
        }

        public static bool TryLoadCahcedResultsAndArtifacts<T>(Hash128 hash, out T results, out string[] artifactPaths, out string rootCachePath)
        {
            artifactPaths = null;
            rootCachePath = GetPathForCachedArtifacts(hash);
            if (!TryLoadCachedResults(hash, out results))
                return false;

            return TryLoadCachedArtifacts(hash, out artifactPaths, out rootCachePath);
        }

        public static bool SaveCachedResults<T>(Hash128 hash, T results)
        {
            var path = GetPathForCachedResults(hash);
            var filePath = string.Format("{0}/{1}", path, typeof(T).Name);

            try
            {
                Directory.CreateDirectory(path);
                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                    formatter.Serialize(stream, results);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public static bool SaveCachedArtifacts(Hash128 hash, string[] artifactPaths, string rootPath)
        {
            var path = GetPathForCachedArtifacts(hash);

            try
            {
                Directory.CreateDirectory(path);
                foreach (var artifact in artifactPaths)
                {
                    var file = new FileInfo(string.Format("{0}/{1}", rootPath, artifact));
                    if (file.Exists)
                        file.CopyTo(string.Format("{0}/{1}", path, artifact), true);
                    else
                        BuildLogger.LogWarning("Unable to find source file '{0}' to add to the build cache.", artifact);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public static bool SaveCachedResultsAndArtifacts<T>(Hash128 hash, T results, string[] artifactPaths, string rootPath)
        {
            if (!SaveCachedResults(hash, results))
                return false;
            return SaveCachedArtifacts(hash, artifactPaths, rootPath);
        }
    }
}
