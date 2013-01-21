using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Util.Forms;
using Util.Media;

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

        private void LoadPackage(string filename)
        {
            LoadPackage(new QQGame.PkgArchive(filename));
        }

        /// <summary>
        /// Loads a QQ game resource package (.PKG file) and adds a root-level
        /// node into the tree view on the left of the main window.
        /// </summary>
        /// <param name="pkg"></param>
        private void LoadPackage(QQGame.PkgArchive ar)
        {
            // TODO: dispose the PkgArchive objects when they are removed from
            // the treeview.
            // TODO: Only create a PkgArchive object when the node is selected.
            PackageFolder f = new PackageFolder(ar);
            vFolderTreeView.AddRootFolder(f);
        }

        private CancellationTokenSource ctsLoadRepository;
        private Repository currentRepository;

        private void StopLoadingRepository()
        {
            currentRepository = null;
            timerLoadProgress.Stop();
            progLoadDirectory.Visible = false;
            txtStatus.Text = "";
        }

        private void LoadRepository(string rootPath)
        {
            DirectoryInfo rootDir = new DirectoryInfo(rootPath);
#if DEBUG
            System.Diagnostics.Debug.WriteLine("LoadRepository(" + rootDir.FullName + ")");
#endif

            // Beautify the directory name. This must be done after we create
            // the DirectoryInfo object, because otherwise "D:\" will become
            // "D:" and point to the current directory on that drive.
            if (rootPath.EndsWith("/") || rootPath.EndsWith("\\"))
                rootPath = rootPath.Substring(0, rootPath.Length - 1);

            // Stop any existing LoadRepository operation.
            if (ctsLoadRepository != null)
            {
                ctsLoadRepository.Cancel();
                ctsLoadRepository = null;
            }
            StopLoadingRepository();

            // Reset the tree view and add the repository as a root folder.
            RepositoryFolder reposFolder = new RepositoryFolder(rootDir);
            vFolderTreeView.Clear();
            vFolderTreeView.AddRootFolder(reposFolder);

            // Create a new repository search object.
            Repository rep = new Repository();
            rep.SynchronizingObject = this;
            rep.PackageDiscovered += delegate(object sender, PackageDiscoveredEventArgs e)
            {
                LoadPackage(e.Package);
            };
            rep.ImagesDiscovered += delegate(object sender, ResourceDiscoveredEventArgs e)
            {
                // Append the image folder as a child of the repository.
                // The repository folder implementation will automatically
                // notify the tree view of the change and get the UI udpated.
                reposFolder.AddImageDirectory(e.Directory, e.Files);
            };

            // Scan the given directory.
            CancellationTokenSource cts = new CancellationTokenSource();
            rep.SearchDirectory(rootDir, cts.Token).ContinueWith((Task t) =>
            {
                if (cts.IsCancellationRequested)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Cancelled: " +
                        rootPath);
#endif
                }
                else
                {
                    // Expand the first root node and show the tree view.
                    // root.Expand();
                    StopLoadingRepository();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
            ctsLoadRepository = cts;

            // Start a timer to show the progress every 100 milliseconds.
            currentRepository = rep;
            progLoadDirectory.Visible = true;
            timerLoadProgress.Start();
        }

        private void timerLoadProgress_Tick(object sender, EventArgs e)
        {
            if (currentRepository == null || currentRepository.CurrentDirectory == null)
                return;
            progLoadDirectory.Value = (int)(currentRepository.CurrentProgress * 100);
            txtStatus.Text = "正在搜索 " + currentRepository.CurrentDirectory.FullName;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            StopLoadingRepository();

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
        
        private void btnOpenPackage_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                //StopAnimation();
                //thumbnailLoader.CancelPendingTasks();
                //lvEntries.Items.Clear();
                //tvFolders.Nodes.Clear();
                LoadPackage(openFileDialog1.FileName);
                //tvFolders.SelectedNode = tvFolders.Nodes[0];
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
                using (Stream input = (ent.ResourceEntry as IVirtualFile).Open())
                using (Stream output = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    //This is .NET 2.0.
                    //byte[] buffer = new byte[65536];
                    //int n;
                    //while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                    //    output.Write(buffer, 0, n);
                    try
                    {
                        input.CopyTo(output);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "导出素材时遇到以下错误:\r\n" +
                            ex.Message + "\r\n保存的文件可能不完整。",
                            this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
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
                using (Stream input = (ent.ResourceEntry as IVirtualFile).Open())
                using (ImageDecoder decoder = new QQGame.MifImageDecoder(input))
                using (Stream output = new FileStream(filename, FileMode.Create, FileAccess.Write))
                using (ImageEncoder encoder = new SvgImageEncoder(output, decoder.FrameCount))
                {
                    for (int i = 0; i < decoder.FrameCount; i++)
                    {
                        Util.Media.ImageFrame frame = decoder.DecodeFrame();
                        using (frame.Image)
                        {
                            encoder.EncodeFrame(frame);
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
            using (Stream stream = (ent.ResourceEntry as IVirtualFile).Open())
            using (ImageDecoder img = new QQGame.MifImageDecoder(stream))
            {
                for (int i = 1; i <= img.FrameCount; i++)
                {
                    ImageFrame frame = img.DecodeFrame(); // TODO: using
                    FileInfo file = new FileInfo(
                        GetNumberedFileName(filename, i, img.FrameCount));
                    if (file.Exists)
                    {
                        if (MessageBox.Show(this, "文件 " + file.FullName +
                            " 已经存在。是否要覆盖？", "保存素材",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Exclamation) != DialogResult.Yes)
                            return;
                    }
                    frame.Image.Save(file.FullName);
                }
            }
            txtStatus.Text = "保存成功";
        }

#if false
        private bool loadD = true;
        private void btnAbout_Click(object sender, EventArgs e)
        {
            if (loadD)
                LoadRepository("D:/");
            else
                LoadRepository("E:/");
            loadD = !loadD;
        }
#else
        private void btnAbout_Click(object sender, EventArgs e)
        {
            FileVersionInfo ver = FileVersionInfo.GetVersionInfo(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            MessageBox.Show(this, this.Text + "\r\n" + 
                "版本 " + ver.ProductVersion, 
                "版本信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
#endif

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
                //tvFolders.Nodes.Clear();
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

        private void vFolderTreeView_ActiveFolderChanged(object sender, EventArgs e)
        {
            IVirtualFolder vFolder = vFolderTreeView.ActiveFolder;
            if (vFolder == null)
            {
                return;
            }
            else if (vFolder is PackageFolder)
            {
                txtStatus.Text = (vFolder as PackageFolder).Archive.FileName;
            }
            else if (vFolder is RepositoryFolder)
            {
                txtStatus.Text = (vFolder as IVirtualItem).DisplayName;
            }
            else if (vFolder is ImageFolder)
            {
                txtStatus.Text = (vFolder as ImageFolder).Directory.FullName.TrimEnd('\\');
            }
            vFolderListView.Folder = vFolder;
            viewList.ResourceFolder = vFolder;
        }

        private void vFolderListView_ActiveItemChanged(object sender, EventArgs e)
        {
            IVirtualItem vItem = vFolderListView.ActiveItem;
            if (vItem == null)
            {
                ImageFolder f = vFolderListView.Folder as ImageFolder;
                if (f != null)
                {
                    txtStatus.Text = "共 " + f.Files.Length + " 个文件";
                }
                return;
            }

            if (vItem is ImageFile)
            {
                object image = (vItem as IExtractIcon).ExtractIcon(
                    ExtractIconType.Thumbnail, Size.Empty);
                if (image is QQGame.MifImage)
                {
                    QQGame.MifImage mif = image as QQGame.MifImage;
                    var cs = mif.CompressedSize;
                    txtImageSize.Text = string.Format(
                        "{0} x {1} ({2:0,0} original, {3:0,0} DELTA, " +
                        "{4:0,0} RLE, {5:0,0} RLE-DELTA, {6:0,0} ZIP)",
                        mif.Width, mif.Height,
                        (vItem as ImageFile).File.Length,
                        cs[QQGame.MifCompressionMode.Delta],
                        cs[QQGame.MifCompressionMode.RleFrame],
                        cs[QQGame.MifCompressionMode.RleDelta],
                        cs[QQGame.MifCompressionMode.Deflate]);
                    int alphaCount;
                    int maxColorCount;
                    int totalColorCount = CountColors(mif, out alphaCount, out maxColorCount);
                    txtFrames.Text = string.Format(
                        "{0} frames ({1}), {2}/{3} colors, {4} alphas",
                        mif.FrameCount,
                        mif.Duration,
                        maxColorCount,
                        totalColorCount,
                        alphaCount);
                }
                using (image as IDisposable) { }
            }
        }

        private int CountColors(QQGame.MifImage mif, 
            out int alphaCount, out int maxColorCount)
        {
            byte[] pixels = new byte[mif.Width * mif.Height * 4];
            HashSet<int> colors = new HashSet<int>();
            HashSet<int> alphas = new HashSet<int>();
            maxColorCount = 0;
            for (int i = 0; i < mif.FrameCount; i++)
            {
                HashSet<int> frameColors = new HashSet<int>();
                mif.FrameIndex = i;
                using (BitmapPixelBuffer buffer = new BitmapPixelBuffer(
                    mif.Frame as Bitmap, PixelFormat.Format32bppArgb))
                {
                    buffer.Read(0, pixels, 0, pixels.Length);
                }

                for (int j = 0; j < mif.Width * mif.Height; j++)
                {
                    int c = BitConverter.ToInt32(pixels, j * 4);
                    colors.Add(c);
                    frameColors.Add(c);
                    alphas.Add((c >> 24) & 0xFF);
                }
                maxColorCount = Math.Max(maxColorCount, frameColors.Count);
            }
            alphaCount = alphas.Count;
            return colors.Count;
        }
    }
}
