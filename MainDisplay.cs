using System.Drawing;
using System.Windows.Forms;

namespace FloorSimulation
{
    /// <summary>
    /// The main display to Paint on.
    /// </summary>
    public partial class MainDisplay : Form
    {
        public MainDisplay()
        {
            //Smoother display
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
            //Non resisable window
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            this.Size = new Size(1280, 720);
            this.BackColor = Color.FromArgb(255, 180, 180, 180);
            this.Text = "AllGreen Floor Simulation";
        }
    }
}
