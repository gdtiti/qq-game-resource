using System;
using System.IO;
using System.IO.Compression;

namespace QQGameRes
{
    public class PackageEntry : ResourceEntry
    {
        public string PackagePath;
        public string EntryPath;
        public uint EntryOffset;
        public uint EntrySize;
        public uint OriginalSize;

        public string Name
        {
            get { return PackagePath + "\\" + EntryPath; }
        }

        public int Size
        {
            get { return (int)OriginalSize; }
        }

        public Stream Open()
        {
            // Open the package file.
            FileStream stream = new FileStream(PackagePath, FileMode.Open, FileAccess.Read);

            // Seek to the entry location and skip two bytes of ZLIB header
            // which is not recognized by .NET's DeflateStream implementation.
            stream.Seek(EntryOffset + 2, SeekOrigin.Begin);

            // Create and return a DeflateStream from here.
            return new DeflateStream(stream, CompressionMode.Decompress);
        }
    }
}
