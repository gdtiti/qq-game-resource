// Copyright (c) 2013 fancidev
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using Util.Forms;
using Util.Media;

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

        object IExtractIcon.ExtractIcon(ExtractIconType type, Size desiredSize)
        {
            return smallIcon.Clone();
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
            if (type.HasFlag(VirtualItemType.Folder))
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

        object IExtractIcon.ExtractIcon(ExtractIconType type, Size desiredSize)
        {
            return smallIcon.Clone();
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

        public ImageFile[] Files { get { return files; } }

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
#if false
            // The following code is to test the VirtualFolderListView.
            foreach (var file in files)
            {
                yield return file;
                System.Threading.Thread.Sleep(100);
            }
#else
            if (type.HasFlag(VirtualItemType.NonFolder))
                return files;
            else
                return null;
#endif
        }

        string IExtractIcon.GetIconKey(ExtractIconType type, Size desiredSize)
        {
            return "Images_Icon_16";
        }

        object IExtractIcon.ExtractIcon(ExtractIconType type, Size desiredSize)
        {
            return smallIcon.Clone();
        }

        private static Image smallIcon = Properties.Resources.Images_Icon_16;
    }

    class ImageFile : PhysicalFile, IExtractIcon, IDisposable
    {
        // private MultiFrameImage cachedImage = null; // cached item

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

        object IExtractIcon.ExtractIcon(ExtractIconType type, Size desiredSize)
        {
            if (type == ExtractIconType.Thumbnail)
            {
                //// Have we already extracted the image?
                //if (cachedImage != null)
                //    return cachedImage;

                // Do we support this extension?
                string ext = base.File.Extension.ToLowerInvariant();
                if (ext == ".mif")
                {
#if false
                    using (Stream stream = base.File.OpenRead())
                    using (QQGame.MifImageDecoder mif = new QQGame.MifImageDecoder(stream))
                    {
                        // TODO: should we dispose the original image???
                        Image img = mif.DecodeFrame().Image;
                        return (Image)img.Clone();
                    }
#else
                    Stream stream = base.File.OpenRead();
                    return new QQGame.MifImage(stream);
#endif
                }
                else if (ext == ".bmp")
                {
                    using (Stream stream = base.File.OpenRead())
                    using (Bitmap bmp = new Bitmap(stream))
                    {
                        // Make a copy of the bitmap, because MSDN says "You must
                        // keep the stream open for the lifetime of the Bitmap."
                        return bmp.Clone();
                    }
                }
            }
            return null;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
