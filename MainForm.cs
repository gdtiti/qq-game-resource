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
            this.Text = "QQ游戏素材浏览器 - " + Path.GetFileName(filename);
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
                "QQ游戏素材浏览器", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                LoadPackage(openFileDialog1.FileName);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            lvEntries.Columns[2].Width = 0;
            lvEntries.Columns[3].Width = 0;
        }

        private void timerAnimation_Tick(object sender, EventArgs e)
        {
            PlayNextFrame();
        }
    }
}
