using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace QQGameRes
{
    /// <summary>
    /// Represents a physical folder in the file system.
    /// </summary>
    public class FileFolder
    {
        DirectoryInfo dir;
        FileInfo[] files;

        public FileFolder(DirectoryInfo dir, FileInfo[] files)
        {
            this.dir = dir;
            this.files = files;
        }

        public FileInfo[] Files
        {
            get { return files; }
        }

#if false
        public FileFolder[] GetSubFolders()
        {
            if (subFolders == null)
            {
                DirectoryInfo[] subdirs = dir.GetDirectories();
                subFolders = new FileFolder[subdirs.Length];
                for (int i = 0; i < subdirs.Length; i++)
                {
                    subFolders[i] = new FileFolder(subdirs[i].FullName);
                }
            }
            return subFolders;
        }

        public FileInfo[] GetFiles()
        {
            return dir.GetFiles("*.mif");
        }
#endif
        public string Path
        {
            get { return dir.FullName; }
        }

        public string Name
        {
            get { return dir.Name; }
        }
    }
}
