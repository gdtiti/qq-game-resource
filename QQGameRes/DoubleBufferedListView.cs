using System;

namespace QQGameRes
{
    public class DoubleBufferedListView : System.Windows.Forms.ListView
    {
        public DoubleBufferedListView()
        {
            base.DoubleBuffered = true;
        }
    }
}
