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
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.progLoadDirectory = new System.Windows.Forms.ToolStripProgressBar();
            this.txtStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.txtImageSize = new System.Windows.Forms.ToolStripStatusLabel();
            this.txtFrames = new System.Windows.Forms.ToolStripStatusLabel();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.columnPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.timerLoadProgress = new System.Windows.Forms.Timer(this.components);
            this.toolStrip3 = new System.Windows.Forms.ToolStrip();
            this.toolStripDropDownButton2 = new System.Windows.Forms.ToolStripDropDownButton();
            this.btnChooseDirectory = new System.Windows.Forms.ToolStripMenuItem();
            this.btnChoosePackage = new System.Windows.Forms.ToolStripMenuItem();
            this.btnExportGroup = new System.Windows.Forms.ToolStripDropDownButton();
            this.btnExportConverted = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnExportFirstFrame = new System.Windows.Forms.ToolStripMenuItem();
            this.btnExportEachFrame = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnExportOriginal = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.预览模式ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.详细信息ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.btnPlayAll = new System.Windows.Forms.ToolStripMenuItem();
            this.btnPlaySelected = new System.Windows.Forms.ToolStripMenuItem();
            this.btnPlayNone = new System.Windows.Forms.ToolStripMenuItem();
            this.btnAbout = new System.Windows.Forms.ToolStripButton();
            this.btnExport = new System.Windows.Forms.ToolStripButton();
            this.vFolderTreeView = new Util.Forms.VirtualFolderTreeView();
            this.vFolderListView = new Util.Forms.VirtualFolderListView();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.toolStrip3.SuspendLayout();
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
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.SizeWE;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 30);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.vFolderTreeView);
            this.splitContainer1.Panel1.Cursor = System.Windows.Forms.Cursors.Default;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.vFolderListView);
            this.splitContainer1.Panel2.Cursor = System.Windows.Forms.Cursors.Default;
            this.splitContainer1.Size = new System.Drawing.Size(788, 429);
            this.splitContainer1.SplitterDistance = 236;
            this.splitContainer1.TabIndex = 8;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.progLoadDirectory,
            this.txtStatus,
            this.txtImageSize,
            this.txtFrames});
            this.statusStrip1.Location = new System.Drawing.Point(0, 459);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.ManagerRenderMode;
            this.statusStrip1.Size = new System.Drawing.Size(788, 28);
            this.statusStrip1.TabIndex = 4;
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
            // toolStrip3
            // 
            this.toolStrip3.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripDropDownButton2,
            this.btnExportGroup,
            this.toolStripDropDownButton1,
            this.btnAbout,
            this.btnExport});
            this.toolStrip3.Location = new System.Drawing.Point(0, 0);
            this.toolStrip3.Name = "toolStrip3";
            this.toolStrip3.Padding = new System.Windows.Forms.Padding(4, 2, 2, 2);
            this.toolStrip3.Size = new System.Drawing.Size(788, 30);
            this.toolStrip3.TabIndex = 1;
            this.toolStrip3.Text = "toolStrip3";
            // 
            // toolStripDropDownButton2
            // 
            this.toolStripDropDownButton2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnChooseDirectory,
            this.btnChoosePackage});
            this.toolStripDropDownButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton2.Image")));
            this.toolStripDropDownButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton2.Name = "toolStripDropDownButton2";
            this.toolStripDropDownButton2.Size = new System.Drawing.Size(94, 23);
            this.toolStripDropDownButton2.Text = "浏览资源";
            // 
            // btnChooseDirectory
            // 
            this.btnChooseDirectory.Name = "btnChooseDirectory";
            this.btnChooseDirectory.Size = new System.Drawing.Size(143, 24);
            this.btnChooseDirectory.Text = "选择目录...";
            this.btnChooseDirectory.Click += new System.EventHandler(this.btnChooseDirectory_Click);
            // 
            // btnChoosePackage
            // 
            this.btnChoosePackage.Name = "btnChoosePackage";
            this.btnChoosePackage.Size = new System.Drawing.Size(143, 24);
            this.btnChoosePackage.Text = "选择文件...";
            this.btnChoosePackage.Click += new System.EventHandler(this.btnChoosePackage_Click);
            // 
            // btnExportGroup
            // 
            this.btnExportGroup.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnExportConverted,
            this.toolStripMenuItem2,
            this.btnExportFirstFrame,
            this.btnExportEachFrame,
            this.toolStripMenuItem1,
            this.btnExportOriginal});
            this.btnExportGroup.Enabled = false;
            this.btnExportGroup.Image = ((System.Drawing.Image)(resources.GetObject("btnExportGroup.Image")));
            this.btnExportGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnExportGroup.Name = "btnExportGroup";
            this.btnExportGroup.Size = new System.Drawing.Size(94, 23);
            this.btnExportGroup.Text = "导出素材";
            // 
            // btnExportConverted
            // 
            this.btnExportConverted.Name = "btnExportConverted";
            this.btnExportConverted.Size = new System.Drawing.Size(162, 24);
            this.btnExportConverted.Text = "转换图片格式";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(159, 6);
            // 
            // btnExportFirstFrame
            // 
            this.btnExportFirstFrame.Name = "btnExportFirstFrame";
            this.btnExportFirstFrame.Size = new System.Drawing.Size(162, 24);
            this.btnExportFirstFrame.Text = "只保存第一帧";
            // 
            // btnExportEachFrame
            // 
            this.btnExportEachFrame.Name = "btnExportEachFrame";
            this.btnExportEachFrame.Size = new System.Drawing.Size(162, 24);
            this.btnExportEachFrame.Text = "单独保存各帧";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(159, 6);
            // 
            // btnExportOriginal
            // 
            this.btnExportOriginal.Name = "btnExportOriginal";
            this.btnExportOriginal.Size = new System.Drawing.Size(162, 24);
            this.btnExportOriginal.Text = "导出原始文件";
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.预览模式ToolStripMenuItem,
            this.详细信息ToolStripMenuItem,
            this.toolStripMenuItem3,
            this.btnPlayAll,
            this.btnPlaySelected,
            this.btnPlayNone});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(94, 23);
            this.toolStripDropDownButton1.Text = "显示方式";
            // 
            // 预览模式ToolStripMenuItem
            // 
            this.预览模式ToolStripMenuItem.Name = "预览模式ToolStripMenuItem";
            this.预览模式ToolStripMenuItem.Size = new System.Drawing.Size(190, 24);
            this.预览模式ToolStripMenuItem.Text = "预览模式";
            // 
            // 详细信息ToolStripMenuItem
            // 
            this.详细信息ToolStripMenuItem.Name = "详细信息ToolStripMenuItem";
            this.详细信息ToolStripMenuItem.Size = new System.Drawing.Size(190, 24);
            this.详细信息ToolStripMenuItem.Text = "详细信息";
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(187, 6);
            // 
            // btnPlayAll
            // 
            this.btnPlayAll.Checked = true;
            this.btnPlayAll.CheckState = System.Windows.Forms.CheckState.Checked;
            this.btnPlayAll.Name = "btnPlayAll";
            this.btnPlayAll.Size = new System.Drawing.Size(190, 24);
            this.btnPlayAll.Text = "自动播放动画";
            this.btnPlayAll.Click += new System.EventHandler(this.btnPlayMode_Click);
            // 
            // btnPlaySelected
            // 
            this.btnPlaySelected.Name = "btnPlaySelected";
            this.btnPlaySelected.Size = new System.Drawing.Size(190, 24);
            this.btnPlaySelected.Text = "仅播放选中的动画";
            this.btnPlaySelected.Click += new System.EventHandler(this.btnPlayMode_Click);
            // 
            // btnPlayNone
            // 
            this.btnPlayNone.Name = "btnPlayNone";
            this.btnPlayNone.Size = new System.Drawing.Size(190, 24);
            this.btnPlayNone.Text = "不播放动画";
            this.btnPlayNone.Click += new System.EventHandler(this.btnPlayMode_Click);
            // 
            // btnAbout
            // 
            this.btnAbout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(69, 23);
            this.btnAbout.Text = "版本信息";
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
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
            // vFolderTreeView
            // 
            this.vFolderTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.vFolderTreeView.Location = new System.Drawing.Point(0, 0);
            this.vFolderTreeView.Margin = new System.Windows.Forms.Padding(4);
            this.vFolderTreeView.Name = "vFolderTreeView";
            this.vFolderTreeView.Size = new System.Drawing.Size(236, 429);
            this.vFolderTreeView.TabIndex = 2;
            this.vFolderTreeView.ActiveFolderChanged += new System.EventHandler(this.vFolderTreeView_ActiveFolderChanged);
            // 
            // vFolderListView
            // 
            this.vFolderListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.vFolderListView.Folder = null;
            this.vFolderListView.Location = new System.Drawing.Point(0, 0);
            this.vFolderListView.Margin = new System.Windows.Forms.Padding(4);
            this.vFolderListView.Name = "vFolderListView";
            this.vFolderListView.Size = new System.Drawing.Size(548, 429);
            this.vFolderListView.TabIndex = 3;
            this.vFolderListView.ThumbnailFrameColor = System.Drawing.Color.DarkGray;
            this.vFolderListView.ThumbnailFrameMargin = 4;
            this.vFolderListView.ThumbnailFrameWidth = 1;
            this.vFolderListView.ThumbnailSize = new System.Drawing.Size(120, 165);
            this.vFolderListView.ActiveItemChanged += new System.EventHandler(this.vFolderListView_ActiveItemChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(788, 487);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip3);
            this.Controls.Add(this.statusStrip1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "QQ游戏资源浏览器";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.MainForm_DragOver);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.toolStrip3.ResumeLayout(false);
            this.toolStrip3.PerformLayout();
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
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel txtFrames;
        private System.Windows.Forms.ToolStripStatusLabel txtImageSize;
        private System.Windows.Forms.ToolStripStatusLabel txtStatus;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ToolStripProgressBar progLoadDirectory;
        private System.Windows.Forms.Timer timerLoadProgress;
        private Util.Forms.VirtualFolderTreeView vFolderTreeView;
        private Util.Forms.VirtualFolderListView vFolderListView;
        private System.Windows.Forms.ToolStripButton btnExport;
        private System.Windows.Forms.ToolStripButton btnAbout;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem btnPlayAll;
        private System.Windows.Forms.ToolStripMenuItem btnPlaySelected;
        private System.Windows.Forms.ToolStripMenuItem btnPlayNone;
        private System.Windows.Forms.ToolStrip toolStrip3;
        private System.Windows.Forms.ToolStripDropDownButton btnExportGroup;
        private System.Windows.Forms.ToolStripMenuItem btnExportConverted;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem btnExportFirstFrame;
        private System.Windows.Forms.ToolStripMenuItem btnExportEachFrame;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem btnExportOriginal;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton2;
        private System.Windows.Forms.ToolStripMenuItem btnChoosePackage;
        private System.Windows.Forms.ToolStripMenuItem btnChooseDirectory;
        private System.Windows.Forms.ToolStripMenuItem 预览模式ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 详细信息ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
    }
}

