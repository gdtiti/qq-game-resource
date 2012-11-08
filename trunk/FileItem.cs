using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace QQGameRes
{
    /// <summary>
    /// Represents a physical file (usually a MIF image) to be previewed.
    /// </summary>
    class FileItem
    {
        public FileInfo FileInfo;
        public Image Thumbnail;
        public bool MultiFrame;
    }
}
