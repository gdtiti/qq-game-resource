namespace QQGameRes
{
    partial class ResourceListView
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.imageListPreview = new System.Windows.Forms.ImageList(this.components);
            this.lvEntries = new QQGameRes.DoubleBufferedListView();
            this.animator = new QQGameRes.AnimationPlayer(this.components);
            this.SuspendLayout();
            // 
            // imageListPreview
            // 
            this.imageListPreview.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imageListPreview.ImageSize = new System.Drawing.Size(130, 175);
            this.imageListPreview.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // lvEntries
            // 
            this.lvEntries.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvEntries.HideSelection = false;
            this.lvEntries.LargeImageList = this.imageListPreview;
            this.lvEntries.Location = new System.Drawing.Point(0, 0);
            this.lvEntries.MultiSelect = false;
            this.lvEntries.Name = "lvEntries";
            this.lvEntries.OwnerDraw = true;
            this.lvEntries.Size = new System.Drawing.Size(150, 150);
            this.lvEntries.TabIndex = 0;
            this.lvEntries.UseCompatibleStateImageBehavior = false;
            this.lvEntries.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.lvEntries_DrawItem);
            this.lvEntries.SelectedIndexChanged += new System.EventHandler(this.lvEntries_SelectedIndexChanged);
            // 
            // animator
            // 
            this.animator.EndDelay = 500;
            this.animator.MinDelay = 25;
            this.animator.UpdateFrame += new System.EventHandler(this.animator_UpdateFrame);
            this.animator.AnimationEnded += new System.EventHandler(this.animator_AnimationEnded);
            // 
            // ResourceListView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lvEntries);
            this.Name = "ResourceListView";
            this.ResumeLayout(false);

        }

        #endregion

        private DoubleBufferedListView lvEntries;
        private System.Windows.Forms.ImageList imageListPreview;
        private AnimationPlayer animator;
    }
}
