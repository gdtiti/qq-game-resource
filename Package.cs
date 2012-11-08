using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace QQGameRes
{
    public struct PackageEntry
    {
        public string Path;
        public uint Offset;
        public uint Size;
        public uint OriginalSize;
    }

    public class Package
    {
        private FileStream stream;
        private BinaryReader reader;
        List<PackageEntry> entries;

        public Package(string filename)
        {
            stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            reader = new BinaryReader(stream);

            // Read file header (16 bytes):
            // 0-3: file signature (0x64)
            // 4-7: number of files
            // 8-11: offset of file name area.
            // 12-15: size of file name area.
            if (reader.ReadInt32() != 0x64)
                throw new IOException("File signature mismatch.");
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
            stream.Seek(indexOffset, SeekOrigin.Begin);
            entries = new List<PackageEntry>();
            byte[] buffer = new byte[65536 * 2];
            for (uint i = 0; i < numFiles; i++)
            {
                PackageEntry entry;

                ushort length = reader.ReadUInt16();
                if (reader.Read(buffer, 0, length + 4) != length + 4)
                    throw new IOException("Premature end of file.");
                if (buffer[length] == 0 && buffer[length + 1] == 0 &&
                    buffer[length + 2] == 0 && buffer[length + 3] == 0)
                {
                    // Encoded in GBK
                    entry.Path = Encoding.GetEncoding("GBK").GetString(buffer, 0, length);
                }
                else
                {
                    // Encoded in UCS-2
                    if (reader.Read(buffer, length + 4, length) != length)
                        throw new IOException("Premature end of file.");
                    entry.Path = Encoding.Unicode.GetString(buffer, 0, length * 2);
                }

                entry.Offset = reader.ReadUInt32();
                entry.OriginalSize = reader.ReadUInt32();
                entry.Size = reader.ReadUInt32();
                entries.Add(entry);
            }
        }

        public int EntryCount
        {
            get { return entries.Count; }
        }

        public PackageEntry GetEntry(int index)
        {
            return entries[index];
        }

        public Stream ExtractEntry(int index)
        {
            uint offset = entries[index].Offset;
            uint size = entries[index].Size;
            if (size > stream.Length)
                throw new IOException("Invalid size field.");

            // Seek to file content and skip two bytes of ZLIB header
            // which is not recognized by .NET's DeflateStream impl.
            stream.Seek(offset + 2, SeekOrigin.Begin);
            
            // Create and return a DeflateStream from here.
            return new DeflateStream(stream, CompressionMode.Decompress, true);
        }
    }
}
