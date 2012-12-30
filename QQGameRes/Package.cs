﻿using System;
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
                            select new PackageEntry(ent)).ToArray();
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

    /// <summary>
    /// Wraps a <code>PkgArchiveEntry</code> object in a ResourceEntry 
    /// interface.
    /// </summary>
    public class PackageEntry : ResourceEntry
    {
        private QQGame.PkgArchiveEntry entry;

        public PackageEntry(QQGame.PkgArchiveEntry ent)
        {
            entry = ent;
        }

        public string Name { get { return entry.FullName; } }

        public int Size { get { return entry.Length; } }

        public Stream Open() { return entry.Open(); }
    }
}