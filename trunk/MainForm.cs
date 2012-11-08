using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

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
            picPreview.Image = null;
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

        private void button2_Click(object sender, EventArgs e)
        {
            if (lvEntries.SelectedIndices.Count == 0)
                return;

            int index = lvEntries.SelectedIndices[0];
            saveFileDialog1.FileName = Path.GetFileName(lvEntries.Items[index].Text);
            if (saveFileDialog1.ShowDialog(this) != DialogResult.OK)
                return;

            string filename = saveFileDialog1.FileName;
            using (Stream stream = pkg.Extract(index))
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

        struct AnimationInfo
        {
            public bool Enabled;       // whether the animation shall be played
            public ListViewItem Item;  // which list view item is being animated
            public MifImage Image;     // the whole image
            public Image CurrentFrame; // the current frame
            public int FrameIndex;     // index of the current frame (0-based)
            public int FrameCount;     // total number of frames
        }

        private AnimationInfo animation;

        private void PlayNextFrame()
        {
            // If there's no animation in progress, reset the timer and exit.
            if (!animation.Enabled)
            {
                timerAnimation.Stop();
                return;
            }

            // Load the next frame in the current image.
            MifFrame frame = animation.Image.GetNextFrame();

            // If there are no more frames, we reset the timer and leave
            // the preview box with the last frame displayed.
            if (frame == null)
            {
                timerAnimation.Stop();
                return;
            }

            // Display the next frame and set the timer interval.
            //picPreview.Image = frame.Image;
            animation.FrameIndex++;
            animation.CurrentFrame = frame.Image;
            lvEntries.RedrawItems(animation.Item.Index, animation.Item.Index, false);
            timerAnimation.Interval = Math.Max(frame.Delay, 25);
            timerAnimation.Start();
        }

        private void lvEntries_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Reset the animation and preview box.
            timerAnimation.Stop();
            picPreview.Image = null;

            // Do nothing if no item is selected.
            if (lvEntries.SelectedIndices.Count == 0)
                return;

            ListViewItem item = lvEntries.SelectedItems[0];
            string filename = item.Text;

            if (item.Tag is FileItem)
            {
                FileItem ent = item.Tag as FileItem;
                if (ent.Thumbnail != null)
                {
                    label1.Text = 
                        ent.Thumbnail.Width + " x " +
                        ent.Thumbnail.Height + ", " +
                        ent.FrameCount + " Frames";
                }
                if (ent.FrameCount > 1)
                {
                    FileInfo f = (item.Tag as FileItem).FileInfo;
                    animation.Enabled = true;
                    animation.Item = item;
                    animation.FrameCount = ent.FrameCount;
                    animation.FrameIndex = 0;
                    animation.Image = new MifImage(f.OpenRead());
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
            foreach (FileFolder f in rep.ImageFolders)
            {
                root.Nodes.Add(f.Path.Substring(path.Length + 1)).Tag = f;
            }

            // Expand the root node and show the tree view.
            root.Expand();
            tvFolders.Visible = true;
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                LoadPackage(openFileDialog1.FileName);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Find the root path of QQ Game.
            string rootPath = Repository.GetInstallationPath();
            if (rootPath == null)
                return;
            LoadRepository(rootPath);
            lvEntries.View = View.LargeIcon;
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
            if (e.Node.Tag is FileFolder)
            {
                PopulateListView(e.Node.Tag as FileFolder);
            }
        }

        private void PopulateListView(FileFolder folder)
        {
            lvEntries.Visible = false;
            lvEntries.Items.Clear();
            foreach (FileInfo f in folder.Files)
            {
                ListViewItem item = new ListViewItem(f.Name);
                item.SubItems.Add(f.Length.ToString("#,#"));
                FileItem fi = new FileItem();
                fi.FileInfo = f;
                item.Tag = fi;
#if false
                using (FileStream stream = f.OpenRead())
                {
                    Image img = new MifImage(stream).GetNextFrame().Image;
                    imageListPreview.Images.Add(img);
                    item.ImageIndex = imageListPreview.Images.Count - 1;
                }
#endif
                lvEntries.Items.Add(item);
            }
            lvEntries.Visible = true;
        }

        private const int ListViewImageMargin = 5;
        private const int ListViewItemBorder = 1;
        private const int ListViewItemMargin = 1;

        private void DrawListViewImage(Image img, Rectangle bounds, Graphics g)
        {
            Size frameSize = imageListPreview.ImageSize;
            frameSize.Width -= ListViewImageMargin * 2;
            frameSize.Height -= ListViewImageMargin * 2;

            // Reduce the bound width to that of the image frame.
            bounds.X += (bounds.Width - frameSize.Width) / 2;
            bounds.Width = frameSize.Width;

            // Allow 1 pixel for the border, 1 pixel for item margin, and
            // 5 pixels for image margin.
            bounds.Y += ListViewItemBorder + ListViewItemMargin + ListViewImageMargin;
            bounds.Height = frameSize.Height;

            // Draw the frame around the image.
            int spacing = 0;
            g.DrawRectangle(Pens.DarkGray,
                bounds.X - 1 - spacing, bounds.Y - 1 - spacing,
                bounds.Width + 1 + 2 * spacing, bounds.Height + 1 + 2 * spacing);

            // Fit the image into the frame, keeping scale.
            Size sz = img.Size;
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

            // Draw the image.
            g.DrawImage(img, bounds);
        }

        private void DrawListViewText(string text, Rectangle bounds, Graphics g)
        {
            // Allow 1 pixel for border and 1 pixel for margin.
            int n = ListViewItemBorder + ListViewItemMargin;
            //bounds.X += n;
            //bounds.Width -= 2 * n;
            bounds.Y += n;
            //bounds.Height -= 2 * n;

            // Skip the image height, and leave 2 pixels in between.
            int h = imageListPreview.ImageSize.Height; // +ListViewItemMargin;
            bounds.Y += h;
            bounds.Height -= h;

            // Now draw the text single-line and centered.
            TextRenderer.DrawText(g, text, lvEntries.Font, bounds, SystemColors.WindowText,
                TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter |
                TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
        }

        private void DrawListViewBorder(Rectangle bounds, Graphics g)
        {
            bounds.Width -= 1;
            bounds.Height -= 1;
            g.DrawRectangle(Pens.SkyBlue, bounds);

            bounds.X += 1;
            bounds.Y += 1;
            bounds.Width -= 2;
            bounds.Height -= 2;
            g.DrawRectangle(Pens.SkyBlue, bounds);
        }

        private void DrawListViewPlayIcon(Rectangle bounds, Graphics g)
        {
            Size frameSize = imageListPreview.ImageSize;

            // Allow 1 pixel for the border and 1 pixel for item margin.
            bounds.Y += ListViewItemBorder + ListViewItemMargin;
            bounds.Height = frameSize.Height;

            // Center the image in the bounds.
            Image bmp = QQGameRes.Properties.Resources.Play_Icon_48;
            bounds.X += (bounds.Width - bmp.Width) / 2;
            bounds.Y += (bounds.Height - bmp.Height) / 2;
            bounds.Width = bmp.Width;
            bounds.Height = bmp.Height;
            g.DrawImageUnscaled(bmp, bounds);
        }

        private void lvEntries_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (!(e.Item.Tag is FileItem))
            {
                e.DrawDefault = true;
                return;
            }
            FileItem fi = e.Item.Tag as FileItem;

            // Load the thumbnail image if not already loaded.
            if (fi.Thumbnail == null)
            {
                using (MifImage mif = new MifImage(fi.FileInfo.OpenRead()))
                {
                    fi.Thumbnail = mif.GetNextFrame().Image;
                    fi.FrameCount = mif.FrameCount;
                }
            }

            // Check if we're currently animating this item.
            bool animating = animation.Enabled && (animation.Item == e.Item);

            // If we are in the process of animation, draw the current frame.
            // Otherwise, draw the thumbnail image.
            Image img = animating ? animation.CurrentFrame : fi.Thumbnail;

            // Draw a focus rectangle if the item is selected.
            if (e.Item.Selected)
                DrawListViewBorder(e.Bounds, e.Graphics);
            
            // Draw the thumbnail or current frame.
            DrawListViewImage(img, e.Bounds, e.Graphics);

            // If this is a multi-frame image, draw a Play icon to indicate
            // that, unless we are currently playing it.
            if (fi.FrameCount > 1 && !animating)
                DrawListViewPlayIcon(e.Bounds, e.Graphics);
            
            // Draw the file name text.
            DrawListViewText(e.Item.Text, e.Bounds, e.Graphics);
        }

        private void lvEntries_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }
    }
}
