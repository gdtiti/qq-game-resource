using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace QQGameRes
{
    public partial class ResourceListView : UserControl
    {
        public ResourceListView()
        {
            InitializeComponent();
        }

        private ResourceFolder _folder;

        /// <summary>
        /// Gets or sets the resource folder being displayed in this view.
        /// </summary>
        public ResourceFolder ResourceFolder
        {
            get { return _folder; }
            set
            {
                if (_folder != value)
                {
                    _folder = value;
                    PopulateListView();
                }
            }
        }

        /// <summary>
        /// Gets the active ResourceListViewEntry that has focus. If no entry 
        /// has focus, returns <code>null</code>.
        /// </summary>
        public ResourceListViewEntry ActiveEntry
        {
            get
            {
                if (lvEntries.SelectedIndices.Count == 0)
                    return null;
                else
                    return lvEntries.SelectedItems[0].Tag as ResourceListViewEntry;
            }
        }

        /// <summary>
        /// Indicates that the active resource entry has changed.
        /// </summary>
        public event EventHandler ActiveEntryChanged;

        /// <summary>
        /// Populates the view with entries from the current resource folder.
        /// After the function returns, the ListView will be scrolled to the
        /// top, and no item will be selected.
        /// </summary>
        private void PopulateListView()
        {
            //StopAnimation();
            thumbnailLoader.CancelPendingTasks();
            lvEntries.SelectedIndices.Clear();
            lvEntries.Items.Clear();

            lvEntries.Visible = false;
            foreach (ResourceEntry entry in _folder.Entries)
            {
                // We create each ListViewItem with empty text. Otherwise if 
                // the actual text is too long, the OwnerDraw bounds for a
                // focused item will be too big.
                ListViewItem item = new ListViewItem("");
                //item.SubItems.Add(entry.Size.ToString("#,#"));
                ResourceListViewEntry tag = new ResourceListViewEntry();
                tag.ResourceEntry = entry;
                item.Tag = tag;
                lvEntries.Items.Add(item);
            }
            lvEntries.Visible = true;
            if (lvEntries.Items.Count > 0)
                lvEntries.RedrawItems(0, lvEntries.Items.Count - 1, true);
        }

        private void lvEntries_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // This handler may get called after the imageListPreview
            // component has been disposed. This can cause a runtime error
            // when the drawing routine attempts to access the image list.
            if (e.Item.ImageList == null)
                return;

            // Get the data associated with the ListViewItem to paint.
            ResourceListViewEntry ent = e.Item.Tag as ResourceListViewEntry;

            // Load the thumbnail image if not already loaded.
            LoadThumbnailAsync(e.Item);

            // Check if we're currently animating this item.
            bool animating = (animator.Tag == e.Item);

            // If we are in the process of animation, draw the current frame.
            // Otherwise, draw the thumbnail image.
            Image img = animating ? animator.CurrentFrame.Image : ent.Thumbnail;

            // If the thumbnail is still being loaded, display a waiting image.
            if (img == null)
                img = ThumbnailLoader.LoadingIcon;

            // Create a custom-drawing helper object.
            ListViewItemDrawer drawer =
                new ListViewItemDrawer(e.Item, e.Bounds, e.Graphics);
#if false
            System.Diagnostics.Debug.WriteLine("ListViewItem Bounds: " +
                e.Bounds.Width + " x " + e.Bounds.Height);
#endif

            // Draw a focus rectangle if the item is selected.
            if (e.Item.Selected)
                drawer.DrawBorder(); // e.DrawFocusRectangle();

            // Draw the thumbnail or current frame.
            drawer.DrawImage(img);

            // If this is a multi-frame image, draw a Play icon to indicate
            // that, unless we are currently playing it.
            if (ent.FrameCount > 1 && !animating)
                drawer.DrawPlayIcon();

            // Draw the file name text.
            drawer.DrawText(Path.GetFileName(ent.ResourceEntry.Name));
        }

        /// <summary>
        /// Starts animating the currently selected ListViewItem.
        /// </summary>
        private void StartAnimation()
        {
            // Stop any existing animation.
            StopAnimation();

            // Get the ListViewItem selected.
            if (lvEntries.SelectedItems.Count == 0)
            {
                return;
            }
            ListViewItem item = lvEntries.SelectedItems[0];
            ResourceListViewEntry ent = item.Tag as ResourceListViewEntry;

            // Starts animation if this item is a multi-frame image.
            if (ent.FrameCount > 1)
            {
                Util.Media.ImageDecoder image = new MifImage(ent.ResourceEntry.Open());
                animator.StartAnimation(image, item);
            }
        }

        /// <summary>
        /// Stops the current animation if any.
        /// </summary>
        private void StopAnimation()
        {
            animator.StopAnimation();
        }

        private void animator_UpdateFrame(object sender, EventArgs e)
        {
            // Invalidate the ListViewItem being animated.
            ListViewItem item = animator.Tag as ListViewItem;
            lvEntries.RedrawItems(item.Index, item.Index, true);
        }

        private void animator_AnimationEnded(object sender, EventArgs e)
        {
            ListViewItem item = animator.Tag as ListViewItem;

            // Invalidate the ListViewItem where animation just finished.
            if (item.Index >= 0) // still in use
            {
                lvEntries.RedrawItems(item.Index, item.Index, true);
            }

            // Dispose the MifImage object used in the animation.
            (animator.Image as MifImage).Dispose();
        }

        private void lvEntries_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Start animating the selected item.
            StartAnimation();

            // Raise the ActiveEntryChanged event.
            if (ActiveEntryChanged != null)
                ActiveEntryChanged(this, null);
        }

        ThumbnailLoader thumbnailLoader = new ThumbnailLoader();

        /// <summary>
        /// Loads the thumbnail image for the given ListViewItem in the 
        /// background if not already loaded.
        /// </summary>
        private void LoadThumbnailAsync(ListViewItem item)
        {
            ResourceListViewEntry tag = item.Tag as ResourceListViewEntry;
            if (tag.Thumbnail != null) // already loaded
                return;

            // Create a task for the thumbnailWorker.
            thumbnailLoader.AddTask(item);
        }
    }
}
