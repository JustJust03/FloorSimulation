using System.Windows.Forms;


namespace FloorSimulation
{
    /// <summary>
    /// A panel with double buffering enabled and flickering reduced.
    /// </summary>
    public class SmoothPanel : Panel
    {
        public SmoothPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }
    }
}
