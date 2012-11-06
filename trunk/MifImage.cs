using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

namespace QQGameRes
{
    public struct MifHeader
    {
        public int Version;
        public int ImageWidth;
        public int ImageHeight;
        public int Type;
        public int LayerCount;
    }

    public class MifImage
    {
        public static Image[] Load(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                // Read MIF header.
                MifHeader header;
                header.Version = reader.ReadInt32();
                header.ImageWidth = reader.ReadInt32();
                header.ImageHeight = reader.ReadInt32();
                header.Type = reader.ReadInt32();
                header.LayerCount = reader.ReadInt32();

                // Valid header fields.
                if (header.Version != 0 && header.Version != 1)
                    throw new IOException("MIF version " + header.Version + " is not supported.");
                if (header.ImageWidth <= 0)
                    throw new IOException("ImageWidth field must be positive.");
                if (header.ImageHeight <= 0)
                    throw new IOException("ImageHeight field must be positive.");
                if (header.Type != 3 && header.Type != 7)
                    throw new IOException("MIF type " + header.Type + " is not supported.");
                if (header.LayerCount <= 0)
                    throw new IOException("LayerCount field must be positive.");

                // Read each image in turn.
                int width = header.ImageWidth;
                int height = header.ImageHeight;
                int bytesPerImage = width * height * 3;
                byte[] buffer = new byte[bytesPerImage];
                List<Image> images = new List<Image>();
                for (int i = 0; i < header.LayerCount; i++)
                {
                    // Skip four bytes if Type is 7.
                    if (header.Type == 7)
                        reader.ReadInt32();

                    // Load image into memory buffer.
                    if (reader.Read(buffer, 0, bytesPerImage) != bytesPerImage)
                        throw new IOException("Premature end of file.");

                    // Create a bitmap and set its pixels accordingly.
                    Bitmap bmp = new Bitmap(width, height);
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte a = buffer[2 * width * height + y * width + x];
                            byte b1 = buffer[2 * (y * width + x)];
                            byte b2 = buffer[2 * (y * width + x) + 1];
                            Color c = Color.FromArgb(
                                (a >= 32) ? 255 : (a << 3), // alpha
                                b2 & 0xF8,                  // red
                                ((b2 << 5) | (b1 >> 3)) & 0xFC,// green
                                (b1 & 0x1F) << 3);          // blue
                            bmp.SetPixel(x, y, c);
                        }
                    }
                    images.Add(bmp);
                }
                return images.ToArray();
            }
        }
    }
}
