using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FloorSimulation
{
    internal class ControlInfo : SmoothPanel
    {
        Floor floor;
        MainDisplay Display;
        public Button tick_button;
        public Button ss_button;
        public Button ShowOccupiance_button;
        public TrackBar SpeedTrackBar;
        public Timer timer;

        public ControlInfo(Point PanelLocation, MainDisplay di, Floor floor_)
        {
            Display = di;
            floor = floor_;

            this.Location = PanelLocation;
            this.Size = new Size((int)(2000 * Floor.ScaleFactor), (int)(2000 * Floor.ScaleFactor));

            //Tick Button
            tick_button = new Button();
            tick_button.Text = "Tick";
            tick_button.ForeColor = Color.White;
            tick_button.Location = new Point(5, 150);
            tick_button.Click += new EventHandler(floor.TickButton);

            //ShowOccupiance Button
            ShowOccupiance_button = new Button();
            ShowOccupiance_button.Text = "Draw Occupiance";
            ShowOccupiance_button.ForeColor = Color.White;
            ShowOccupiance_button.Location = new Point(5, 200);
            ShowOccupiance_button.Click += new EventHandler(floor.DrawOccupiance);

            //Timer
            InitTimer();

            //Start/Stop button
            ss_button = new Button();
            ss_button.Text = "Start";
            ss_button.ForeColor = Color.White;
            ss_button.Location = new Point(100, 150);
            ss_button.Click += new EventHandler(ToggleSimButton);

            //Trackbar
            InitTrackBar();

            Controls.Add(tick_button);
            Controls.Add(ShowOccupiance_button);
            Controls.Add(ss_button);

            Paint += PaintControls;
        }

        private void InitTimer()
        {
            timer = new Timer();
            timer.Interval = Program.TICKS_PER_SECOND;
            timer.Tick += floor.TickButton;
        }

        private void InitTrackBar()
        {
            SpeedTrackBar = new TrackBar
            {
                Location = new Point(100, 50),
                Width = this.Width - 120,
                Minimum = 1,
                Maximum = 32,
                Value = 1,
            };
            Text = "TrackBar Example";
            SpeedTrackBar.Scroll += SpeedTBScroll;
            floor.SpeedMultiplier = SpeedTrackBar.Value;

            Controls.Add(SpeedTrackBar);
        }

        private void PaintControls(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawString(floor.ElapsedSimTime.ToString(@"hh\:mm\:ss"), Display.StandardFont, Display.StandardWhiteBrush, new Point(5, 5));
            g.DrawString("Speed Multiplier", Display.StandardFont, Display.StandardWhiteBrush, new Point(100, 5));

            g.DrawString(SpeedTrackBar.Minimum.ToString(), Display.StandardFont, Display.StandardWhiteBrush, new Point(120, 90));
            g.DrawString(SpeedTrackBar.Maximum.ToString(), Display.StandardFont, Display.StandardWhiteBrush, new Point(Width - 40, 90));
        }

        private void SpeedTBScroll(object sender, EventArgs e)
        {
            floor.SpeedMultiplier = SpeedTrackBar.Value;
        }


        private void ToggleSimButton(object sender, EventArgs e)
        {
            if (Display.isSimulating)
            {
                timer.Stop();
                ss_button.Text = "Start";
            }
            else
            {
                timer.Start();
                ss_button.Text = "Stop";
            }
            Display.isSimulating = !Display.isSimulating;
        }

    }
}
