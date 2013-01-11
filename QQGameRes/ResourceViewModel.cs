using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using Util.Forms;

namespace QQGameRes
{
    /// <summary>
    /// Encapsulates a PKG archive as a VirtualFolder.
    /// </summary>
    class PackageFolder : IVirtualItem, IVirtualFolder, IExtractIcon
    {
        private QQGame.PkgArchive archive;

        public PackageFolder(QQGame.PkgArchive ar)
        {
            this.archive = ar;
        }

        public QQGame.PkgArchive Archive
        {
            get { return archive; }
        }

        public IEnumerable<IVirtualItem> EnumerateItems(VirtualItemType type)
        {
            yield break;
        }

        public string Name
        {
            get { return "Not Used"; }
        }

        public string FullName
        {
            get { return "pkg://" + archive.FileName; }
        }

        public string DisplayName
        {
            get { return Path.GetFileName(archive.FileName); }
        }

        public string GetIconKey(ExtractIconType type, Size desiredSize)
        {
            return "Package_Icon_16";
        }

        public Image ExtractIcon(ExtractIconType type, Size desiredSize)
        {
            return (Image)smallIcon.Clone();
        }

        private static Image smallIcon = Properties.Resources.Package_Icon_16;
    }

    /// <summary>
    /// Encapsulates a QQGame repository as a VirtualFolder.
    /// </summary>
    class RepositoryFolder : IVirtualItem, IVirtualFolder, IExtractIcon, INotifyFolderChanged
    {
        private DirectoryInfo reposDir;
        private List<ImageFolder> imageFolders;

        public RepositoryFolder(DirectoryInfo dir)
        {
            this.reposDir = dir;
            this.imageFolders = new List<ImageFolder>();
        }

        public void AddImageDirectory(DirectoryInfo dir)
        {
            ImageFolder imageFolder = new ImageFolder(dir, reposDir);
            imageFolders.Add(imageFolder);
            if (FolderChanged != null)
            {
                FolderChangedEventArgs e = new FolderChangedEventArgs();
                e.ChangeType = FolderChangeType.ItemsAdded;
                e.Item = imageFolder;
                FolderChanged(this, e);
            }
        }

        IEnumerable<IVirtualItem> IVirtualFolder.EnumerateItems(VirtualItemType type)
        {
            return imageFolders;
        }

        string IVirtualItem.Name
        {
            get { return "Not Used"; }
        }

        string IVirtualItem.FullName
        {
            get { return "repository://" + reposDir.FullName; }
        }

        string IVirtualItem.DisplayName
        {
            get { return reposDir.FullName.TrimEnd('\\'); }
        }

        string IExtractIcon.GetIconKey(ExtractIconType type, Size desiredSize)
        {
            return "Folder_Icon_16";
        }

        Image IExtractIcon.ExtractIcon(ExtractIconType type, Size desiredSize)
        {
            return (Image)smallIcon.Clone();
        }

        private static Image smallIcon = Properties.Resources.Folder_Icon_16;

        public event EventHandler<FolderChangedEventArgs> FolderChanged;
    }

    /// <summary>
    /// Encapsulates a directory of image files as a VirtualFolder.
    /// </summary>
    class ImageFolder : IVirtualItem, IVirtualFolder, IExtractIcon
    {
        private DirectoryInfo thisDir;
        private DirectoryInfo rootDir;

        public ImageFolder(DirectoryInfo thisDir, DirectoryInfo rootDir)
        {
            this.thisDir = thisDir;
            this.rootDir = rootDir;
        }

        public DirectoryInfo Directory { get { return thisDir; } }

        string IVirtualItem.Name
        {
            get { return "Not Used"; }
        }

        string IVirtualItem.FullName
        {
            get { return "ImageFolder://" + thisDir.FullName; }
        }

        string IVirtualItem.DisplayName
        {
            get
            {
                string name = thisDir.FullName;
                string root = rootDir.FullName;
                if (name.Length <= root.Length)
                    return "(root)";
                else
                    return name.Substring(root.Length).TrimEnd('\\');
            }
        }

        IEnumerable<IVirtualItem> IVirtualFolder.EnumerateItems(VirtualItemType type)
        {
            yield break;
        }

        string IExtractIcon.GetIconKey(ExtractIconType type, Size desiredSize)
        {
            return "Images_Icon_16";
        }

        Image IExtractIcon.ExtractIcon(ExtractIconType type, Size desiredSize)
        {
            return (Image)smallIcon.Clone();
        }

        private static Image smallIcon = Properties.Resources.Images_Icon_16;
    }

}
