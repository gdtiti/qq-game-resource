using System;
using Util.Forms;

namespace QQGameRes
{
    /// <summary>
    /// Stores information associated with an item in ResourceListView.
    /// </summary>
    public class ResourceListViewEntry
    {
        /// <summary>
        /// The resource entry associated with the item.
        /// </summary>
        public IVirtualItem ResourceEntry;

        /// <summary>
        /// The thumbnail of the associated resource. If the resource is
        /// a multi-frame image, this is the first frame. If the thumbnail
        /// has not been loaded or the resource is not a supported image, 
        /// this is <code>null</code>.
        /// </summary>
        public System.Drawing.Image Thumbnail;

        /// <summary>
        /// If the associated resource is an animatable image, this is the
        /// number of frames in the animation. If the associated resource is
        /// a static image, this should be set to 1. If the associated 
        /// resource is not a supported image, this should be set to 0.
        /// </summary>
        public int FrameCount;
    }
}
