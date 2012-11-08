using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

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
            for (int i = 0; i < pkg.EntryCount; i++)
            {
                PackageEntry entry = pkg.GetEntry(i);
                ListViewItem item = new ListViewItem(entry.Path);
                item.SubItems.Add(entry.OriginalSize.ToString("#,#"));
                item.SubItems.Add(entry.Size.ToString("#,#"));
                item.SubItems.Add(entry.Offset.ToString("X8"));
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
            // Stop the current animation, if any.
            StopAnimation();

            // Do nothing if no item is selected.
            if (lvEntries.SelectedIndices.Count == 0)
                return;

            ListViewItem item = lvEntries.SelectedItems[0];
            string filename = item.Text;

            if (item.Tag is ListItemInfo)
            {
                ListItemInfo tag = item.Tag as ListItemInfo;
                if (tag.Thumbnail != null)
                {
                    txtImageSize.Text = tag.Thumbnail.Width + " x " + tag.Thumbnail.Height;
                    txtFrames.Text = tag.FrameCount + " Frames";
                }
                if (tag.FrameCount > 1)
                {
                    ResourceEntry ent = (item.Tag as ListItemInfo).ResourceEntry;
                    animatedItem = item;
                    animatedImage = new MifImage(ent.Open());
                    animationEnded = false;
                    PlayNextFrame();
                }
                return;
            }

#if false
            if (filename.EndsWith(".mif", StringComparison.InvariantCultureIgnoreCase))
            {
                currentImage = new MifImage(pkg.Extract(item.Index));
                PlayNextFrame();
            }
            else if (filename.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase))
            {
                using (Stream stream = pkg.Extract(item.Index))
                {
                    picPreview.Image = new Bitmap(stream);
                }
            }
#endif
        }

        private void LoadRepository(string path)
        {
            if (path.EndsWith("/") || path.EndsWith("\\"))
                path = path.Substring(0, path.Length - 1);
            Repository rep = new Repository(path);

            // Hide and clear the tree view to reduce UI glitter.
            tvFolders.Visible = false;
            tvFolders.Nodes.Clear();

            // Add branch for image files under this path.
            TreeNode root = tvFolders.Nodes.Add(path);
            foreach (FileGroup f in rep.ImageFolders)
            {
                root.Nodes.Add(f.Name.Substring(path.Length + 1)).Tag = f;
            }

            // Expand the root node and show the tree view.
            root.Expand();
            tvFolders.Visible = true;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            btnAnimate.Visible = false;
            toolStripSeparator3.Visible = false;

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
            if (e.Node.Tag is FileGroup)
            {
                StopAnimation();
                PopulateListView(e.Node.Tag as FileGroup);
            }
        }

        /// <summary>
        /// Populates the list view with entries from the given collection.
        /// After the function returns, the list view will be scrolled to
        /// the top, and no item will be selected.
        /// </summary>
        /// <param name="folder"></param>
        private void PopulateListView(FileGroup folder)
        {
            lvEntries.Items.Clear();
            lvEntries.SelectedIndices.Clear();
            lvEntries.Visible = false;
            foreach (ResourceEntry ent in folder.Entries)
            {
                ListViewItem item = new ListViewItem(ent.Name);
                item.SubItems.Add(ent.Size.ToString("#,#"));
                ListItemInfo tag = new ListItemInfo();
                tag.ResourceEntry = ent;
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

            if (!(e.Item.Tag is ListItemInfo))
            {
                e.DrawDefault = true;
                return;
            }
            ListItemInfo tag = e.Item.Tag as ListItemInfo;

            // Load the thumbnail image if not already loaded.
            if (tag.Thumbnail == null)
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

            // Check if we're currently animating this item.
            bool animating = (animatedItem == e.Item);

            // If we are in the process of animation, draw the current frame.
            // Otherwise, draw the thumbnail image.
            Image img = animating ? animatedImage.CurrentFrame.Image : tag.Thumbnail;

            // Create a custom-drawing helper object.
            ListViewItemDrawer drawer = 
                new ListViewItemDrawer(e.Item, e.Bounds, e.Graphics);

            // Draw a focus rectangle if the item is selected.
            if (e.Item.Selected)
                drawer.DrawBorder();
            
            // Draw the thumbnail or current frame.
            drawer.DrawImage(img);

            // If this is a multi-frame image, draw a Play icon to indicate
            // that, unless we are currently playing it.
            if (tag.FrameCount > 1 && !animating)
                drawer.DrawPlayIcon();
            
            // Draw the file name text.
            drawer.DrawText();
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

            int index = lvEntries.SelectedIndices[0];
            saveFileDialog1.FileName = Path.GetFileName(lvEntries.Items[index].Text);
            if (saveFileDialog1.ShowDialog(this) != DialogResult.OK)
                return;

            string filename = saveFileDialog1.FileName;
            using (Stream stream = pkg.ExtractEntry(index))
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
    }
}
