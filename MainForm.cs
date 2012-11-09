﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QQGameRes
{
    public partial class MainForm : Form
    {
        private Package pkg;
        
        public MainForm()
        {
            InitializeComponent();
        }
        
        private void LoadPackage(string filename)
        {
            pkg = new Package(filename);
            lvEntries.Items.Clear();
            foreach (PackageEntry entry in pkg.Entries)
            {
                ListViewItem item = new ListViewItem(entry.EntryPath);
                item.SubItems.Add(entry.Size.ToString("#,#"));
                lvEntries.Items.Add(item);
            }
            this.Text = "QQ游戏资源浏览器 - " + Path.GetFileName(filename);
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
                Package pkg = new Package(file.FullName);
                string name = Path.GetFileName(file.Name);
                TreeNode node = new TreeNode();
                node.Text = name;
                node.ImageIndex = 1;
                node.SelectedImageIndex = 1;
                node.Tag = pkg;
                tvFolders.Nodes.Add(node);
            }

            // Expand the first root node and show the tree view.
            root.Expand();
            tvFolders.Visible = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            btnAnimate.Visible = false;
            toolStripSeparator3.Visible = false;

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

        private static Bitmap UnknownTypeIcon = Properties.Resources.Page_Icon_64;
        private static Bitmap LoadingImageIcon = Properties.Resources.Image_Icon_16;

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
            LoadThumbnail(e.Item);

            // Check if we're currently animating this item.
            bool animating = (animatedItem == e.Item);

            // If we are in the process of animation, draw the current frame.
            // Otherwise, draw the thumbnail image.
            Image img = animating ? animatedImage.CurrentFrame.Image : tag.Thumbnail;

            // If the thumbnail is still being loaded, display a waiting image.
            if (img == null)
                img = LoadingImageIcon;

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
            drawer.DrawText(tag.ResourceEntry.Name);
        }

        private void btnOpenPackage_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                LoadPackage(openFileDialog1.FileName);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (lvEntries.SelectedIndices.Count == 0)
                return;

            ListViewItem item = lvEntries.SelectedItems[0];
            ListViewItemTag tag = item.Tag as ListViewItemTag;

            saveFileDialog1.FileName = Path.GetFileName(item.Text);
            if (saveFileDialog1.ShowDialog(this) != DialogResult.OK)
                return;

            string filename = saveFileDialog1.FileName;
            using (Stream stream = tag.ResourceEntry.Open())
            {
                byte[] buffer = new byte[65536];
                using (FileStream output = new FileStream(
                    filename, FileMode.Create, FileAccess.Write))
                {
                    int n;
                    while ((n = stream.Read(buffer, 0, buffer.Length)) > 0)
                        output.Write(buffer, 0, n);
                }
            }
            MessageBox.Show(this, "成功导出到 " + filename,
                "QQ游戏资源浏览器", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        LinkedList<ThumbnailTask> thumbnailTasks = new LinkedList<ThumbnailTask>();
        BackgroundWorker currentWorker;

        /// <summary>
        /// Loads the thumbnail image for the given list view item 
        /// if not already loaded.
        /// </summary>
        /// <param name="tag"></param>
        private void LoadThumbnail(ListViewItem item)
        {
             ListViewItemTag tag = item.Tag as ListViewItemTag;
            if (tag.Thumbnail != null) // already loaded
                return;

            // Create a task for the thumbnailWorker.
            ThumbnailTask task = new ThumbnailTask();
            task.Tag = item.Tag as ListViewItemTag;
            task.ItemIndex = item.Index;
            lock (thumbnailTasks)
            {
                if (thumbnailTasks.Count == 0)
                {
                    currentWorker = new BackgroundWorker();
                    currentWorker.WorkerReportsProgress = true;
                    currentWorker.DoWork += currentWorker_DoWork;
                    currentWorker.ProgressChanged += currentWorker_ProgressChanged;
                    currentWorker.RunWorkerAsync(thumbnailTasks);
                }
                thumbnailTasks.AddFirst(task);
            }

#if false
            // Check if the resource format is supported.
            string name = tag.ResourceEntry.Name.ToLowerInvariant();
            if (name.EndsWith(".mif"))
            {
                using (MifImage mif = new MifImage(tag.ResourceEntry.Open()))
                {
                    if (mif.GetNextFrame())
                    {
                        tag.Thumbnail = mif.CurrentFrame.Image;
                        tag.FrameCount = mif.FrameCount;
                        return;
                    }
                }
            }
            else if (name.EndsWith(".bmp"))
            {
                using (Stream stream = tag.ResourceEntry.Open())
                {
                    tag.Thumbnail = new Bitmap(stream);
                    tag.FrameCount = 1;
                    return;
                }
            }

            // If a thumbnail image cannot be loaded, we use a default one.
            tag.Thumbnail = UnknownTypeIcon;
            tag.FrameCount = 1;
#endif
        }

        void currentWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ThumbnailTask task = e.UserState as ThumbnailTask;
            int index = task.ItemIndex;
            if (index >= 0 && index < lvEntries.Items.Count &&
                task.Tag == lvEntries.Items[index].Tag)
            {
                lvEntries.RedrawItems(index, index, true);
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine("ProgressChanged(" + index + ")");
#endif
        }

        private void currentWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            LinkedList<ThumbnailTask> tasks = e.Argument as LinkedList<ThumbnailTask>;
            while (true)
            {
                // Retrieve the first undone task in the task queue.
                ThumbnailTask task = null;
                lock (tasks)
                {
                    while (tasks.Count > 0)
                    {
                        task = tasks.First.Value as ThumbnailTask;
                        if (task.Tag.ResourceEntry != null && task.Tag.Thumbnail == null)
                            break;
                        tasks.RemoveFirst();
                    }
                    if (tasks.Count == 0)
                        return;
                }

                // Perform this task.
                ListViewItemTag tag = task.Tag;
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Starting task for item " + task.ItemIndex);
#endif

                // Check if the resource format is supported.
                string name = tag.ResourceEntry.Name.ToLowerInvariant();
                if (name.EndsWith(".mif"))
                {
                    using (MifImage mif = new MifImage(tag.ResourceEntry.Open()))
                    {
                        if (mif.GetNextFrame())
                        {
                            tag.Thumbnail = mif.CurrentFrame.Image;
                            tag.FrameCount = mif.FrameCount;
                        }
                    }
                }
                else if (name.EndsWith(".bmp"))
                {
                    using (Stream stream = tag.ResourceEntry.Open())
                    {
                        tag.Thumbnail = new Bitmap(stream);
                        tag.FrameCount = 1;
                    }
                }

                // If a thumbnail image cannot be loaded, we use a default one.
                if (tag.Thumbnail == null)
                {
                    tag.Thumbnail = UnknownTypeIcon;
                    tag.FrameCount = 1;
                }

                // Report progress.
                currentWorker.ReportProgress(0, task);
            }
        }
    }

    class ThumbnailTask
    {
        public int ItemIndex;
        public ListViewItemTag Tag;
    }
}