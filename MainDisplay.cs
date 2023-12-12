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
        //public string date = "2023-04-14";
        public string date = "2023-07-18";
        public List<string> days = new List<string> { "DI", "WO" };
        //public List<string> days = new List<string> { "VR"};

        public MetaInfo InfoPanel;
        public ControlInfo ControlPanel;
        private ReadData rd;
        public string SaveFileBase;

        public bool LoadHeatMap = false;
        public string HeatMapName = "2023-07-18_HeatMapTesting";


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
            rd = new ReadData(days);
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
            SaveFileBase = date + "_" + "HeatMapTesting";
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

            //List<DanishTrolley> L = rd.ReadBoxHistoryToTrolleys(date, floor, length: "top_200");
            //List<DanishTrolley> L = rd.ReadBoxHistoryToTrolleys(date, floor);
            //List<DanishTrolley> L = rd.ReadBoxHistoryToTrolleys(date, floor, DistributeSecondDay: false);
            List<DanishTrolley> L = rd.ReadBoxHistoryToTrolleys(date, floor, length: "for_30", DistributeSecondDay: false);

            floor.PlaceShops(rd.UsedShopHubs);
            floor.PlaceStartHubs();
            floor.PlaceBuffHubs();
            floor.PlaceFullTrolleyHubs();
            floor.AssignRegions(L);
            floor.DistributeTrolleys(L);

            if (LoadHeatMap)
                rd.LoadHeatMap(HeatMapName, floor.FirstWW);
        }
    }
}
