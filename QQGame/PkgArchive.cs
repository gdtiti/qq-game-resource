using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace QQGame
{
    /// <summary>
    /// Represents a package of compressed files in the PKG archive format.
    /// </summary>
    public class PkgArchive : IDisposable
    {
        private BinaryReader reader;
        private List<PkgArchiveEntry> entries;

        /// <summary>
        /// Opens the specified archive for reading.
        /// </summary>
        /// <param name="filename">Path of the file to read.</param>
        /// <exception cref="InvalidDataException">The file format is not 
        /// supported.</exception>
        /// <exception cref="IOException">An IO error occurred.</exception>
        public PkgArchive(string filename)
        {
            this.reader = new BinaryReader(
                new FileStream(filename, FileMode.Open, FileAccess.Read));

            try
            {
                ReadHeaderAndIndexSection();
            }
            catch (Exception ex)
            {
                reader.Dispose();
                throw ex;
            }
        }

        private void ReadHeaderAndIndexSection()
        {
            // Read file header (16 bytes):
            // 0-3: file signature (0x64)
            // 4-7: number of files
            // 8-11: offset of file name area.
            // 12-15: size of file name area.
            if (reader.ReadInt32() != 0x64)
                throw new InvalidDataException("File signature mismatch.");
            uint numFiles = reader.ReadUInt32();
            uint indexOffset = reader.ReadUInt32();
            uint indexSize = reader.ReadUInt32();

            // Read file names. For each file name entry, the format is:
            // 2 bytes: (X) length of file name, in characters
            // X or 2X bytes: file name (encoded in GBK or UCS2)
            // 4 bytes: all zero; this marks the end of file name
            // 4 bytes: file offset
            // 4 bytes: original file size
            // 4 bytes: file size (packed)
            reader.BaseStream.Seek(indexOffset, SeekOrigin.Begin);
            entries = new List<PkgArchiveEntry>();
            byte[] buffer = new byte[65536 * 2];
            for (uint i = 0; i < numFiles; i++)
            {
                PkgArchiveEntryInfo info = new PkgArchiveEntryInfo();
                info.IndexOffset = (int)reader.BaseStream.Position;
                //PkgArchiveEntry entry = new PkgArchiveEntry();
                //entry.PackagePath = filename;

                ushort length = reader.ReadUInt16();
                if (reader.Read(buffer, 0, length + 4) != length + 4)
                    throw new InvalidDataException("Premature end of file.");
                if (buffer[length] == 0 && buffer[length + 1] == 0 &&
                    buffer[length + 2] == 0 && buffer[length + 3] == 0)
                {
                    // Encoded in GBK
                    info.FileName = Encoding.GetEncoding("GBK").GetString(buffer, 0, length);
                    info.IsFileNameInUnicode = false;
                }
                else
                {
                    // Encoded in UCS-2
                    if (reader.Read(buffer, length + 4, length) != length)
                        throw new InvalidDataException("Premature end of file.");
                    info.FileName = Encoding.Unicode.GetString(buffer, 0, length * 2);
                    info.IsFileNameInUnicode = true;
                }

                info.IndexSize = (int)reader.BaseStream.Position - info.IndexOffset;
                info.ContentOffset = reader.ReadInt32();
                info.OriginalSize = reader.ReadInt32();
                info.ContentSize = reader.ReadInt32();
                entries.Add(new PkgArchiveEntry(this, info));
            }
        }

        /// <summary>
        /// Gets the collection of entries that are currently in the archive.
        /// </summary>
        public ReadOnlyCollection<PkgArchiveEntry> Entries
        {
            get { return new ReadOnlyCollection<PkgArchiveEntry>(entries); }
        }

        /// <summary>
        /// Disposes the archive object and closes the underlying stream.
        /// </summary>
        public void Dispose()
        {
            this.reader.Dispose();
        }

        /// <summary>
        /// Gets the underlying stream of the archive.
        /// </summary>
        internal Stream Stream { get { return reader.BaseStream; } }
    }

    /// <summary>
    /// Contains storage-related information about an entry in a pkg archive.
    /// </summary>
    internal class PkgArchiveEntryInfo
    {
        /// <summary>
        /// Full path of the file entry in the archive.
        /// </summary>
        public string FileName;

        /// <summary>
        /// Specifies whether the FullName field is encoded in UCS-2 or in GBK.
        /// </summary>
        public bool IsFileNameInUnicode;

        /// <summary>
        /// Offset of the index entry of this file in the archive.
        /// </summary>
        public int IndexOffset;

        /// <summary>
        /// Size (in bytes) of the index entry of this file in the archive.
        /// </summary>
        public int IndexSize;

        /// <summary>
        /// Offset of the compressed content of this file in the archive.
        /// </summary>
        public int ContentOffset;

        /// <summary>
        /// Size (in bytes) of the compressed content of this file in the archive.
        /// </summary>
        public int ContentSize;

        /// <summary>
        /// Size (in bytes) of the uncompressed file.
        /// </summary>
        public int OriginalSize;
    }

    /// <summary>
    /// Represents a compressed file within a pkg archive.
    /// </summary>
    /// <remarks>
    /// The interface of this class is modeled after 
    /// <code>System.IO.Compression.ZipArchiveEntry</code>.
    /// </remarks>
    public class PkgArchiveEntry
    {
        private PkgArchive archive;
        private PkgArchiveEntryInfo info;

        internal PkgArchiveEntry(PkgArchive archive, PkgArchiveEntryInfo info)
        {
            this.archive = archive;
            this.info = info;
        }

        /// <summary>
        /// Gets the archive that the entry belongs to.
        /// </summary>
        public PkgArchive Archive { get { return archive; } }

        /// <summary>
        /// Gets the relative path of the entry in the archive.
        /// </summary>
        /// <remarks>
        /// There is no restriction on the format of the name of an entry. 
        /// Therefore this may not be a valid file system path, and care 
        /// should be taken when extracting an entry to the file system to
        /// prevent security problem.
        /// </remarks>
        public string FullName { get { return info.FileName; } }

        /// <summary>
        /// Gets the file name of the entry in the archive.
        /// </summary>
        /// <remarks>
        /// There is no restriction on the format of the name of an entry. 
        /// Therefore this may not be a valid file system path, and care 
        /// should be taken when extracting an entry to the file system to
        /// prevent security problem.
        /// </remarks>
        public string Name
        {
            get { return Path.GetFileName(this.FullName); }
        }

        /// <summary>
        /// 	Gets the compressed size of the entry in the archive.
        /// </summary>
        public int CompressedLength { get { return info.ContentSize; } }

        /// <summary>
        /// Gets the uncompressed size of the entry in the archive.
        /// </summary>
        public int Length { get { return info.OriginalSize; } }

        /// <summary>
        /// Opens the entry from the archive.
        /// </summary>
        /// <returns>A stream to read the contents of the entry from.</returns>
        
        /// <remarks>The <code>PkgArchive</code> object must not be disposed
        /// in order to access this stream.</remarks>
        /// <exception cref="InvalidDataException">The file format is not 
        /// supported.</exception>
        /// <exception cref="IOException">An IO error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The containing archive 
        /// is disposed.</exception>
        public Stream Open()
        {
            // Obtain the underlying stream of the archive.
            Stream stream = archive.Stream;

            // Validate the content range specified in the index entry.
            if (info.ContentOffset < 0)
                throw new InvalidDataException("ContentOffset must be greater than or equal to 0.");
            if (info.ContentOffset >= stream.Length)
                throw new InvalidDataException("ContentOffset must not point beyond EOF.");
            if (info.ContentSize < 6)
                throw new InvalidDataException("ContentSize must be greater than or equal to 6.");
            if (info.ContentSize > stream.Length - info.ContentOffset)
                throw new InvalidDataException("ContentSize must not exceed the remaining stream length.");

            // In the range specified by [ContentOffset, ContentSize], the
            // first two bytes is a ZLIB header and the last four bytes is
            // a ZLIB checksum. These 6 bytes are not recognized by .NET's
            // DeflateStream implementation and therefore must be skipped.

            // In addition, to restrict the stream to the range specified
            // in the index entry, we create a StreamView object.
            Util.IO.StreamView streamView = new Util.IO.StreamView(
                stream, info.ContentOffset + 2, info.ContentSize - 6);

            // Create and return a DeflateStream from here.
            return new DeflateStream(stream, CompressionMode.Decompress, true);
        }

        /// <summary>
        /// Returns the relative path of the entry in the archive.
        /// </summary>
        public override string ToString()
        {
            return this.FullName;
        }
    }
}
