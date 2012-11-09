﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace QQGameRes
{
    /// <summary>
    /// Represents a collection of physical files in the same directory.
    /// </summary>
    public class FileGroup : ResourceGroup
    {
        DirectoryInfo dir;
        FileEntry[] files;

        public FileGroup(DirectoryInfo dir, FileInfo[] files)
        {
            this.dir = dir;
            this.files = new FileEntry[files.Length];
            for (int i = 0; i < files.Length; i++)
                this.files[i] = new FileEntry(files[i]);
        }

        /// <summary>
        /// Gets the full path of the containing directory of the files.
        /// </summary>
        public string Name
        {
            get { return dir.FullName; }
        }

        /// <summary>
        /// Gets the files in this group.
        /// </summary>
        public ResourceEntry[] Entries
        {
            get { return files; }
        }
    }
}