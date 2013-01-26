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

            // Add icons for toolbar items. We need to do this manually
            // because adding icons in the IDE somehow deteroriates the
            // image quality.
            btnChooseDirectory.Image = Properties.Resources.Folder_Icon_16;
            btnChoosePackage.Image = Properties.Resources.Package_Icon_16;
            btnAbout.Image = Properties.Resources.Tips_Icon_16;

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

        private void btnChoosePackage_Click(object sender, EventArgs e)
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

        private void NewSafeImage()
        {
            IVirtualItem vItem = vFolderListView.ActiveItem;
            if (vItem == null || !(vItem is IExtractIcon) || !(vItem is IVirtualFile))
            {
                MessageBox.Show("No entry selected.");
                return;
            }

            IExtractIcon vIcon = vItem as IExtractIcon;
            object icon = vIcon.ExtractIcon(ExtractIconType.Thumbnail, Size.Empty);
            MultiFrameImage image;
            if (icon is MultiFrameImage)
                image = icon as MultiFrameImage;
            else if (icon is Image)
                image = new GdiImage(icon as Image);
            else
            {
                using (icon as IDisposable) { }
                return;
            }

#if false
            string ext = Path.GetExtension(vItem.Name).ToLowerInvariant();
            string filter = "原始格式|*" + ext;

            // If the selected item is a multi-frame image, export as SVG.
            if (image.FrameCount > 1)
            {
                filter += "|SVG 动画|*.svg";
            }

            // If the selected item is an image, display additional format
            // conversion options in the save dialog.
            if (true)
            {
                filter += "|PNG 图片|*.png";
                filter += "|BMP 图片|*.bmp";
                filter += "|JPEG 图片|*.jpg";
                filter += "|TIFF 图片|*.tif";
            }
#endif
            string filter = "Flash 动画|*.swf";
            saveFileDialog1.Filter = filter;
            saveFileDialog1.FilterIndex = 1;
            //if (image.FrameCount > 1)
            //{
            //    saveFileDialog1.FilterIndex = 2;
            //}
            saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(
                vItem.Name);

            // Show the dialog.
            if (saveFileDialog1.ShowDialog(this) != DialogResult.OK)
                return;

            SwfImageEncoder.Encode(image, saveFileDialog1.FileName);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            NewSafeImage();
            return;

            IVirtualItem vItem = vFolderListView.ActiveItem;
            if (vItem == null || !(vItem is IExtractIcon) || !(vItem is IVirtualFile))
            {
                MessageBox.Show("No entry selected.");
                return;
            }
            IExtractIcon vIcon = vItem as IExtractIcon;
            object icon = vIcon.ExtractIcon(ExtractIconType.Thumbnail, Size.Empty);
            MultiFrameImage image;
            if (icon is MultiFrameImage)
                image = icon as MultiFrameImage;
            else if (icon is Image)
                image = new GdiImage(icon as Image);
            else
            {
                using (icon as IDisposable) { }
                return;
            }

            //ResourceListViewEntry ent = viewList.ActiveEntry;
            string ext = Path.GetExtension(vItem.Name).ToLowerInvariant();
            string filter = "原始格式|*" + ext;

            // If the selected item is a multi-frame image, export as SVG.
            if (image.FrameCount > 1)
            {
                filter += "|SVG 动画|*.svg";
            }

            // If the selected item is an image, display additional format
            // conversion options in the save dialog.
            if (true)
            {
                filter += "|PNG 图片|*.png";
                filter += "|BMP 图片|*.bmp";
                filter += "|JPEG 图片|*.jpg";
                filter += "|TIFF 图片|*.tif";
            }
            saveFileDialog1.Filter = filter;
            saveFileDialog1.FilterIndex = 1;
            if (image.FrameCount > 1)
            {
                saveFileDialog1.FilterIndex = 2;
            }
            saveFileDialog1.FileName = Path.GetFileNameWithoutExtension(
                vItem.Name);

            // Show the dialog.
            if (saveFileDialog1.ShowDialog(this) != DialogResult.OK)
                return;

            string filename = saveFileDialog1.FileName;

            // If the filter index is 1 (save as is), just copy the stream.
            if (saveFileDialog1.FilterIndex == 1)
            {
                using (Stream input = (vItem as IVirtualFile).Open())
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
                image.Dispose();
                return;
            }

            // Get the requested image format.
            int filterIndex = saveFileDialog1.FilterIndex;
            if (image.FrameCount > 1)
                filterIndex--;
            ImageFormat desiredFormat =
                (filterIndex == 2) ? ImageFormat.Png :
                (filterIndex == 3) ? ImageFormat.Bmp :
                (filterIndex == 4) ? ImageFormat.Jpeg :
                (filterIndex == 5) ? ImageFormat.Tiff : ImageFormat.Emf;

            // If this is a single-frame image, convert and save it.
            if (image.FrameCount <= 1)
            {
                image.Frame.Save(filename, desiredFormat);
                txtStatus.Text = "保存成功";
                image.Dispose();
                return;
            }

            // If this is a multi-frame image and user chooses to save as
            // SVG, do that.
            if (filterIndex == 1)
            {
                // TODO: fix this
                using (Stream input = (vItem as IVirtualFile).Open())
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
                image.Dispose();
                return;
            }

            // Now for a multi-frame image, ask the user how they want to save it.
            DialogResult result = MessageBox.Show(this,
                "选中的图片包含 " + image.FrameCount + " 帧。" +
                "是否将每一帧单独存为一个文件？\r\n" +
                "如果选择是，则各帧将分别保存为\r\n    " +
                GetNumberedFileName(Path.GetFileName(filename),
                                    1, image.FrameCount) + "\r\n" +
                "    ......\r\n    " +
                GetNumberedFileName(Path.GetFileName(filename),
                                    image.FrameCount, image.FrameCount) + "\r\n" +
                "如果选择否，则只保存第一帧到 " + Path.GetFileName(filename) +
                "。", this.Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            // Do nothing if the user is confused and canceled the action.
            if (result == DialogResult.Cancel)
            {
                image.Dispose();
                return;
            }

            // If the user clicked "No", then we only save the first frame,
            // which is just the thumbnail.
            if (result == DialogResult.No)
            {
                image.Frame.Save(filename, desiredFormat);
                txtStatus.Text = "保存成功";
                image.Dispose();
                return;
            }

            // Now the user clicked "Yes", so we need to save each frame
            // in an individual file.
            for (int i = 0; i < image.FrameCount; i++)
            {
                image.FrameIndex = i;
                FileInfo file = new FileInfo(
                    GetNumberedFileName(filename, i + 1, image.FrameCount));
                if (file.Exists)
                {
                    if (MessageBox.Show(this, "文件 " + file.FullName +
                        " 已经存在。是否要覆盖？", "保存素材",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Exclamation) != DialogResult.Yes)
                    {
                        image.Dispose();
                        return;
                    }
                }
                image.Frame.Save(file.FullName);
            }
            txtStatus.Text = "保存成功";
            image.Dispose();
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            FileVersionInfo ver = FileVersionInfo.GetVersionInfo(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            MessageBox.Show(this, this.Text + "\r\n" + 
                "版本 " + ver.ProductVersion, 
                "版本信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnChooseDirectory_Click(object sender, EventArgs e)
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
        }

        private void vFolderListView_ActiveItemChanged(object sender, EventArgs e)
        {
            IVirtualItem vItem = vFolderListView.ActiveItem;
            btnExport.Enabled = (vItem != null);
            btnExportGroup.Enabled = (vItem != null);

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
                var mif = vFolderListView.GetLoadedThumbnail(vItem) as QQGame.MifImage;
                if (mif != null)
                {
                    bool is16bit = (mif.Frame.PixelFormat == PixelFormat.Format16bppRgb565);

                    txtImageSize.Text = string.Format(
                        "{0} x {1} 像素 / {2} 位 / 原始 {3:#,0} KB / 占用 {4:#,0} KB",
                        mif.Width,
                        mif.Height,
                        is16bit ? 16 : 32,
                        ((vItem as ImageFile).File.Length + 1023) / 1024,
                        (mif.CompressedSize + 1023) / 1024);
#if true
                    txtFrames.Text = string.Format(
                        "共 {0} 帧，{1:0.0} 秒",
                        mif.FrameCount,
                        mif.Duration.TotalSeconds);
#else
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
#endif
                }
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

                using (IPixelBuffer pixelBuffer = new BitmapPixelBuffer(
                       mif.Frame as Bitmap,
                       PixelFormat.Format32bppArgb,
                       ImageLockMode.ReadOnly))
                {
                    pixelBuffer.Read(0, pixels, 0, pixels.Length);
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

        private void btnPlayMode_Click(object sender, EventArgs e)
        {
            btnPlayAll.Checked = (sender == btnPlayAll);
            btnPlaySelected.Checked = (sender == btnPlaySelected);
            btnPlayNone.Checked = (sender == btnPlayNone);

            if (sender == btnPlayAll)
            {
                
            }
            if (sender == btnPlaySelected)
            {
            }
            if (sender == btnPlayNone)
            {
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            string path = @"D:\Games\QQGame\GameShow\item";
            FileInfo[] files = (new DirectoryInfo(path)).GetFiles("*.mif");
            int numFiles = 0;
            long totalSize = 0;
            long totalCompressed = 0;

            // Load and release all the files.
            Stopwatch watch = new Stopwatch();
            foreach (FileInfo file in files)
            {
                numFiles++;
                totalSize += file.Length;
                using (Stream input = file.OpenRead())
                using (MemoryStream memory = new MemoryStream())
                {
                    input.CopyTo(memory);
                    memory.Seek(0, SeekOrigin.Begin);

                    using (QQGame.MifImage mif = new QQGame.MifImage(memory))
                    {
                        watch.Start();
                        totalCompressed += mif.CompressedSize;
                        if (mif.FrameCount > 1)
                        {
                            for (int i = 0; i < mif.FrameCount * 10; i++)
                                mif.AdvanceFrame(true);
                        }
                        watch.Stop();
                    }
                }
                if (numFiles >= 100)
                    break;
            }

            string msg = "";
            msg += string.Format(
                "Loaded {0} files in {1:0.000} seconds.\n",
                numFiles, watch.Elapsed.TotalSeconds);
            msg += string.Format(
                "Total  size: {0:#,0} KB.\n", totalSize / 1024);
            msg += string.Format(
                "Memory size: {0:#,0} KB.\n", totalCompressed / 1024);
            System.Diagnostics.Debug.Write("\n" + msg);
            MessageBox.Show(msg);
        }
    }
}
