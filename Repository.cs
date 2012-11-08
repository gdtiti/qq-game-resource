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
            RegistryKey key = 
                Registry.CurrentUser.OpenSubKey(@"Software\Tencent\QQGame\SYS");
            if (key != null)
            {
                object val = key.GetValue("GameDirectory");
                if (val != null && val is string)
                    return (string)val;
            }
            return null;
        }

        List<FileInfo> imgFiles;
        List<FileInfo> pkgFiles;

        public Repository(string path)
        {
            // Search the directory recursively for supported files.
            imgFiles = new List<FileInfo>();
            pkgFiles = new List<FileInfo>();
            SearchForSupportedFiles(new DirectoryInfo(path));
        }

        private void SearchForSupportedFiles(DirectoryInfo dir)
        {
            foreach (FileInfo f in dir.GetFiles())
            {
                string ext = f.Extension.ToLowerInvariant();
                if (ext == ".mif")
                    imgFiles.Add(f);
                else if (ext == ".pkg")
                    pkgFiles.Add(f);
            }

            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                SearchForSupportedFiles(d);
            }
        }

        public IEnumerable<FileInfo> ImageFiles
        {
            get { return imgFiles; }
        }

        public IEnumerable<FileInfo> PackageFiles
        {
            get { return pkgFiles; }
        }
    }
}
