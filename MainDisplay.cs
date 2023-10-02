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

            this.Size = new Size(1422, 800);
            this.BackColor = Color.DarkSlateGray;
            this.Text = "AllGreen Floor Simulation";

            Floor F = new Floor(new Point(0, 0), this);
            Controls.Add(F);
        }
    }
}
