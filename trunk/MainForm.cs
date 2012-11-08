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

        private MifImage currentImage;

        private void PlayNextFrame()
        {
            // If there's no image present, reset the timer and exit.
            if (currentImage == null)
            {
                timerAnimation.Stop();
                return;
            }

            // Load the next frame in the current image.
            MifFrame frame = currentImage.GetNextFrame();

            // If there are no more frames, we reset the timer and leave
            // the preview box with the last frame displayed.
            if (frame == null)
            {
                timerAnimation.Stop();
                return;
            }

            // Display the next frame and set the timer interval.
            picPreview.Image = frame.Image;
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

            int index = lvEntries.SelectedIndices[0];
            string filename = lvEntries.Items[index].Text;

            if (lvEntries.Items[index].Tag is FileInfo)
            {
                FileInfo f = lvEntries.Items[index].Tag as FileInfo;
                currentImage = new MifImage(f.OpenRead());
                label1.Text = currentImage.Width + " x " +
                    currentImage.Height + ", " + currentImage.FrameCount + " Frames";
                PlayNextFrame();
                return;
            }

            if (filename.EndsWith(".mif", StringComparison.InvariantCultureIgnoreCase))
            {
                currentImage = new MifImage(pkg.Extract(index));
                PlayNextFrame();
            }
            else if (filename.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase))
            {
                using (Stream stream = pkg.Extract(index))
                {
                    picPreview.Image = new Bitmap(stream);
                }
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
                lvEntries.Visible = false;
                lvEntries.Items.Clear();
                foreach (FileInfo f in ((FileFolder)e.Node.Tag).Files)
                {
                    ListViewItem item = new ListViewItem(f.Name);
                    item.SubItems.Add(f.Length.ToString("#,#"));
                    item.Tag = f;
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
            //lvEntries.View = View.LargeIcon;
        }

#if false
        private void tvFolders_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            // Return if the selected node is not a folder.
            if (e.Node.Tag == null)
                return;

            // Return if the selected node does not contain a DUMMY child.
            if (e.Node.Nodes.Count == 0 || e.Node.Nodes[0].Tag != null)
                return;

            // Remove the dummy child.
            e.Node.Nodes.Clear();

            if (e.Node.Tag is FileFolder)
            {
                FileFolder[] subFolders = ((FileFolder)e.Node.Tag).GetSubFolders();
                foreach (FileFolder subFolder in subFolders)
                {
                    TreeNode child = e.Node.Nodes.Add(subFolder.Name);
                    child.Tag = subFolder;
                    if (subFolder.GetSubFolders().Length > 0)
                        child.Nodes.Add("DUMMY");
                }
            }
        }
#endif

#if false
        private void tvFolders_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag == null)
                return;
            if (e.Node.Tag is FileFolder)
            {
                FileInfo[] files = ((FileFolder)e.Node.Tag).GetFiles();
                lvEntries.Items.Clear();
                foreach (FileInfo file in files)
                {
                    ListViewItem item = new ListViewItem(file.Name);
                    item.SubItems.Add(file.Length.ToString("#,#"));
                    lvEntries.Items.Add(item);
                }
            }
        }
#endif
    }
}
