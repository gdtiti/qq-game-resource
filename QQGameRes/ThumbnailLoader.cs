using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;
using System.Drawing;
using Util.Media;

namespace QQGameRes
{
    /// <summary>
    /// Manages a worker thread to extract the thumbnails of resource entries
    /// in the background.
    /// </summary>
    class ThumbnailLoader
    {
        /// <summary>
        /// The icon used for a resource entry whose thumbnail cannot be 
        /// extracted because its format is not supported.
        /// </summary>
        public static Bitmap DefaultIcon = Properties.Resources.Page_Icon_64;

        /// <summary>
        /// The icon used to indicate that the thumbnail of a resource entry
        /// is being loaded.
        /// </summary>
        public static Bitmap LoadingIcon = Properties.Resources.Image_Icon_16;

        /// <summary>
        /// A LIFO queue (stack) of thumbnail extraction tasks. This object is
        /// shared by the UI thread and the worker thread, and therefore must 
        /// be locked each time it is accessed.
        /// </summary>
        private Stack<ThumbnailTask> taskQueue = new Stack<ThumbnailTask>();

        /// <summary>
        /// The worker thread.
        /// </summary>
        private BackgroundWorker worker;

        /// <summary>
        /// Cancels all pending tasks. Tasks that have already started or
        /// finished will not be affected.
        /// </summary>
        public void CancelPendingTasks()
        {
            lock (taskQueue)
            {
                taskQueue.Clear();
            }
        }

        /// <summary>
        /// Loads the thumbnail image for the given ListViewItem in the
        /// background. The ListViewItem will be redrawn after the thumbnail
        /// is loaded.
        /// </summary>
        /// <param name="item">The ListViewItem whose thumbnail is to be
        /// loaded.</param>
        public void AddTask(ListViewItem item)
        {
            // Create a task for the thumbnailWorker.
            ThumbnailTask task = new ThumbnailTask();
            task.Item = item;
            task.Tag = item.Tag as ResourceListViewEntry;

            // Insert the task to the beginning of the task queue.
            lock (taskQueue)
            {
                // If the task queue is currently empty, any previous worker
                // thread will have exited or will exit soon. Therefore we
                // need to create a new worker thread.
                if (taskQueue.Count == 0)
                {
                    worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = true;
                    worker.DoWork += worker_DoWork;
                    worker.ProgressChanged += worker_ProgressChanged;
                    worker.RunWorkerAsync(taskQueue);
                }
                taskQueue.Push(task);
            }
        }

        /// <summary>
        /// Event handler that is triggered when the worker thread has loaded
        /// a thumbnail in the task queue.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ThumbnailTask task = e.UserState as ThumbnailTask;
            int index = task.Item.Index;
            if (index >= 0)
                task.Item.ListView.RedrawItems(index, index, true);
#if DEBUG
            System.Diagnostics.Debug.WriteLine("ProgressChanged(" + index + ")");
#endif
        }

        /// <summary>
        /// Entry routine of the worker thread.
        /// </summary>
        private static void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Stack<ThumbnailTask> tasks = (e.Argument as Stack<ThumbnailTask>);
            while (true)
            {
                // Retrieve the next undone task in the task queue, starting
                // from the end because those are newer tasks.
                ThumbnailTask task = null;
                lock (tasks)
                {
                    while (tasks.Count > 0)
                    {
                        task = tasks.Peek();
                        if (task.Tag.Thumbnail == null)
                            break;
                        tasks.Pop();
                    }
                    if (tasks.Count == 0) // no more tasks
                        return;
                }

                // Perform this task.
                ResourceListViewEntry tag = task.Tag;
#if DEBUG && false
                System.Diagnostics.Debug.WriteLine("Starting task for item " + task.ItemIndex);
#endif

                // Check if the resource format is supported.
                string name = tag.ResourceEntry.Name.ToLowerInvariant();
                if (name.EndsWith(".mif"))
                {
                    using (Stream stream = tag.ResourceEntry.Open())
                    using (ImageDecoder mif = new QQGame.MifImageDecoder(stream))
                    {
                        tag.Thumbnail = mif.DecodeFrame().Image;
                        tag.FrameCount = mif.FrameCount;
                    }
                }
                else if (name.EndsWith(".bmp"))
                {
                    using (Stream stream = tag.ResourceEntry.Open())
                    {
                        tag.Thumbnail = new Bitmap(stream);
                        tag.FrameCount = 1;
                    }
                }

                // If a thumbnail image cannot be loaded, we use a default one.
                if (tag.Thumbnail == null)
                {
                    tag.Thumbnail = DefaultIcon;
                    tag.FrameCount = 1;
                }

                // Report progress.
                (sender as BackgroundWorker).ReportProgress(0, task);
            }
        }
    }

    /// <summary>
    /// Contains information about a thumbnail extraction task.
    /// </summary>
    class ThumbnailTask
    {
        /// <summary>
        /// The ListViewItem whose thumbnail to be extracted. This field can
        /// only be accessed by the UI thread, and must not be accessed by
        /// the worker thread.
        /// </summary>
        public ListViewItem Item;

        /// <summary>
        /// Contains information about the resource whose thumbnail is to be
        /// extracted.
        /// </summary>
        public ResourceListViewEntry Tag;
    }
}
