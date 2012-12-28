using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace QQGameRes
{
#if false
    public struct PackageEntryInfo
    {
        public string Path;
        public uint Offset;
        public uint Size;
        public uint OriginalSize;
    }
#endif

    public class Package : ResourceFolder
    {
        private QQGame.PkgArchive ar;
        private string filename;
        private PackageEntry[] entries;

        // TODO: dispose ar when not used.
        public Package(string filename)
        {
            this.filename = filename;
            this.ar = new QQGame.PkgArchive(filename);
            this.entries = (from ent in ar.Entries
                            select new PackageEntry(ent)
                            {
                                PackagePath = filename,
                                EntryPath = ent.FullName,
                                EntrySize = ent.CompressedLength,
                                EntryOffset = 0,
                                OriginalSize = ent.Length
                            }).ToArray();
        }

        public string Name
        {
            get { return this.filename; }
        }

        public ResourceEntry[] Entries
        {
            get { return this.entries; }
        }
    }

    public class PackageEntry : ResourceEntry
    {
        private QQGame.PkgArchiveEntry entry;

        public PackageEntry(QQGame.PkgArchiveEntry ent)
        {
            entry = ent;
        }

        public string PackagePath;
        public string EntryPath;
        public int EntryOffset;
        public int EntrySize;
        public int OriginalSize;

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
            return entry.Open();
        }
    }
}
