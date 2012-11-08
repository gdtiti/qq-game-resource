using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;

namespace QQGameRes
{
    public class Repository
    {
        public static string GetInstallationPath()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Tencent\QQGame\SYS"))
            {
                if (key != null)
                {
                    object val = key.GetValue("GameDirectory");
                    if (val != null && val is string)
                        return (string)val;
                }
            }
            return null;
        }

        List<FileGroup> imageFolders;
        List<FileInfo> packageFiles;

        public Repository(string path)
        {
            // Search the directory recursively for supported files.
            imageFolders = new List<FileGroup>();
            packageFiles = new List<FileInfo>();
            SearchForSupportedFiles(new DirectoryInfo(path));
        }

        private void SearchForSupportedFiles(DirectoryInfo dir)
        {
            List<FileInfo> selected = new List<FileInfo>();
            foreach (FileInfo f in dir.GetFiles())
            {
                string ext = f.Extension.ToLowerInvariant();
                if (ext == ".mif")
                    selected.Add(f);
                //else if (ext == ".bmp") // too many trivial clip arts
                //    selected.Add(f);
                else if (ext == ".pkg")
                    packageFiles.Add(f);
            }
            if (selected.Count > 0)
            {
                imageFolders.Add(new FileGroup(dir, selected.ToArray()));
            }

            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                SearchForSupportedFiles(d);
            }
        }

        public IEnumerable<FileGroup> ImageFolders
        {
            get { return imageFolders; }
        }

        public IEnumerable<FileInfo> PackageFiles
        {
            get { return packageFiles; }
        }
    }
}
