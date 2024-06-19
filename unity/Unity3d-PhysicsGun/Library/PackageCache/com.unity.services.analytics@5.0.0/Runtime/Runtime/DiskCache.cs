using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using UnityEngine;

namespace Unity.Services.Analytics.Internal
{
    internal interface IDiskCache
    {
        /// <summary>
        /// Deletes the cache file (if one exists).
        /// </summary>
        void Clear();

        /// <summary>
        /// Compiles the provided list of indices and payload buffer into a binary file and saves it to disk.
        /// </summary>
        /// <param name="eventEndIndices">A list of array indices marking where in the payload each event ends</param>
        /// <param name="payload">The raw UTF8 byte stream of event data</param>
        void Write(List<int> eventEndIndices, Stream payload);

        /// <summary>
        /// Clears and overwrites the contents of the provided list and buffer.
        /// </summary>
        /// <param name="eventEndIndices"></param>
        /// <param name="buffer"></param>
        bool Read(List<int> eventEndIndices, Stream buffer);
    }

    internal interface IFileSystemCalls
    {
        bool CanAccessFileSystem();
        bool FileExists(string path);
        void DeleteFile(string path);

        Stream OpenFileForWriting(string path);
        Stream OpenFileForReading(string path);
    }

    internal class FileSystemCalls : IFileSystemCalls
    {
        public bool CanAccessFileSystem()
        {
            // Switch requires a specific setup to have write access to the disc so it won't be handled here.
            return
                Application.platform != RuntimePlatform.Switch &&
#if !UNITY_2021_1_OR_NEWER
                Application.platform != RuntimePlatform.XboxOne &&
#endif
#if UNITY_2019 || UNITY_2020_2_OR_NEWER
                Application.platform != RuntimePlatform.GameCoreXboxOne &&
                Application.platform != RuntimePlatform.GameCoreXboxSeries &&
                Application.platform != RuntimePlatform.PS5 &&
#endif
                Application.platform != RuntimePlatform.PS4 &&
                !String.IsNullOrEmpty(Application.persistentDataPath);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public Stream OpenFileForWriting(string path)
        {
            // NOTE: FileMode.Create either makes a new file OR blats the existing one.
            // This is the desired behaviour.
            // See https://learn.microsoft.com/en-us/dotnet/api/system.io.filemode
            return new FileStream(path, FileMode.Create);
        }

        public Stream OpenFileForReading(string path)
        {
            // NOTE: FileMode.Open will throw an exception if the file does not exist.
            // So ensure the Exists check is always done before getting near here.
            // See https://learn.microsoft.com/en-us/dotnet/api/system.io.filemode
            return new FileStream(path, FileMode.Open);
        }
    }

    internal class DiskCache : IDiskCache
    {
        internal const string k_FileHeaderString = "UGSEventCache";
        internal const int k_CacheFileVersionOne = 1;

        private readonly string k_CacheFilePath;
        private readonly IFileSystemCalls k_SystemCalls;
        private readonly long k_CacheFileMaximumSize;

        internal DiskCache(IFileSystemCalls systemCalls)
        {
            if (systemCalls.CanAccessFileSystem())
            {
                // NOTE: On console platforms where file system access is restricted, even asking for the persistentDataPath
                // can cause an exception, let alone trying to write to it. Be careful!

                // NOTE: Since we now have some defence against trying to read files that don't match the new file format,
                // we are safe to keep reusing the same file path. We will simply ignore and delete/overwrite the cache
                // from older SDK versions.
                k_CacheFilePath = $"{Application.persistentDataPath}/eventcache";
            }

            k_SystemCalls = systemCalls;
            k_CacheFileMaximumSize = 5 * 1024 * 1024; // 5MB, 1024B * 1024KB * 5
        }

        internal DiskCache(string cacheFilePath, IFileSystemCalls systemCalls, long maximumFileSize)
        {
            k_CacheFilePath = cacheFilePath;
            k_SystemCalls = systemCalls;
            k_CacheFileMaximumSize = maximumFileSize;
        }

        public void Write(List<int> eventEndIndices, Stream payload)
        {
            if (eventEndIndices.Count > 0 &&
                k_SystemCalls.CanAccessFileSystem())
            {
                // Tick through eventEnds until you find the highest one that is still under the file size limit
                int cacheEnd = 0;
                int cacheEventCount = 0;
                for (int e = 0; e < eventEndIndices.Count; e++)
                {
                    if (eventEndIndices[e] < k_CacheFileMaximumSize)
                    {
                        cacheEnd = eventEndIndices[e];
                        cacheEventCount = e + 1;
                    }
                }

                using (Stream file = k_SystemCalls.OpenFileForWriting(k_CacheFilePath))
                {
                    using (var writer = new BinaryWriter(file))
                    {
                        writer.Write(k_FileHeaderString);       // a specific string to signal file format validity
                        writer.Write(k_CacheFileVersionOne);    // int version specifier
                        writer.Write(cacheEventCount);          // int event count (cropped to maximum file size)
                        for (int i = 0; i < cacheEventCount; i++)
                        {
                            writer.Write(eventEndIndices[i]);   // int event end index
                        }

                        long payloadOriginalPosition = payload.Position;
                        payload.Position = 0;
                        for (int i = 0; i < cacheEnd; i++)
                        {
                            // NOTE: the cast to byte is important -- ReadByte actually returns an int, which is 4 bytes.
                            // So you get 3 extra bytes of 0 added if you take it verbatim. Casting to byte cuts it back down to size.
                            writer.Write((byte)payload.ReadByte());   // byte[] event data
                        }
                        payload.Position = payloadOriginalPosition;
                    }
                }
            }
        }

        public void Clear()
        {
            if (k_SystemCalls.CanAccessFileSystem() &&
                k_SystemCalls.FileExists(k_CacheFilePath))
            {
                k_SystemCalls.DeleteFile(k_CacheFilePath);
            }
        }

        public bool Read(List<int> eventEndIndices, Stream buffer)
        {
            if (k_SystemCalls.CanAccessFileSystem() &&
                k_SystemCalls.FileExists(k_CacheFilePath))
            {
#if UNITY_ANALYTICS_EVENT_LOGS
                Debug.Log("Reading cached events: " + k_CacheFilePath);
#endif
                using (Stream file = k_SystemCalls.OpenFileForReading(k_CacheFilePath))
                {
                    using (var reader = new BinaryReader(file))
                    {
                        try
                        {
                            string header = reader.ReadString();
                            if (header == k_FileHeaderString)
                            {
                                int version = reader.ReadInt32();
                                switch (version)
                                {
                                    case k_CacheFileVersionOne:
                                        ReadVersionOneCacheFile(eventEndIndices, reader, buffer);
                                        return true;
                                    default:
                                        Debug.LogWarning($"Unable to read event cache file: unknown file format version {version}");
                                        Clear();
                                        break;
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"Unable to read event cache file: corrupt");
                                Clear();
                            }
                        }
                        catch (Exception)
                        {
                            Debug.LogWarning($"Unable to read event cache file: corrupt");
                            Clear();
                        }
                    }
                }
            }

            return false;
        }

        private void ReadVersionOneCacheFile(in List<int> eventEndIndices, BinaryReader reader, in Stream buffer)
        {
            int eventCount = reader.ReadInt32();            // int32 event count
            for (int i = 0; i < eventCount; i++)
            {
                int eventEndIndex = reader.ReadInt32();     // int32 event end index
                eventEndIndices.Add(eventEndIndex);
            }

            buffer.SetLength(0);
            buffer.Position = 0;
            reader.BaseStream.CopyTo(buffer);               // byte[] event data is the rest of the file
        }
    }
}
