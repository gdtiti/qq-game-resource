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
        private PackageItem[] children = null;

        public PackageFolder(QQGame.PkgArchive ar)
        {
            this.archive = ar;
        }

        public QQGame.PkgArchive Archive
        {
            get { return archive; }
        }

        string IVirtualItem.Name
        {
            get { return "Not Used"; }
        }

#if false
        string IVirtualItem.FullName
        {
            get { return "pkg://" + archive.FileName; }
        }
#endif

        string IVirtualItem.DisplayName
        {
            get { return Path.GetFileName(archive.FileName); }
        }

        IEnumerable<IVirtualItem> IVirtualFolder.EnumerateItems(VirtualItemType type)
        {
            if (children == null)
            {
                children = (from ent in archive.Entries
                            select new PackageItem(ent)
                           ).ToArray();
            }
            return children;
        }

        string IExtractIcon.GetIconKey(ExtractIconType type, Size desiredSize)
        {
            return "Package_Icon_16";
        }

        Image IExtractIcon.ExtractIcon(ExtractIconType type, Size desiredSize)
        {
            return (Image)smallIcon.Clone();
        }

        private static Image smallIcon = Properties.Resources.Package_Icon_16;
    }
    
    /// <summary>
    /// Encapsulates <code>QQGame.PkgArchiveEntry</code> as a virtual item.
    /// </summary>
    class PackageItem : IVirtualItem, IVirtualFile
    {
        private QQGame.PkgArchiveEntry entry;

        public PackageItem(QQGame.PkgArchiveEntry ent)
        {
            entry = ent;
        }

        string IVirtualItem.Name
        {
            get { return entry.FullName; }
        }

        string IVirtualItem.DisplayName
        {
            get { return Path.GetFileName(entry.FullName); }
        }

        Stream IVirtualFile.Open(FileMode mode, FileAccess access, FileShare share)
        {
            if ((mode == FileMode.Open || mode == FileMode.OpenOrCreate)
                && access == FileAccess.Read)
                return entry.Open();
            throw new NotSupportedException();
        }
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

        public void AddImageDirectory(DirectoryInfo dir, FileInfo[] files)
        {
            ImageFolder imageFolder = new ImageFolder(dir, reposDir, files);
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
            if ((type & VirtualItemType.Folder) != 0)
                return imageFolders;
            else
                return null;
        }

        string IVirtualItem.Name
        {
            get { return "Not Used"; }
        }

#if false
        string IVirtualItem.FullName
        {
            get { return "repository://" + reposDir.FullName; }
        }
#endif

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
        private ImageFile[] files;

        public ImageFolder(
            DirectoryInfo thisDir, DirectoryInfo rootDir, FileInfo[] imageFiles)
        {
            this.thisDir = thisDir;
            this.rootDir = rootDir;
            this.files = imageFiles.Select(x => new ImageFile(x)).ToArray();
        }

        public DirectoryInfo Directory { get { return thisDir; } }

        string IVirtualItem.Name
        {
            get { return "Not Used"; }
        }

#if false
        string IVirtualItem.FullName
        {
            get { return "ImageFolder://" + thisDir.FullName; }
        }
#endif

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
            return files;
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

    class ImageFile : PhysicalFile, IExtractIcon
    {
        public ImageFile(FileInfo file) : base(file) { }

        string IExtractIcon.GetIconKey(ExtractIconType type, Size desiredSize)
        {
            if (type == ExtractIconType.Thumbnail)
            {
                return base.File.FullName;
            }
            else
            {
                return null;
            }
        }

        Image IExtractIcon.ExtractIcon(ExtractIconType type, Size desiredSize)
        {
            if (type == ExtractIconType.Thumbnail)
            {
                // Do we support this extension?
                string ext = base.File.Extension.ToLowerInvariant();
                if (ext == ".mif")
                {
                    using (Stream stream = base.File.OpenRead())
                    using (QQGame.MifImageDecoder mif = new QQGame.MifImageDecoder(stream))
                    {
                        // TODO: should we dispose the original image???
                        Image img = mif.DecodeFrame().Image;
                        return ResizeImage(img, desiredSize);
                    }
                }
                else if (ext == ".bmp")
                {
                    using (Stream stream = base.File.OpenRead())
                    using (Bitmap bmp = new Bitmap(stream))
                    {
                        // Make a copy of the bitmap, because MSDN says "You must
                        // keep the stream open for the lifetime of the Bitmap."
                        return (Image)bmp.Clone();
                    }
                }
            }
            return null;
        }

        private static Bitmap ResizeImage(Image image, Size size)
        {
            // Fit the source image into the target image, keeping scale.
            Rectangle bounds = new Rectangle(0, 0, size.Width, size.Height);
            Size sz = image.Size;
            if (sz.Width > bounds.Width)
            {
                sz.Height = sz.Height * bounds.Width / sz.Width;
                sz.Width = bounds.Width;
            }
            if (sz.Height > bounds.Height)
            {
                sz.Width = sz.Width * bounds.Height / sz.Height;
                sz.Height = bounds.Height;
            }
            bounds.X += (bounds.Width - sz.Width) / 2;
            bounds.Y += (bounds.Height - sz.Height) / 2;
            bounds.Width = sz.Width;
            bounds.Height = sz.Height;

            // Create a new bitmap of the desired size.
            Bitmap newBitmap = new Bitmap(size.Width, size.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.DrawImage(image, bounds);
            }
            return newBitmap;
        }
    }
}
