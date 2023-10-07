using System;
using System.Drawing;
using System.Windows.Forms;

namespace FloorSimulation
{
    /// <summary>
    /// The main display to Paint on.
    /// </summary>
    public partial class MainDisplay : Form
    {
        public Button tick_button;
        public Button ss_button;
        public Timer timer;
        private Floor F;
        private bool isSimulating = false;
        
        public MainDisplay()
        {
            //Smoother display
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
            //Non resisable window
            this.FormBorderStyle = FormBorderStyle.FixedSingle;


            this.Size = new Size(1422, 840); //40px for the top bar.
            this.BackColor = Color.DarkSlateGray;
            this.Text = "AllGreen Floor Simulation";

            //Floor
            F = new Floor(new Point(0, 0), this);

            //Tick Button
            tick_button = new Button();
            tick_button.Text = "Tick";
            tick_button.Location = new Point(1000, 200);
            tick_button.Click += new EventHandler(F.TickButton);

            //Start/Stop button
            InitTimer();
            ss_button = new Button();
            ss_button.Text = "Start";
            ss_button.Location = new Point(1000, 400);
            ss_button.Click += new EventHandler(ToggleSimButton);

            Controls.Add(F);
            Controls.Add(tick_button);
            Controls.Add(ss_button);
        }

        private void InitTimer()
        {
            timer = new Timer();
            timer.Interval = 30;
            timer.Tick += F.TickButton;
        }

        private void ToggleSimButton(object sender, EventArgs e)
        {
            if (isSimulating)
            {
                timer.Stop();
                ss_button.Text = "Start";
            }
            else
            {
                timer.Start();
                ss_button.Text = "Stop";
            }
            isSimulating = !isSimulating;
        }
    }
}
