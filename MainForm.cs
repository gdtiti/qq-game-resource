﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            pictureBox1.Image = null;
            for (int i = 0; i < pkg.EntryCount; i++)
            {
                PackageEntry entry = pkg.GetEntry(i);
                ListViewItem item = new ListViewItem(entry.Path);
                item.SubItems.Add(entry.OriginalSize.ToString("#,#"));
                item.SubItems.Add(entry.Size.ToString("#,#"));
                item.SubItems.Add(entry.Offset.ToString("X8"));
                lvEntries.Items.Add(item);
            }
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
                this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void lvEntries_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvEntries.SelectedIndices.Count == 0)
                return;

            int index = lvEntries.SelectedIndices[0];
            string filename = lvEntries.Items[index].Text;
            if (filename.EndsWith(".mif", StringComparison.InvariantCultureIgnoreCase))
            {
                using (Stream stream = pkg.Extract(index))
                {
                    Image[] images = MifImage.Load(stream);
                    pictureBox1.Image = images[0];
                }
            }
            else if (filename.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase))
            {
                using (Stream stream = pkg.Extract(index))
                {
                    pictureBox1.Image = new Bitmap(stream);
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

        private Image[] animationFrames;
        private int animationCurrent;

        private void button1_Click(object sender, EventArgs e)
        {
#if true
            if (lvEntries.SelectedIndices.Count == 0)
                return;

            int index = lvEntries.SelectedIndices[0];
            using (Stream stream = pkg.Extract(index))
            {
                Image[] images = MifImage.Load(stream);
                animationFrames = images;
                animationCurrent = 0;
                pictureBox1.Image = images[0];
                timer1.Interval = 100;
                timer1.Start();
            }
#endif
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (animationFrames == null || ++animationCurrent >= animationFrames.Length)
            {
                timer1.Stop();
                return;
            }
            pictureBox1.Image = animationFrames[animationCurrent];
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            lvEntries.Columns[2].Width = 0;
            lvEntries.Columns[3].Width = 0;
        }
    }
}
