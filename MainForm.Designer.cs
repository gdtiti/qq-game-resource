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
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Folder");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Repository", new System.Windows.Forms.TreeNode[] {
            treeNode1});
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("Folder");
            System.Windows.Forms.TreeNode treeNode4 = new System.Windows.Forms.TreeNode("Package", new System.Windows.Forms.TreeNode[] {
            treeNode3});
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.imageListPreview = new System.Windows.Forms.ImageList(this.components);
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.timerAnimation = new System.Windows.Forms.Timer(this.components);
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tvFolders = new System.Windows.Forms.TreeView();
            this.imageListTree = new System.Windows.Forms.ImageList(this.components);
            this.lvEntries = new QQGameRes.DoubleBufferedListView();
            this.columnPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.toolStrip3 = new System.Windows.Forms.ToolStrip();
            this.btnOpenFolder = new System.Windows.Forms.ToolStripButton();
            this.btnOpenPackage = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnExport = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnAnimate = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.btnAbout = new System.Windows.Forms.ToolStripButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.txtImageSize = new System.Windows.Forms.ToolStripStatusLabel();
            this.txtFrames = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip3.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // imageListPreview
            // 
            this.imageListPreview.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imageListPreview.ImageSize = new System.Drawing.Size(130, 175);
            this.imageListPreview.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = "QQ游戏资源包 (*.pkg)|*.pkg|所有文件 (*.*)|*.*";
            this.openFileDialog1.Title = "打开QQ游戏资源包";
            // 
            // timerAnimation
            // 
            this.timerAnimation.Tick += new System.EventHandler(this.timerAnimation_Tick);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Filter = "所有文件 (*.*)|*.*";
            this.saveFileDialog1.Title = "导出资源";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 31);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tvFolders);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.lvEntries);
            this.splitContainer1.Size = new System.Drawing.Size(856, 366);
            this.splitContainer1.SplitterDistance = 284;
            this.splitContainer1.TabIndex = 8;
            // 
            // tvFolders
            // 
            this.tvFolders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvFolders.HideSelection = false;
            this.tvFolders.ImageIndex = 0;
            this.tvFolders.ImageList = this.imageListTree;
            this.tvFolders.Indent = 21;
            this.tvFolders.Location = new System.Drawing.Point(0, 0);
            this.tvFolders.Name = "tvFolders";
            treeNode1.ImageIndex = 2;
            treeNode1.Name = "Node2";
            treeNode1.Text = "Folder";
            treeNode2.ImageIndex = 0;
            treeNode2.Name = "Node0";
            treeNode2.Text = "Repository";
            treeNode3.ImageIndex = 2;
            treeNode3.Name = "Node4";
            treeNode3.Text = "Folder";
            treeNode4.ImageIndex = 1;
            treeNode4.Name = "Node3";
            treeNode4.Text = "Package";
            this.tvFolders.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode2,
            treeNode4});
            this.tvFolders.SelectedImageIndex = 0;
            this.tvFolders.ShowLines = false;
            this.tvFolders.Size = new System.Drawing.Size(284, 366);
            this.tvFolders.TabIndex = 10;
            this.tvFolders.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvFolders_AfterSelect);
            // 
            // imageListTree
            // 
            this.imageListTree.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListTree.ImageStream")));
            this.imageListTree.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListTree.Images.SetKeyName(0, "Folder-Icon-16.png");
            this.imageListTree.Images.SetKeyName(1, "Package-Icon-16.png");
            this.imageListTree.Images.SetKeyName(2, "Images-Icon-16.png");
            // 
            // lvEntries
            // 
            this.lvEntries.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnPath,
            this.columnSize});
            this.lvEntries.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvEntries.FullRowSelect = true;
            this.lvEntries.GridLines = true;
            this.lvEntries.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvEntries.HideSelection = false;
            this.lvEntries.LargeImageList = this.imageListPreview;
            this.lvEntries.Location = new System.Drawing.Point(0, 0);
            this.lvEntries.Margin = new System.Windows.Forms.Padding(4);
            this.lvEntries.MultiSelect = false;
            this.lvEntries.Name = "lvEntries";
            this.lvEntries.OwnerDraw = true;
            this.lvEntries.Size = new System.Drawing.Size(568, 366);
            this.lvEntries.TabIndex = 5;
            this.lvEntries.UseCompatibleStateImageBehavior = false;
            this.lvEntries.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.lvEntries_DrawItem);
            this.lvEntries.SelectedIndexChanged += new System.EventHandler(this.lvEntries_SelectedIndexChanged);
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
            // toolStrip3
            // 
            this.toolStrip3.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnOpenFolder,
            this.btnOpenPackage,
            this.toolStripSeparator1,
            this.btnExport,
            this.toolStripSeparator2,
            this.btnAnimate,
            this.toolStripSeparator3,
            this.btnAbout});
            this.toolStrip3.Location = new System.Drawing.Point(0, 0);
            this.toolStrip3.Name = "toolStrip3";
            this.toolStrip3.Size = new System.Drawing.Size(856, 31);
            this.toolStrip3.TabIndex = 11;
            this.toolStrip3.Text = "toolStrip3";
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.Image = ((System.Drawing.Image)(resources.GetObject("btnOpenFolder.Image")));
            this.btnOpenFolder.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(93, 28);
            this.btnOpenFolder.Text = "打开目录";
            // 
            // btnOpenPackage
            // 
            this.btnOpenPackage.Image = ((System.Drawing.Image)(resources.GetObject("btnOpenPackage.Image")));
            this.btnOpenPackage.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnOpenPackage.Name = "btnOpenPackage";
            this.btnOpenPackage.Size = new System.Drawing.Size(93, 28);
            this.btnOpenPackage.Text = "打开资源";
            this.btnOpenPackage.Click += new System.EventHandler(this.btnOpenPackage_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 31);
            // 
            // btnExport
            // 
            this.btnExport.Image = ((System.Drawing.Image)(resources.GetObject("btnExport.Image")));
            this.btnExport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(93, 28);
            this.btnExport.Text = "导出资源";
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 31);
            // 
            // btnAnimate
            // 
            this.btnAnimate.Image = ((System.Drawing.Image)(resources.GetObject("btnAnimate.Image")));
            this.btnAnimate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAnimate.Name = "btnAnimate";
            this.btnAnimate.Size = new System.Drawing.Size(93, 28);
            this.btnAnimate.Text = "播放动画";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 31);
            // 
            // btnAbout
            // 
            this.btnAbout.Image = ((System.Drawing.Image)(resources.GetObject("btnAbout.Image")));
            this.btnAbout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(93, 28);
            this.btnAbout.Text = "版本信息";
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.txtImageSize,
            this.txtFrames});
            this.statusStrip1.Location = new System.Drawing.Point(0, 397);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(856, 28);
            this.statusStrip1.TabIndex = 10;
            this.statusStrip1.Text = "statusStrip1";
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
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(856, 425);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip3);
            this.Controls.Add(this.statusStrip1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "QQ游戏素材浏览器";
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

        private DoubleBufferedListView lvEntries;
        private System.Windows.Forms.ColumnHeader columnPath;
        private System.Windows.Forms.ColumnHeader columnSize;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Timer timerAnimation;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView tvFolders;
        private System.Windows.Forms.ImageList imageListPreview;
        private System.Windows.Forms.ToolStrip toolStrip3;
        private System.Windows.Forms.ToolStripButton btnOpenFolder;
        private System.Windows.Forms.ToolStripButton btnOpenPackage;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnExport;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton btnAnimate;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton btnAbout;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel txtFrames;
        private System.Windows.Forms.ToolStripStatusLabel txtImageSize;
        private System.Windows.Forms.ImageList imageListTree;
    }
}

