using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace QQGameRes
{
    /// <summary>
    /// Custom-draws a list view item.
    /// </summary>
    class ListViewItemDrawer
    {
        public ListViewItem Item { get; set; }
        public Rectangle Bounds { get; set; }
        public Graphics Graphics { get; set; }

        private const int ListViewImageMargin = 5;
        private const int ListViewItemBorder = 1;
        private const int ListViewItemMargin = 1;

        public ListViewItemDrawer(ListViewItem item, Rectangle bounds, Graphics g)
        {
            this.Item = item;
            this.Bounds = bounds;
            this.Graphics = g;
        }

        /// <summary>
        /// Draws an image as the large icon of the item.
        /// </summary>
        /// <param name="img">The image to draw.</param>
        public void DrawImage(Image img)
        {
            Rectangle bounds = this.Bounds;

            Size frameSize = this.Item.ImageList.ImageSize;
            frameSize.Width -= ListViewImageMargin * 2;
            frameSize.Height -= ListViewImageMargin * 2;

            // Reduce the bound width to that of the image frame.
            bounds.X += (bounds.Width - frameSize.Width) / 2;
            bounds.Width = frameSize.Width;

            // Allow 1 pixel for the border, 1 pixel for item margin, and
            // 5 pixels for image margin.
            bounds.Y += ListViewItemBorder + ListViewItemMargin + ListViewImageMargin;
            bounds.Height = frameSize.Height;

            // Draw the frame around the image.
            int spacing = 0;
            this.Graphics.DrawRectangle(Pens.DarkGray,
                bounds.X - 1 - spacing, bounds.Y - 1 - spacing,
                bounds.Width + 1 + 2 * spacing, bounds.Height + 1 + 2 * spacing);

            // Fit the image into the frame, keeping scale.
            Size sz = img.Size;
            if (sz.Width > bounds.Width)
            {
                sz.Height = sz.Height * bounds.Width / sz.Width;
                sz.Width = bounds.Width;
            }
            if (sz.Height > bounds.Height)
            {
                sz.Width = sz.Width * bounds.Height / sz.Height;
                sz.Height = bounds.Height;
            }
            bounds.X += (bounds.Width - sz.Width) / 2;
            bounds.Y += (bounds.Height - sz.Height) / 2;
            bounds.Width = sz.Width;
            bounds.Height = sz.Height;

            // Draw the image.
            this.Graphics.DrawImage(img, bounds);
        }

        public void DrawText()
        {
            Rectangle bounds = this.Bounds;

            // Allow 1 pixel for border and 1 pixel for margin.
            int n = ListViewItemBorder + ListViewItemMargin;
            bounds.Y += n;

            // Skip the image height, and leave 2 pixels in between.
            int h = this.Item.ImageList.ImageSize.Height;
            bounds.Y += h;
            bounds.Height -= h;

            // Now draw the text single-line and centered.
            TextRenderer.DrawText(this.Graphics, this.Item.Text, 
                this.Item.Font, bounds, SystemColors.WindowText,
                TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter |
                TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
        }

        public void DrawBorder()
        {
            Rectangle bounds = this.Bounds;

            bounds.Width -= 1;
            bounds.Height -= 1;
            this.Graphics.DrawRectangle(Pens.SkyBlue, bounds);

            bounds.X += 1;
            bounds.Y += 1;
            bounds.Width -= 2;
            bounds.Height -= 2;
            this.Graphics.DrawRectangle(Pens.SkyBlue, bounds);
        }

        // Load the Play icon from the resource and alpha blend it.
        private static Bitmap LoadPlayIcon()
        {   
            Bitmap bmp = QQGameRes.Properties.Resources.Play_Icon_48;
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color c = bmp.GetPixel(x, y);
                    bmp.SetPixel(x, y, Color.FromArgb(c.A / 2, c));
                }
            }
            return bmp;
        }

        private static Bitmap PlayIcon = LoadPlayIcon();

        public void DrawPlayIcon()
        {
            Size frameSize = this.Item.ImageList.ImageSize;
            Rectangle bounds = this.Bounds;

            // Allow 1 pixel for the border and 1 pixel for item margin.
            bounds.Y += ListViewItemBorder + ListViewItemMargin;
            bounds.Height = frameSize.Height;

            // Center the image in the bounds.
            bounds.X += (bounds.Width - PlayIcon.Width) / 2;
            bounds.Y += (bounds.Height - PlayIcon.Height) / 2;
            bounds.Width = PlayIcon.Width;
            bounds.Height = PlayIcon.Height;
            this.Graphics.DrawImageUnscaled(PlayIcon, bounds);
        }
    }
}
