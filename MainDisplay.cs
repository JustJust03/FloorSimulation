using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace FloorSimulation
{
    /// <summary>
    /// The main display to Paint on.
    /// </summary>
    public partial class MainDisplay : Form
    {
        public Button tick_button;
        public Button ss_button;
        public TrackBar SpeedTrackBar;
        public Timer timer;
        private Floor floor;
        private Font StandardFont;
        private Font BiggerSFont;
        private Brush StandardWhiteBrush;
        public bool isSimulating = false;
        public string date = "2023-07-18";
        
        public MainDisplay()
        {
            //Smoother display
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();


            this.Size = new Size(1422, 840); //40px for the top bar.
            this.BackColor = Color.DarkSlateGray;
            this.Text = "AllGreen Floor Simulation";
            StandardFont = new Font("Segoe UI", 12.0f, FontStyle.Bold);
            BiggerSFont = new Font("Segoe UI", 14.2f, FontStyle.Bold);
            StandardWhiteBrush = Brushes.White;

            //Floor
            floor = new Floor(new Point(0, 0), this);

            //Tick Button
            tick_button = new Button();
            tick_button.Text = "Tick";
            tick_button.Location = new Point(1500, 200);
            tick_button.Click += new EventHandler(floor.TickButton);

            //Start/Stop button
            InitTimer();
            ss_button = new Button();
            ss_button.Text = "Start";
            ss_button.Location = new Point(1500, 300);
            ss_button.Click += new EventHandler(ToggleSimButton);

            //Trackbar
            InitTrackBar();

            Controls.Add(floor);
            Controls.Add(tick_button);
            Controls.Add(ss_button);

            InitData();
            Paint += PaintMainDisplay;
        }

        private void PaintMainDisplay(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawString(floor.ElapsedSimTime.ToString(@"hh\:mm\:ss"), StandardFont, StandardWhiteBrush, new Point(1400, 50));
            g.DrawString("Speed Multiplier", StandardFont, StandardWhiteBrush, new Point(1500, 50));
            g.DrawString(" 1  2  3  4  5  6  7  8  9", BiggerSFont, StandardWhiteBrush, new Point(1500, 150));
        }

        private void InitData()
        {
            ReadData rd = new ReadData();
            List<ShopHub> shops = rd.ReadHubData(floor);

            List<DanishTrolley> L = rd.ReadBoxHistoryToTrolleys("2023-07-18", floor, length: "mini");
            floor.PlaceShops(rd.UsedShopHubs);
            floor.FirstStartHub.AddUndistributedTrolleys(L);
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
                Location = new Point(1500, 100),
                Width = 200,
                Minimum = 1,
                Maximum = 9,
                Value = 1,
            };
            Text = "TrackBar Example";
            SpeedTrackBar.Scroll += SpeedTBScroll;
            floor.SpeedMultiplier = SpeedTrackBar.Value;

            Controls.Add(SpeedTrackBar); // Add the TrackBar to the form
        }

        private void SpeedTBScroll(object sender, EventArgs e)
        {
            floor.SpeedMultiplier = SpeedTrackBar.Value;
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
