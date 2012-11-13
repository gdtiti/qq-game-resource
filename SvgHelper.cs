using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing.Imaging;

namespace QQGameRes
{
    public class SvgHelper
    {
        /// <summary>
        /// Saves an animated image in SVG file format.
        /// </summary>
        /// <param name="img">The image to save.</param>
        /// <param name="filename">The file to save.</param>
        public static void SaveAnimation(AnimationImage img, string filename)
        {
            using (StreamWriter writer = new StreamWriter(
                new FileStream(filename, FileMode.Create, FileAccess.Write)))
            {
                // Write SVG header.
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<svg xmlns=\"http://www.w3.org/2000/svg\"" +
                                 " xmlns:xlink=\"http://www.w3.org/1999/xlink\">");

                // Write each image in PNG format.
                int timing = 0;
                for (int i = 1; i <= img.FrameCount; i++)
                {
                    if (!img.GetNextFrame())
                        break;

                    // The frame is initially set to invisible.
                    writer.WriteLine("  <g visibility=\"{0}\">",
                                     i == 1 ? "visible" : "hidden");

                    // SMIL animation to display the image.
                    if (i > 1)
                    {
                        writer.WriteLine(
                            "    <animate attributeName=\"visibility\"" +
                            " attributeType=\"XML\" calcMode=\"discrete\"" +
                            " begin=\"" + timing + "ms\" to=\"visible\"/>");
                    }

                    // SMIL animation to hide the image.
                    timing += img.CurrentFrame.Delay;
                    if (i < img.FrameCount)
                    {
                        writer.WriteLine(
                            "    <animate attributeName=\"visibility\"" +
                            " attributeType=\"XML\" calcMode=\"discrete\"" +
                            " begin=\"" + timing + "ms\" to=\"hidden\"/>");
                    }

                    // Write embedded PNG file.
                    writer.Write("    <image width=\"" + img.Width + "\" height=\"" +
                        img.Height + "\" xlink:href=\"data:image/png;base64,");

                    using (MemoryStream mem = new MemoryStream())
                    {
                        img.CurrentFrame.Image.Save(mem, ImageFormat.Png);
                        writer.Write(Convert.ToBase64String(mem.GetBuffer(), 0, (int)mem.Length));
                    }
                    writer.WriteLine("\"/>");
                    writer.WriteLine("  </g>");
                }

                // Finish SVG file.
                writer.WriteLine("</svg>");
            }
        }
    }
}
