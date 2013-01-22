namespace QQGameRes
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.timerAnimation = new System.Windows.Forms.Timer(this.components);
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.vFolderTreeView = new Util.Forms.VirtualFolderTreeView();
            this.vFolderListView = new Util.Forms.VirtualFolderListView();
            this.toolStrip3 = new System.Windows.Forms.ToolStrip();
            this.btnOpenFolder = new System.Windows.Forms.ToolStripButton();
            this.btnOpenPackage = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnExport = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnAbout = new System.Windows.Forms.ToolStripButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.progLoadDirectory = new System.Windows.Forms.ToolStripProgressBar();
            this.txtStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.txtImageSize = new System.Windows.Forms.ToolStripStatusLabel();
            this.txtFrames = new System.Windows.Forms.ToolStripStatusLabel();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.columnPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.timerLoadProgress = new System.Windows.Forms.Timer(this.components);
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.自动播放ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.仅播放选中项目ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.不播放动画ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewList = new QQGameRes.ResourceListView();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip3.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = "QQ游戏资源包 (*.pkg)|*.pkg|所有文件 (*.*)|*.*";
            this.openFileDialog1.Title = "打开QQ游戏资源包";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Filter = "所有文件 (*.*)|*.*";
            this.saveFileDialog1.Title = "保存素材";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 30);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.vFolderTreeView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.vFolderListView);
            this.splitContainer1.Panel2.Controls.Add(this.viewList);
            this.splitContainer1.Size = new System.Drawing.Size(788, 404);
            this.splitContainer1.SplitterDistance = 211;
            this.splitContainer1.TabIndex = 8;
            // 
            // vFolderTreeView
            // 
            this.vFolderTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.vFolderTreeView.Location = new System.Drawing.Point(0, 0);
            this.vFolderTreeView.Margin = new System.Windows.Forms.Padding(4);
            this.vFolderTreeView.Name = "vFolderTreeView";
            this.vFolderTreeView.Size = new System.Drawing.Size(211, 404);
            this.vFolderTreeView.TabIndex = 11;
            this.vFolderTreeView.ActiveFolderChanged += new System.EventHandler(this.vFolderTreeView_ActiveFolderChanged);
            // 
            // vFolderListView
            // 
            this.vFolderListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.vFolderListView.Folder = null;
            this.vFolderListView.Location = new System.Drawing.Point(0, 200);
            this.vFolderListView.Margin = new System.Windows.Forms.Padding(4);
            this.vFolderListView.Name = "vFolderListView";
            this.vFolderListView.Size = new System.Drawing.Size(573, 204);
            this.vFolderListView.TabIndex = 1;
            this.vFolderListView.ThumbnailFrameColor = System.Drawing.Color.DarkGray;
            this.vFolderListView.ThumbnailSize = new System.Drawing.Size(120, 165);
            this.vFolderListView.ActiveItemChanged += new System.EventHandler(this.vFolderListView_ActiveItemChanged);
            // 
            // toolStrip3
            // 
            this.toolStrip3.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnOpenFolder,
            this.btnOpenPackage,
            this.toolStripSeparator1,
            this.btnExport,
            this.toolStripSeparator2,
            this.btnAbout,
            this.toolStripDropDownButton1});
            this.toolStrip3.Location = new System.Drawing.Point(0, 0);
            this.toolStrip3.Name = "toolStrip3";
            this.toolStrip3.Padding = new System.Windows.Forms.Padding(4, 2, 2, 2);
            this.toolStrip3.Size = new System.Drawing.Size(788, 30);
            this.toolStrip3.TabIndex = 11;
            this.toolStrip3.Text = "toolStrip3";
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.Image = ((System.Drawing.Image)(resources.GetObject("btnOpenFolder.Image")));
            this.btnOpenFolder.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(85, 23);
            this.btnOpenFolder.Text = "打开目录";
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // btnOpenPackage
            // 
            this.btnOpenPackage.Image = ((System.Drawing.Image)(resources.GetObject("btnOpenPackage.Image")));
            this.btnOpenPackage.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnOpenPackage.Name = "btnOpenPackage";
            this.btnOpenPackage.Size = new System.Drawing.Size(85, 23);
            this.btnOpenPackage.Text = "打开资源";
            this.btnOpenPackage.Click += new System.EventHandler(this.btnOpenPackage_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 26);
            // 
            // btnExport
            // 
            this.btnExport.Enabled = false;
            this.btnExport.Image = ((System.Drawing.Image)(resources.GetObject("btnExport.Image")));
            this.btnExport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(85, 23);
            this.btnExport.Text = "保存素材";
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 26);
            // 
            // btnAbout
            // 
            this.btnAbout.Image = ((System.Drawing.Image)(resources.GetObject("btnAbout.Image")));
            this.btnAbout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(85, 23);
            this.btnAbout.Text = "版本信息";
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.progLoadDirectory,
            this.txtStatus,
            this.txtImageSize,
            this.txtFrames});
            this.statusStrip1.Location = new System.Drawing.Point(0, 434);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(788, 28);
            this.statusStrip1.TabIndex = 10;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // progLoadDirectory
            // 
            this.progLoadDirectory.Name = "progLoadDirectory";
            this.progLoadDirectory.Size = new System.Drawing.Size(100, 22);
            // 
            // txtStatus
            // 
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.Size = new System.Drawing.Size(559, 23);
            this.txtStatus.Spring = true;
            this.txtStatus.Text = "Status";
            this.txtStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtImageSize
            // 
            this.txtImageSize.Name = "txtImageSize";
            this.txtImageSize.Size = new System.Drawing.Size(65, 23);
            this.txtImageSize.Text = "图片尺寸";
            // 
            // txtFrames
            // 
            this.txtFrames.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
            this.txtFrames.Name = "txtFrames";
            this.txtFrames.Size = new System.Drawing.Size(47, 23);
            this.txtFrames.Text = "共?帧";
            // 
            // folderBrowserDialog1
            // 
            this.folderBrowserDialog1.Description = "请选取QQ游戏目录。本程序会自动搜索这个目录和其子目录中的游戏资源。";
            this.folderBrowserDialog1.ShowNewFolderButton = false;
            // 
            // columnPath
            // 
            this.columnPath.Text = "文件名";
            this.columnPath.Width = 180;
            // 
            // columnSize
            // 
            this.columnSize.Text = "大小";
            this.columnSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnSize.Width = 80;
            // 
            // timerLoadProgress
            // 
            this.timerLoadProgress.Tick += new System.EventHandler(this.timerLoadProgress_Tick);
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.自动播放ToolStripMenuItem,
            this.仅播放选中项目ToolStripMenuItem,
            this.不播放动画ToolStripMenuItem});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(94, 23);
            this.toolStripDropDownButton1.Text = "动画模式";
            // 
            // 自动播放ToolStripMenuItem
            // 
            this.自动播放ToolStripMenuItem.Checked = true;
            this.自动播放ToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.自动播放ToolStripMenuItem.Name = "自动播放ToolStripMenuItem";
            this.自动播放ToolStripMenuItem.Size = new System.Drawing.Size(176, 24);
            this.自动播放ToolStripMenuItem.Text = "自动播放";
            // 
            // 仅播放选中项目ToolStripMenuItem
            // 
            this.仅播放选中项目ToolStripMenuItem.Name = "仅播放选中项目ToolStripMenuItem";
            this.仅播放选中项目ToolStripMenuItem.Size = new System.Drawing.Size(176, 24);
            this.仅播放选中项目ToolStripMenuItem.Text = "仅播放选中项目";
            // 
            // 不播放动画ToolStripMenuItem
            // 
            this.不播放动画ToolStripMenuItem.Name = "不播放动画ToolStripMenuItem";
            this.不播放动画ToolStripMenuItem.Size = new System.Drawing.Size(176, 24);
            this.不播放动画ToolStripMenuItem.Text = "不播放动画";
            // 
            // viewList
            // 
            this.viewList.Dock = System.Windows.Forms.DockStyle.Top;
            this.viewList.Location = new System.Drawing.Point(0, 0);
            this.viewList.Margin = new System.Windows.Forms.Padding(4);
            this.viewList.Name = "viewList";
            this.viewList.ResourceFolder = null;
            this.viewList.Size = new System.Drawing.Size(573, 200);
            this.viewList.TabIndex = 0;
            this.viewList.ActiveEntryChanged += new System.EventHandler(this.viewList_ActiveEntryChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(788, 462);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip3);
            this.Controls.Add(this.statusStrip1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "QQ游戏资源浏览器";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.MainForm_DragOver);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip3.ResumeLayout(false);
            this.toolStrip3.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ColumnHeader columnPath;
        private System.Windows.Forms.ColumnHeader columnSize;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Timer timerAnimation;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip3;
        private System.Windows.Forms.ToolStripButton btnOpenFolder;
        private System.Windows.Forms.ToolStripButton btnOpenPackage;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnExport;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton btnAbout;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel txtFrames;
        private System.Windows.Forms.ToolStripStatusLabel txtImageSize;
        private System.Windows.Forms.ToolStripStatusLabel txtStatus;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private ResourceListView viewList;
        private System.Windows.Forms.ToolStripProgressBar progLoadDirectory;
        private System.Windows.Forms.Timer timerLoadProgress;
        private Util.Forms.VirtualFolderTreeView vFolderTreeView;
        private Util.Forms.VirtualFolderListView vFolderListView;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem 自动播放ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 仅播放选中项目ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 不播放动画ToolStripMenuItem;
    }
}

