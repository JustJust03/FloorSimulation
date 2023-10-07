using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace FloorSimulation
{
    /// <summary>
    /// The Usable floor in AllGreen.
    /// This is a panel which hosts all objects that are on the floor in allgreen (Distributers, trolleys...).
    /// This does not necessarily indicate the size of the working streets.
    /// </summary>
    internal class Floor : SmoothPanel
    {
        MainDisplay Display;
        readonly Color FloorColor = Color.FromArgb(255, 180, 180, 180); //Concrete gray
        public readonly Pen BPen = new Pen(Color.Black);

        // Real size: 4000 cm x 4000 cm
        public const int RealFloorWidth = 2000; //cm
        public const int RealFloorHeight = 2000; //cm
        public const float ScaleFactor = 0.4f; //((Height of window - 40) / RealFloorHeight) - (800 / 2000 = 0.4)


        private List<DanishTrolley> TrolleyList; // A list with all the trolleys that are on the floor.
        private ShopHub FirstShop;
        private StartHub FirstStartHub;
        private Distributer FirstDistr;
        private WalkWay FirstWW;

        /// <summary>
        /// Sets the pixel floor size by using the ScaleFactor.
        /// </summary>
        /// <param name="PanelLocation">Where should the panel be drawn from (topleft)</param>
        /// <param name="di">On which display is this being drawn</param>
        public Floor(Point PanelLocation, MainDisplay di)
        {
            Display = di;

            Size PixelFloorSize = new Size((int)(RealFloorWidth * ScaleFactor),
                                           (int)(RealFloorHeight * ScaleFactor));
            this.Location = PanelLocation;
            this.Size = PixelFloorSize;
            this.BackColor = FloorColor;

            TrolleyList = new List<DanishTrolley>();

            FirstShop = new ShopHub("IKEA", 1, new Point(0, 0), this, 2, AccPoint_: new Point(200, 90));
            FirstStartHub = new StartHub("Start hub", 0, new Point(1000, 1800), this, initial_trolleys_: 5, vertical_trolleys_: true);
            FirstWW = new WalkWay(new Point(200, 0), new Size(800, 2000), this);
            FirstDistr = new Distributer(0, this, FirstWW, Rpoint_: new Point(200, 70));

            FirstDistr.TravelTo(null);

            this.Paint += PaintTrolleys;
            this.Invalidate();
        }

        public void TickButton(object sender, EventArgs e)
        {
            FirstDistr.Tick();
            Invalidate();
        }

        /// <summary>
        /// Paints all trolleys that are in the trolleylist, thus all trolleys that are on the floor.
        /// </summary>
        public void PaintTrolleys(object obj, PaintEventArgs pea)
        {
            Graphics g = pea.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach (DanishTrolley t in TrolleyList)
            {
                t.DrawObject(g);
            }

            FirstWW.DrawObject(g);
            FirstShop.DrawHub(g, true);
            FirstStartHub.DrawHub(g, true);
            FirstDistr.DrawObject(g);
        }

        public Point ConvertToSimPoint(Point RPoint)
        {
            return new Point((int)(RPoint.X * ScaleFactor), (int)(RPoint.Y * ScaleFactor));
        }
        public Size ConvertToSimSize(Size RSize)
        {
            return new Size((int)(RSize.Width * ScaleFactor), (int)(RSize.Height * ScaleFactor));
        }
    }
}
