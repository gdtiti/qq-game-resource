using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace QQGameRes
{
    public class DirectorySearcher
    {

    }

    public class DirectorySearcherProgress
    {
        public DirectoryInfo CurrentDirectory { get; set; }
    }

    /// <summary>
    /// Represents a collection of QQ game resource files arranged in
    /// a hierarchy.
    /// </summary>
    public class Repository
    {
        /// <summary>
        /// Gets the full path of the installation directory of QQ games.
        /// </summary>
        /// <returns>Full path of the installation directory of QQ games,
        /// or <code>null</code> if this cannot be determined.</returns>
        public static string GetInstallationPath()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Tencent\QQGame\SYS"))
            {
                if (key != null)
                    return key.GetValue("GameDirectory") as string;
                else
                    return null;
            }
        }

        private List<FileGroup> imageFolders = new List<FileGroup>();
        private List<FileInfo> packageFiles = new List<FileInfo>();

        /// <summary>
        /// Creates a Repository object.
        /// </summary>
        public Repository()
        {
        }

#if false
        public Repository(string path)
        {
            // Search the directory recursively for supported files.
            imageFolders = new List<FileGroup>();
            packageFiles = new List<FileInfo>();
            SearchForSupportedFiles(new DirectoryInfo(path));
        }
#endif

        /// <summary>
        /// Searches the given directory recursively for supported files.
        /// </summary>
        /// <param name="path">Path to the directory to search for supported
        /// files.</param>
        /// <param name="ct">Token used to request cancellation of the task.
        /// </param>
        /// <returns>A <code>Task</code> that contains the actual search work.
        /// </returns>
        public Task LoadDirectoryAsync(
            string path, DirectorySearcherProgress progress, CancellationToken ct)
        {
            return Task.Factory.StartNew(
                () => SearchForSupportedFiles(new DirectoryInfo(path), progress, ct),
                ct);
        }

        // TODO: we should lock the fields.
        private void SearchForSupportedFiles(
            DirectoryInfo dir, DirectorySearcherProgress progress, CancellationToken ct)
        {
            progress.CurrentDirectory = dir;
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
            if (ct.IsCancellationRequested)
                return;

            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                if (ct.IsCancellationRequested)
                    return;
                SearchForSupportedFiles(d, progress, ct);
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
