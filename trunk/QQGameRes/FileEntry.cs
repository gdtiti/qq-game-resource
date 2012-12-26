using System;
using System.IO;

namespace QQGameRes
{
    /// <summary>
    /// Encapsulates a physical file as a resource item.
    /// </summary>
    public class FileEntry : ResourceEntry
    {
        private FileInfo file;

        public FileEntry(FileInfo file)
        {
            this.file = file;
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string Name
        {
            get { return file.Name; }
        }

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        public int Size
        {
            get { return (int)file.Length; }
        }

        /// <summary>
        /// Opens the file for reading.
        /// </summary>
        /// <returns>A stream to read the file from.</returns>
        public Stream Open()
        {
            return file.OpenRead();
        }
    }
}
