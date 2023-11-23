using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.ComponentModel;

namespace FloorSimulation
{
    /// <summary>
    /// The main display to Paint on.
    /// </summary>
    internal partial class MainDisplay : Form
    {
        public Button tick_button;
        public Button ss_button;
        public Button ShowOccupiance_button;
        public TrackBar SpeedTrackBar;
        public Timer timer;
        private Floor floor;
        public Font StandardFont;
        public Font BiggerSFont;
        public Brush StandardWhiteBrush;
        public bool isSimulating = false;
        public string date = "2023-05-16";
        public MetaInfo InfoPanel;
        public ControlInfo ControlPanel;
        private ReadData rd = new ReadData();
        public string SaveFileBase;
        
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
            floor = new Floor(new Point(0, 0), this, rd);
            Controls.Add(floor);

            //ControlInfo
            ControlPanel = new ControlInfo(new Point(floor.Width, floor.Location.X), this, floor);
            Controls.Add(ControlPanel);

            InitData();

            //MetaInfo
            InfoPanel = new MetaInfo(new Point(floor.Width, (int)(2000 * Floor.ScaleFactor)), this, floor);
            Controls.Add(InfoPanel);

            Paint += PaintMainDisplay;
            SaveFileBase = date + "_" + "1Street";
        }

        private void PaintMainDisplay(object sender, PaintEventArgs e)
        {
            ControlPanel.Invalidate();
        }

        public void InvalInfo()
        {
            InfoPanel.Invalidate();
        }

        private void InitData()
        {
            List<ShopHub> shops = rd.ReadHubData(floor);

            //List<DanishTrolley> L = rd.ReadBoxHistoryToTrolleys("2023-07-18", floor, length: "top_200");
            List<DanishTrolley> L = rd.ReadBoxHistoryToTrolleys("2023-05-16", floor);

            floor.PlaceShops(rd.UsedShopHubs);
            floor.PlaceStartHubs();
            floor.PlaceBuffHubs();
            floor.PlaceFullTrolleyHubs();
            floor.DistributeTrolleys(L);
        }
    }
}
