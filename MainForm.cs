using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QQGameRes
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        
        private void LoadPackage(string filename)
        {
            Package pkg = new Package(filename);
            TreeNode node = new TreeNode();
            node.Text = Path.GetFileName(filename);
            node.ImageIndex = 1;
            node.SelectedImageIndex = 1;
            node.Tag = pkg;
            tvFolders.Nodes.Add(node);
        }

        private ListViewItem animatedItem;
        private MifImage animatedImage;
        private bool animationEnded;
        private const int AnimationEndDelay = 500;

        /// <summary>
        /// Stops the current animation (if any) and schedules the ListView
        /// item being animated for redrawal by invalidating it.
        /// </summary>
        private void StopAnimation()
        {
            timerAnimation.Stop();
            if (animatedItem != null)
            {
                if (animatedItem.Index >= 0) // still in use
                    lvEntries.RedrawItems(animatedItem.Index, animatedItem.Index, true);
                animatedItem = null;
            }
            if (animatedImage != null)
            {
                animatedImage.Dispose();
                animatedImage = null;
            }
        }

        private void PlayNextFrame()
        {
            // If the currently selected list item is not being animated,
            // reset the timer and exit.
            if (animatedItem == null || animatedImage == null ||
                !animatedItem.Selected || animatedItem.Index == -1)
            {
                StopAnimation();
                return;
            }

            // Try load the next frame in the image being animated. If there
            // are no more frames, wait for 500 milliseconds and then reset
            // the timer and display the thumbnail image.
            if (!animatedImage.GetNextFrame())
            {
                if (animationEnded)
                {
                    StopAnimation();
                }
                else
                {
                    animationEnded = true;
                    timerAnimation.Interval = AnimationEndDelay;
                    timerAnimation.Start();
                }
                return;
            }

            // Redraw the frame and set the next timer interval.
            lvEntries.RedrawItems(animatedItem.Index, animatedItem.Index, true);
            timerAnimation.Interval = Math.Max(animatedImage.CurrentFrame.Delay, 25);
            timerAnimation.Start();
        }

        private void lvEntries_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Stop the current animation if any.
            StopAnimation();

            // Update button state.
            btnExport.Enabled = (lvEntries.SelectedIndices.Count > 0);

            // Do nothing if no item is selected.
            if (lvEntries.SelectedIndices.Count == 0)
                return;

            // Get the item selected.
            ListViewItem item = lvEntries.SelectedItems[0];
            string filename = item.Text;
            ListViewItemTag tag = item.Tag as ListViewItemTag;

            // Update status message of the image size.
            if (tag.Thumbnail != null)
            {
                txtImageSize.Text = tag.Thumbnail.Width + " x " + tag.Thumbnail.Height;
                txtFrames.Text = tag.FrameCount + " Frames";
            }

            // Start animation if this item is a multi-frame image.
            if (tag.FrameCount > 1)
            {
                ResourceEntry entry = tag.ResourceEntry;
                animatedItem = item;
                animatedImage = new MifImage(entry.Open());
                animationEnded = false;
                PlayNextFrame();
            }
        }

        private void LoadRepository(string path)
        {
            if (path.EndsWith("/") || path.EndsWith("\\"))
                path = path.Substring(0, path.Length - 1);
            Repository rep = new Repository(path);

            // Hide and clear the tree view to reduce UI glitter.
            tvFolders.Visible = false;
            tvFolders.Nodes.Clear();

            // Create a root-level node for the repository.
            TreeNode root = tvFolders.Nodes.Add(path);

            // Create a child node for each image folder in the repository.
            foreach (FileGroup group in rep.ImageFolders)
            {
                string name = group.Name;
                if (name.Length <= path.Length + 1)
                    name = "(root)";
                else
                    name = name.Substring(path.Length + 1);

                TreeNode node = new TreeNode();
                node.Text = name;
                node.ImageIndex = 2;
                node.SelectedImageIndex = 2;
                node.Tag = group;
                root.Nodes.Add(node);
            }

            // Create a root-level node for each .PKG package.
            foreach (FileInfo file in rep.PackageFiles)
            {
                LoadPackage(file.FullName);
            }

            // Expand the first root node and show the tree view.
            root.Expand();
            tvFolders.Visible = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            SetWindowTheme(tvFolders.Handle, "EXPLORER", null);
            SetWindowTheme(lvEntries.Handle, "EXPLORER", null);

            // Load the root path of QQ Game.
            string rootPath = Repository.GetInstallationPath();
            if (rootPath == null)
            {
                MessageBox.Show(this, "找不到 QQ 游戏的安装目录。请手动指定目录或资源包。",
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            LoadRepository(rootPath);
        }

        private void timerAnimation_Tick(object sender, EventArgs e)
        {
            PlayNextFrame();
        }

        private void MainForm_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string filename in filenames)
                    System.Diagnostics.Debug.WriteLine(filename);
            }
        }

        private void tvFolders_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is ResourceGroup)
            {
                StopAnimation();
                PopulateListView(e.Node.Tag as ResourceGroup);
            }
        }

        /// <summary>
        /// Populates the list view with entries from the given collection.
        /// After the function returns, the list view will be scrolled to
        /// the top, and no item will be selected.
        /// </summary>
        /// <param name="group"></param>
        private void PopulateListView(ResourceGroup group)
        {
            StopAnimation();
            thumbnailLoader.CancelPendingTasks();
            lvEntries.Items.Clear();
            lvEntries.SelectedIndices.Clear();

            lvEntries.Visible = false;
            foreach (ResourceEntry entry in group.Entries)
            {
                // We create the item with empty text. Otherwise if the actual
                // text is too long, the OwnerDraw bounds for a focused item
                // will be too big.
                ListViewItem item = new ListViewItem("");
                item.SubItems.Add(entry.Size.ToString("#,#"));
                ListViewItemTag tag = new ListViewItemTag();
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
            // Sometimes, this handler gets called after some of the 
            // components (in particular, imageListPreview) have been 
            // disposed. This can cause a runtime error as the drawing
            // routine attempts to access the image list.
            if (e.Item.ImageList == null)
                return;

            if (!(e.Item.Tag is ListViewItemTag))
            {
                e.DrawDefault = true;
                return;
            }
            ListViewItemTag tag = e.Item.Tag as ListViewItemTag;

            // Load the thumbnail image if not already loaded.
            LoadThumbnailAsync(e.Item);

            // Check if we're currently animating this item.
            bool animating = (animatedItem == e.Item);

            // If we are in the process of animation, draw the current frame.
            // Otherwise, draw the thumbnail image.
            Image img = animating ? animatedImage.CurrentFrame.Image : tag.Thumbnail;

            // If the thumbnail is still being loaded, display a waiting image.
            if (img == null)
                img = ThumbnailLoader.LoadingIcon;

            // Create a custom-drawing helper object.
            ListViewItemDrawer drawer = 
                new ListViewItemDrawer(e.Item, e.Bounds, e.Graphics);
#if DEBUG && false
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
            if (tag.FrameCount > 1 && !animating)
                drawer.DrawPlayIcon();
            
            // Draw the file name text.
            drawer.DrawText(Path.GetFileName(tag.ResourceEntry.Name));
        }

        private void btnOpenPackage_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                StopAnimation();
                thumbnailLoader.CancelPendingTasks();
                lvEntries.Items.Clear();
                tvFolders.Nodes.Clear();
                LoadPackage(openFileDialog1.FileName);
                tvFolders.SelectedNode = tvFolders.Nodes[0];
            }
        }

        private static string GetNumberedFileName(
            string filename, int number, int max)
        {
            string numberFormat = "0000000000".Substring(0, max.ToString().Length);
            string ext = Path.GetExtension(filename);
            return filename.Substring(0, filename.Length - ext.Length) +
                "-" + number.ToString(numberFormat) + ext;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (lvEntries.SelectedIndices.Count == 0)
                return;

            ListViewItem item = lvEntries.SelectedItems[0];
            ListViewItemTag tag = item.Tag as ListViewItemTag;
            string ext = Path.GetExtension(tag.ResourceEntry.Name).ToLowerInvariant();

            // If the selected item is an image, display additional format
            // conversion options in the save dialog.
            string filter = "原始格式|*" + ext;
            if (tag.Thumbnail != null && tag.Thumbnail != ThumbnailLoader.DefaultIcon)
            {
                filter += "|PNG 图片|*.png";
                filter += "|BMP 图片|*.bmp";
                filter += "|JPEG 图片|*.jpg";
                filter += "|TIFF 图片|*.tif";
            }
            saveFileDialog1.Filter = filter;
            saveFileDialog1.FilterIndex = 1;
            if (ext == ".mif")
            {
                saveFileDialog1.FilterIndex = 2;
            }
            saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(
                tag.ResourceEntry.Name);

            // Show the dialog.
            if (saveFileDialog1.ShowDialog(this) != DialogResult.OK)
                return;

            string filename = saveFileDialog1.FileName;

            // If the filter index is 1 (save as is), just copy the stream.
            if (saveFileDialog1.FilterIndex == 1)
            {
                using (Stream stream = tag.ResourceEntry.Open(),
                       output = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(output, 65536);
                }
                txtStatus.Text = "保存成功";
                return;
            }

            // Get the requested image format.
            int filterIndex = saveFileDialog1.FilterIndex;
            ImageFormat desiredFormat =
                (filterIndex == 2) ? ImageFormat.Png :
                (filterIndex == 3) ? ImageFormat.Bmp :
                (filterIndex == 4) ? ImageFormat.Jpeg :
                (filterIndex == 5) ? ImageFormat.Tiff : ImageFormat.Bmp;

            // If this is a single-frame image, convert and save it.
            if (tag.FrameCount <= 1)
            {
                tag.Thumbnail.Save(filename, desiredFormat);
                txtStatus.Text = "保存成功";
                return;
            }

            // Now for a multi-frame image, ask the user how they want to save it.
            DialogResult result = MessageBox.Show(this,
                "选中的图片包含 " + tag.FrameCount + " 帧。" +
                "是否将每一帧单独存为一个文件？\r\n" +
                "如果选择是，则各帧将分别保存为\r\n    " +
                GetNumberedFileName(Path.GetFileName(filename),
                                    1, tag.FrameCount) + "\r\n" +
                "    ......\r\n    " +
                GetNumberedFileName(Path.GetFileName(filename),
                                    tag.FrameCount, tag.FrameCount) + "\r\n" +
                "如果选择否，则只保存第一帧到 " + Path.GetFileName(filename) +
                "。", this.Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            // Do nothing if the user is confused and canceled the action.
            if (result == DialogResult.Cancel)
                return;

            // If the user clicked "No", then we only save the first frame,
            // which is just the thumbnail.
            if (result == DialogResult.No)
            {
                tag.Thumbnail.Save(filename, desiredFormat);
                txtStatus.Text = "保存成功";
                return;
            }

            // Now the user clicked "Yes", so we need to save each frame
            // in an individual file.
            using (MifImage img = new MifImage(tag.ResourceEntry.Open()))
            {
                for (int i = 1; i <= tag.FrameCount; i++)
                {
                    if (!img.GetNextFrame())
                        break;
                    FileInfo file = new FileInfo(
                        GetNumberedFileName(filename, i, tag.FrameCount));
                    if (file.Exists)
                    {
                        if (MessageBox.Show(this, "文件 " + file.FullName +
                            " 已经存在。是否要覆盖？", "保存素材",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Exclamation) != DialogResult.Yes)
                            return;
                    }
                    img.CurrentFrame.Image.Save(file.FullName);
                }
            }
            txtStatus.Text = "保存成功";
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            FileVersionInfo ver = FileVersionInfo.GetVersionInfo(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            MessageBox.Show(this, this.Text + "\r\n" + 
                "版本 " + ver.ProductVersion, 
                "版本信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, String pszSubAppName, String pszSubIdList);

        ThumbnailLoader thumbnailLoader = new ThumbnailLoader();

        /// <summary>
        /// Loads the thumbnail image for the given ListViewItem in the 
        /// background if not already loaded.
        /// </summary>
        private void LoadThumbnailAsync(ListViewItem item)
        {
             ListViewItemTag tag = item.Tag as ListViewItemTag;
            if (tag.Thumbnail != null) // already loaded
                return;

            // Create a task for the thumbnailWorker.
            thumbnailLoader.AddTask(item);
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                StopAnimation();
                thumbnailLoader.CancelPendingTasks();
                lvEntries.Items.Clear();
                tvFolders.Nodes.Clear();
                LoadRepository(folderBrowserDialog1.SelectedPath);
            }
        }
    }
}
