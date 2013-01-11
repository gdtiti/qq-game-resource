using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using Util.Forms;

namespace QQGameRes
{
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

        public string DisplayName
        {
            get { return Path.GetFileName(archive.FileName); }
        }

        public string GetIconKey(Size desiredSize)
        {
            return "Package_Icon_16";
        }

        public Image ExtractIcon(Size desiredSize)
        {
            return (Image)smallIcon.Clone();
        }

        private static Image smallIcon = Properties.Resources.Package_Icon_16;
    }
}
