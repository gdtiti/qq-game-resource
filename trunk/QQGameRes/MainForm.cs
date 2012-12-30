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
using ControlExtensions;
using System.Reactive;
using System.Reactive.Linq;

namespace QQGameRes
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

#if false
        private ResourceFolder _activeFolder;

        /// <summary>
        /// Gets or sets the active resource folder being shown in this 
        /// window. This should be the folder that is selected in the 
        /// TreeView, and the contents of this folder should be populated
        /// in the ListView.
        /// </summary>
        public ResourceFolder ActiveFolder
        {
            get { return _activeFolder; }
            set
            {
                if (_activeFolder != value)
                {
                    _activeFolder = value;
                    if (ActiveFolderChanged != null)
                        ActiveFolderChanged(this, null);
                }
            }
        }

        /// <summary>
        /// Indicates that the ActiveFolder property has changed.
        /// </summary>
        public event EventHandler ActiveFolderChanged;
#endif

        /// <summary>
        /// Loads a QQ game resource package (.PKG file) and appends it to the
        /// tree view on the left of the main window.
        /// </summary>
        /// <param name="filename"></param>
        private void LoadPackage(string filename)
        {
            // TODO: dispose the PkgArchive objects when they are removed from
            // the treeview.
            // TODO: Only create a PkgArchive object when the node is selected.
            Package pkg = new Package(filename);
            TreeNode node = new TreeNode();
            node.Text = Path.GetFileName(filename);
            node.ImageIndex = 1;
            node.SelectedImageIndex = 1;
            node.Tag = pkg;
            tvFolders.Nodes.Add(node);
        }

        private void NewLoadRepository(string path)
        {
            var root = new DirectoryInfo(path);
            var dirs = root.EnumerateDirectories("*", SearchOption.AllDirectories);
            dirs.ToObservable().Do((DirectoryInfo di) =>
            {
                System.Diagnostics.Debug.WriteLine(di.FullName);
            }).Subscribe();
        }

        private void LoadRepository(string path)
        {
            if (path.EndsWith("/") || path.EndsWith("\\"))
                path = path.Substring(0, path.Length - 1);

            LoadDirectoryForm f = new LoadDirectoryForm();
            f.SearchPath = path;
            //f.SearchPath = "D:";
            if (f.ShowDialog(this) != System.Windows.Forms.DialogResult.OK)
                return;
            Repository rep = f.Repository;
            //Repository rep = new Repository(path);

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
            tvFolders.SetWindowTheme("explorer");
            //SetWindowTheme(lvEntries.Handle, "EXPLORER", null);

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
            if (e.Node.Tag is ResourceFolder)
            {
                viewList.ResourceFolder = e.Node.Tag as ResourceFolder;
            }
        }

        private void btnOpenPackage_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                //StopAnimation();
                //thumbnailLoader.CancelPendingTasks();
                //lvEntries.Items.Clear();
                //tvFolders.Nodes.Clear();
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
            if (viewList.ActiveEntry == null)
            {
                MessageBox.Show("No entry selected.");
                return;
            }

            ResourceListViewEntry ent = viewList.ActiveEntry;
            string ext = Path.GetExtension(ent.ResourceEntry.Name).ToLowerInvariant();
            string filter = "原始格式|*" + ext;

            // If the selected item is a multi-frame image, export as SVG.
            if (ent.FrameCount > 1)
            {
                filter += "|SVG 动画|*.svg";
            }

            // If the selected item is an image, display additional format
            // conversion options in the save dialog.
            if (ent.Thumbnail != null && ent.Thumbnail != ThumbnailLoader.DefaultIcon)
            {
                filter += "|PNG 图片|*.png";
                filter += "|BMP 图片|*.bmp";
                filter += "|JPEG 图片|*.jpg";
                filter += "|TIFF 图片|*.tif";
            }
            saveFileDialog1.Filter = filter;
            saveFileDialog1.FilterIndex = 1;
            if (ent.FrameCount > 1)
            {
                saveFileDialog1.FilterIndex = 2;
            }
            saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(
                ent.ResourceEntry.Name);

            // Show the dialog.
            if (saveFileDialog1.ShowDialog(this) != DialogResult.OK)
                return;

            string filename = saveFileDialog1.FileName;

            // If the filter index is 1 (save as is), just copy the stream.
            if (saveFileDialog1.FilterIndex == 1)
            {
                using (Stream stream = ent.ResourceEntry.Open(),
                       output = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[65536];
                    int n;
                    while ((n = stream.Read(buffer, 0, buffer.Length)) > 0)
                        output.Write(buffer, 0, n);
                }
                txtStatus.Text = "保存成功";
                return;
            }

            // Get the requested image format.
            int filterIndex = saveFileDialog1.FilterIndex;
            if (ent.FrameCount > 1)
                filterIndex--;
            ImageFormat desiredFormat =
                (filterIndex == 2) ? ImageFormat.Png :
                (filterIndex == 3) ? ImageFormat.Bmp :
                (filterIndex == 4) ? ImageFormat.Jpeg :
                (filterIndex == 5) ? ImageFormat.Tiff : ImageFormat.Emf;

            // If this is a single-frame image, convert and save it.
            if (ent.FrameCount <= 1)
            {
                ent.Thumbnail.Save(filename, desiredFormat);
                txtStatus.Text = "保存成功";
                return;
            }

            // If this is a multi-frame image and user chooses to save as
            // SVG, do that.
            if (filterIndex == 1)
            {
                using (MifImage img = new MifImage(ent.ResourceEntry.Open()))
                using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                using (Util.Media.ImageEncoder svgEncoder = new Util.Media.SvgImageEncoder(stream, img.FrameCount))
                {
                    for (int i = 0; i < img.FrameCount; i++)
                    {
                        Util.Media.ImageFrame frame = img.DecodeFrame();
                        using (frame.Image)
                        {
                            svgEncoder.EncodeFrame(frame);
                        }
                    }
                }
                txtStatus.Text = "保存成功";
                return;
            }

            // Now for a multi-frame image, ask the user how they want to save it.
            DialogResult result = MessageBox.Show(this,
                "选中的图片包含 " + ent.FrameCount + " 帧。" +
                "是否将每一帧单独存为一个文件？\r\n" +
                "如果选择是，则各帧将分别保存为\r\n    " +
                GetNumberedFileName(Path.GetFileName(filename),
                                    1, ent.FrameCount) + "\r\n" +
                "    ......\r\n    " +
                GetNumberedFileName(Path.GetFileName(filename),
                                    ent.FrameCount, ent.FrameCount) + "\r\n" +
                "如果选择否，则只保存第一帧到 " + Path.GetFileName(filename) +
                "。", this.Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            // Do nothing if the user is confused and canceled the action.
            if (result == DialogResult.Cancel)
                return;

            // If the user clicked "No", then we only save the first frame,
            // which is just the thumbnail.
            if (result == DialogResult.No)
            {
                ent.Thumbnail.Save(filename, desiredFormat);
                txtStatus.Text = "保存成功";
                return;
            }

            // Now the user clicked "Yes", so we need to save each frame
            // in an individual file.
            using (MifImage img = new MifImage(ent.ResourceEntry.Open()))
            {
                for (int i = 1; i <= ent.FrameCount; i++)
                {
                    if (!img.GetNextFrame())
                        break;
                    FileInfo file = new FileInfo(
                        GetNumberedFileName(filename, i, ent.FrameCount));
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
            //NewLoadRepository("D:");
            //return;
            FileVersionInfo ver = FileVersionInfo.GetVersionInfo(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            MessageBox.Show(this, this.Text + "\r\n" + 
                "版本 " + ver.ProductVersion, 
                "版本信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            string path = Repository.GetInstallationPath();
            if (path != null)
            {
                folderBrowserDialog1.SelectedPath = path;
                //folderBrowserDialog1.
            }
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                //StopAnimation();
                //thumbnailLoader.CancelPendingTasks();
                //lvEntries.Items.Clear();
                tvFolders.Nodes.Clear();
                LoadRepository(folderBrowserDialog1.SelectedPath);
            }
        }

        private void viewList_ActiveEntryChanged(object sender, EventArgs e)
        {
            // Update button state.
            btnExport.Enabled = (viewList.ActiveEntry != null);

            // Update status message of the image size.
            ResourceListViewEntry ent = viewList.ActiveEntry;
            if (ent == null) // no item selected
            {
            }
            else
            {
                if (ent.Thumbnail != null)
                {
                    txtImageSize.Text = ent.Thumbnail.Width + " x " + ent.Thumbnail.Height;
                    txtFrames.Text = ent.FrameCount + " Frames";
                }
            }
        }
    }
}
