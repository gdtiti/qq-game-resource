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
    class ListItemInfo
    {
        /// <summary>
        /// The resource entry associated with the list item.
        /// </summary>
        public ResourceEntry ResourceEntry;

        /// <summary>
        /// The thumbnail of the list item, or <code>null</code> if not 
        /// available.
        /// </summary>
        public Image Thumbnail;

        /// <summary>
        /// If the list item supports animation, returns the number of frames
        /// in the animation. Otherwise, this should be set to 1.
        /// </summary>
        public int FrameCount;
    }
}
