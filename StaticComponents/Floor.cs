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

        private int Nshops = 16;

        private List<DanishTrolley> TrolleyList; // A list with all the trolleys that are on the floor.
        public List<Hub> HubList; // A list with all the hubs that are on the floor (starthub: 0, shophubs >= 1)
        public StartHub FirstStartHub;
        public BufferHub BuffHub;
        public Distributer FirstDistr;
        //public Distributer SecondDistr;
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
            HubList = new List<Hub>();

            FirstWW = new WalkWay(new Point(0, 0), new Size(2000, 2000), this);
            FirstStartHub = new StartHub("Start hub", 0, new Point(200, 1800), this, FirstWW, initial_trolleys_: 5, vertical_trolleys_: true);
            HubList.Add(FirstStartHub);
            FirstDistr = new Distributer(0, this, FirstWW, Rpoint_: new Point(600, 70));
            //SecondDistr = new Distributer(0, this, FirstWW, Rpoint_: new Point(800, 70));
            BuffHub = new BufferHub("Buffer hub", 1, new Point(0, 20), this, FirstWW);
            HubList.Add(BuffHub);
            init_shops();

            FirstStartHub.InitFirstTrolley();

            this.Paint += PaintFloor;
            this.Invalidate();
        }

        public void TickButton(object sender, EventArgs e)
        {
            FirstDistr.Tick();
            //SecondDistr.Tick();
            Invalidate();
        }

        /// <summary>
        /// Paints all trolleys that are in the trolleylist, thus all trolleys that are on the floor.
        /// </summary>
        public void PaintTrolleys(Graphics g)
        {
            foreach (DanishTrolley t in TrolleyList)
                t.DrawObject(g);
        }

        /// <summary>
        /// Paints all the hubs that are in hublist
        /// </summary>
        public void PaintHubs(Graphics g)
        {
            foreach (Hub h in HubList)
                h.DrawHub(g, DrawOutline: true);
        }

        /// <summary>
        /// Paints all the objects that should be on the floor
        /// </summary>
        public void PaintFloor(object obj, PaintEventArgs pea)
        {
            Graphics g = pea.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            FirstWW.DrawObject(g, DrawOccupiance: true);
            PaintHubs(g);
            PaintTrolleys(g);
            FirstDistr.DrawObject(g);
            //SecondDistr.DrawObject(g);
        }
        
        /// <summary>
        /// Lays down the shops in rows with 4 m in between them
        /// </summary>
        public void init_shops()
        {
            int UpperY = 340;
            int LowerY = 1400;
            int StreetWidth = 300;

            int x = 0;
            int y = UpperY;
            int two_per_row = 1; //Keeps track of how many cols are placed without space between them
            for (int i = 0; i < Nshops;  i++) 
            { 
                Hub Shop = new ShopHub("Shop: " + i, 1, new Point(x, y), this, FirstWW, initial_trolleys: 2);
                if (y < LowerY)
                    y += 160;
                else
                {
                    y = UpperY;

                    two_per_row++;
                    if (two_per_row <= 2) 
                        x += StreetWidth + 200;
                    else
                    {
                        x += 160;
                        two_per_row = 1;
                    }
                }
                HubList.Add(Shop);
            }
        }

        public Point ConvertToSimPoint(Point RPoint)
        {
            return new Point((int)(RPoint.X * ScaleFactor), (int)(RPoint.Y * ScaleFactor));
        }
        public Size ConvertToSimSize(Size RSize)
        {
            return new Size((int)(RSize.Width * ScaleFactor), (int)(RSize.Height * ScaleFactor));
        }
        public Point ConvertToRealPoint(Point Point)
        {
            return new Point((int)Math.Ceiling(Point.X / ScaleFactor), (int)Math.Ceiling(Point.Y / ScaleFactor));
        }
        public Size ConvertToRealSize(Size Size)
        {
            return new Size((int)Math.Ceiling(Size.Width / ScaleFactor), (int)Math.Ceiling(Size.Height / ScaleFactor));
        }
    }
}
