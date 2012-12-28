using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QQGameRes
{
    public partial class LoadDirectoryForm : Form
    {
        public LoadDirectoryForm()
        {
            InitializeComponent();
        }

        public Repository Repository { get; private set; }

        public string SearchPath { get; set; }

        private DirectorySearcherProgress progress;

        CancellationTokenSource cts = new CancellationTokenSource();

        private void LoadDirectoryForm_Load(object sender, EventArgs e)
        {
            if (SearchPath == null)
                throw new InvalidOperationException("SearchPath must be set before showing the dialog.");

            this.labelProgress.Text = SearchPath;
            this.Repository = new Repository();
            progress = new DirectorySearcherProgress();
            Task t = Repository.LoadDirectoryAsync(SearchPath, progress, cts.Token);
            t.ContinueWith((Task _) => 
            {
                if (!cts.IsCancellationRequested)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
            this.timerProgress.Start();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            cts.Cancel();
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void timerProgress_Tick(object sender, EventArgs e)
        {
            labelProgress.Text =  progress.CurrentDirectory.FullName;
        }
    }
}
