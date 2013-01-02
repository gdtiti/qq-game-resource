using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Util.Events;
using Util.IO;
using System.ComponentModel;

namespace Util.Events
{
    public static class EventExtensions
    {
        public static void RaiseMarshalled<TEventArgs>(
            this EventHandler<TEventArgs> handlers, object sender, TEventArgs e,
            ISynchronizeInvoke sync)
            where TEventArgs : EventArgs
        {
            if (handlers != null)
            {
                if (sync != null)
                    sync.BeginInvoke(handlers, new object[] { sender, e });
                else
                    handlers(sender, e);
            }
        }

        /// <summary>
        /// Raises the event and invokes its handlers in their respective
        /// synchronization contexts where necessary.
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>See http://stackoverflow.com/questions/1698889/raise-events-in-net-on-the-main-ui-thread
        /// </remarks>
        public static void Raise(this MulticastDelegate handlers, object sender, EventArgs e)
        {
            if (handlers == null)
                return;

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                var sync = handler.Target as System.ComponentModel.ISynchronizeInvoke;
                if (sync != null && sync.InvokeRequired)
                {
                    sync.BeginInvoke(handler, new object[] { sender, e });
                }
                else
                {
                    handler.DynamicInvoke(new object[] { sender, e });
                }
            }
        }

#if true
        public static void Raise<TEventArgs>(
            this EventHandler<TEventArgs> handlers, object sender, TEventArgs e)
            where TEventArgs : EventArgs
#else
        public static void Raise(this EventHandler handlers, object sender, EventArgs e)
#endif
        {
            if (handlers == null)
                return;

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                var sync = handler.Target as System.ComponentModel.ISynchronizeInvoke;
                if (sync != null && sync.InvokeRequired)
                {
                    sync.BeginInvoke(handler, new object[] { sender, e });
                }
                else
                {
                    handler.DynamicInvoke(new object[] { sender, e });
                }
            }
        }

        public static void RaiseUnlessCanceled(
            this MulticastDelegate handlers, object sender, EventArgs e, CancellationToken ct)
        {
            if (handlers == null)
                return;

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                if (ct.IsCancellationRequested)
                    break;
                var sync = handler.Target as System.ComponentModel.ISynchronizeInvoke;
                if (sync != null && sync.InvokeRequired)
                {
                    sync.BeginInvoke(
                        new Action<Delegate, CancellationToken, object[]>(InvocationHelper),
                        new object[] { handler, ct, new object[] { sender, e } });
                }
                else
                {
                    handler.DynamicInvoke(new object[] { sender, e });
                }
            }
        }

        private static void InvocationHelper(Delegate d, CancellationToken ct, object[] args)
        {
            if (!ct.IsCancellationRequested)
            {
                d.DynamicInvoke(args);
            }
        }
    }
}

namespace QQGameRes
{
    /// <summary>
    /// Maintains a collection of QQ Game resource files arranged in a
    /// hierarchical structure.
    /// </summary>
    public class Repository
    {
        /// <summary>
        /// Gets the full path of the installation directory of QQ games.
        /// </summary>
        /// <returns>Full path of the installation directory of QQ games,
        /// or <code>null</code> if this cannot be determined.</returns>
        public static string GetInstallationPath()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Tencent\QQGame\SYS"))
            {
                if (key != null)
                    return key.GetValue("GameDirectory") as string;
                else
                    return null;
            }
        }

        /// <summary>
        /// Creates a Repository object.
        /// </summary>
        public Repository()
        {
        }

        /// <summary>
        /// Occurs when one or more supported image files are discovered.
        /// </summary>
        public event EventHandler<ResourceDiscoveredEventArgs> ImagesDiscovered;

        /// <summary>
        /// Occurs when a QQ Game package file (.PKG) is discovered.
        /// </summary>
        public event EventHandler<PackageDiscoveredEventArgs> PackageDiscovered;

        /// <summary>
        /// Gets or sets the synchronizing object used to marshal event calls.
        /// </summary>
        public ISynchronizeInvoke SynchronizingObject { get; set; }

        /// <summary>
        /// Gets the current directory being scanned.
        /// </summary>
        public DirectoryInfo CurrentDirectory { get; private set; }

        /// <summary>
        /// Gets a number between 0.0 and 1.0 which indicates the percentage
        /// of work completed of the current search.
        /// </summary>
        public double CurrentProgress { get; private set; }

        /// <summary>
        /// Searches the given directory recursively for supported resource 
        /// files.
        /// </summary>
        /// <param name="dir">The directory to search.</param>
        /// <param name="ct">Token used to request cancellation of the task.
        /// </param>
        /// <returns>A <code>Task</code> that contains the actual search work.
        /// </returns>
        public Task SearchDirectory(DirectoryInfo dir, CancellationToken ct)
        {
            return Task.Factory.StartNew(() =>
            {
                DoSearchDirectory(dir, ct);
            }, ct);
        }

        private void DoSearchDirectory(DirectoryInfo dir, CancellationToken ct)
        {
            foreach (ScanDirectoryInfo e in dir.ScanDirectories())
            {
                if (ct.IsCancellationRequested)
                    return;
                if (e.Exception != null)
                    continue;

                this.CurrentDirectory = e.Directory;
                this.CurrentProgress = e.ProgressBefore;

                // Search the directory for supported files.
                List<FileInfo> imgs = new List<FileInfo>();
                foreach (FileInfo f in e.Directory.EnumerateFiles())
                {
                    string ext = f.Extension.ToLowerInvariant();
                    if (ext == ".pkg")
                    {
                        try
                        {
                            var pkg = new QQGame.PkgArchive(f.FullName);
                            var arg = new PackageDiscoveredEventArgs(e.Directory, pkg);
                            PackageDiscovered.RaiseMarshalled(this, arg, this.SynchronizingObject);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else if (ext == ".mif")
                    {
                        // ".bmp": // too many trivial clip arts
                        imgs.Add(f);
                    }
                }

                // If supported images are found, raise the ImagesDiscovered event.
                if (imgs.Count > 0)
                {
                    ImagesDiscovered.RaiseMarshalled(this,
                        new ResourceDiscoveredEventArgs(e.Directory, imgs.ToArray()),
                        this.SynchronizingObject);
                }
            }
        }
    }

    public class PackageDiscoveredEventArgs : EventArgs
    {
        public PackageDiscoveredEventArgs(DirectoryInfo directory, QQGame.PkgArchive package)
        {
            this.Directory = directory;
            this.Package = package;
        }
        public DirectoryInfo Directory { get; private set; }
        public QQGame.PkgArchive Package { get; private set; }
    }

    public class ResourceDiscoveredEventArgs : EventArgs
    {
        public ResourceDiscoveredEventArgs(DirectoryInfo directory, FileInfo[] files)
        {
            this.Directory = directory;
            this.Files = files;
        }
        public DirectoryInfo Directory { get; private set; }
        public FileInfo[] Files { get; private set; }
    }
}
