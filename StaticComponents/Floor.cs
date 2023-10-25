using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using FloorSimulation.StaticComponents.Hubs;

namespace FloorSimulation
{
    /// <summary>
    /// The Usable floor in AllGreen.
    /// This is a panel which hosts all objects that are on the floor in allgreen (Distributers, trolleys...).
    /// This does not necessarily indicate the size of the working streets.
    /// </summary>
    internal class Floor : SmoothPanel
    {
        public MainDisplay Display;
        readonly Color FloorColor = Color.FromArgb(255, 180, 180, 180); //Concrete gray
        public readonly Pen BPen = new Pen(Color.Black);
        public int Ticks = 0;
        public double MilisecondsPerTick;
        public TimeSpan ElapsedSimTime = TimeSpan.Zero;
        public int SpeedMultiplier;

        // Real size: 4000 cm x 4000 cm
        public const int RealFloorWidth = 4000; //cm
        public const int RealFloorHeight = 4000; //cm
        public const float ScaleFactor = 0.3f; //((Height of window - 40) / RealFloorHeight) - (800 / 2000 = 0.4)
        public string Layout;

        public List<DanishTrolley> TrolleyList; // A list with all the trolleys that are on the floor.
        public List<Hub> HubList; // A list with all the hubs that are on the floor (starthub: 0, shophubs >= 1)
        public StartHub FirstStartHub;
        public BufferHub BuffHub;
        public FullTrolleyHub FTHub;
        public TruckHub TrHub;
        public Distributer FirstDistr;
        public Distributer SecondDistr;
        public Distributer ThirdDistr;
        public Distributer FourthDistr;
        public LangeHarry FirstHarry;
        public WalkWay FirstWW;


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
            MilisecondsPerTick = (1.0 / Program.TICKS_PER_SECOND) * 1000;

            TrolleyList = new List<DanishTrolley>();
            HubList = new List<Hub>();

            FirstWW = new WalkWay(new Point(0, 0), new Size(4000, 4000), this, DevTools_: true);
            FirstStartHub = new StartHub("Start hub", 0, new Point(200, 3800), this, vertical_trolleys_: true);
            HubList.Add(FirstStartHub);
            FirstHarry = new LangeHarry(0, this, FirstWW, new Point(3500, 1700));
            FirstDistr = new Distributer(0, this, FirstWW, Rpoint_: new Point(600, 70));
            SecondDistr = new Distributer(1, this, FirstWW, Rpoint_: new Point(800, 70));
            ThirdDistr = new Distributer(2, this, FirstWW, Rpoint_: new Point(1000, 70));
            FourthDistr = new Distributer(3, this, FirstWW, Rpoint_: new Point(1200, 70));
            BuffHub = new BufferHub("Buffer hub", 1, new Point(0, 40), this);
            FTHub = new FullTrolleyHub("Full Trolley Hub", 2, new Point(400, 340), this, new Size(200, 3400));
            TrHub = new TruckHub("Truck Hub", 3, new Point(2980, 500), this);
            HubList.Add(BuffHub);
            HubList.Add(FTHub);
            HubList.Add(TrHub);

            this.Paint += PaintFloor;
            this.Invalidate();
        }

        public void TickButton(object sender, EventArgs e)
        {
            Ticks += SpeedMultiplier;
            ElapsedSimTime = ElapsedSimTime.Add(TimeSpan.FromMilliseconds(MilisecondsPerTick * SpeedMultiplier));
            FirstDistr.Tick();
            SecondDistr.Tick();
            ThirdDistr.Tick();
            FourthDistr.Tick();
            Display.Invalidate();
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

            FirstWW.DrawObject(g);
            PaintHubs(g);
            PaintTrolleys(g);
            FirstHarry.DrawObject(g);
            if(!FirstDistr.IsOnHarry)
                FirstDistr.DrawObject(g);
            if(!SecondDistr.IsOnHarry)
                SecondDistr.DrawObject(g);
            if(!ThirdDistr.IsOnHarry)
                ThirdDistr.DrawObject(g);
            if(!FourthDistr.IsOnHarry)
                FourthDistr.DrawObject(g);
        }
        

        public void PlaceShops(List<ShopHub> Shops, string shape = "S-Patern")
        {
            int UpperY = 440;
            int LowerY = 3480; // This diff should be devisable by the height of a shop (160)
            int StreetWidth = 500;
            Layout = shape;

            if (!(shape == "S-Patern"))
                throw new Exception("Haven't implemented another layout than the S layout");

            int x = 0;
            int y = LowerY;
            int two_per_row = 1; //Keeps track of how many cols are placed without space between them
            for (int i = 0; i < Shops.Count;  i++) 
            { 
                ShopHub Shop = Shops[i];
                Shop.TeleportHub(new Point(x, y));

                if (two_per_row == 1 && y > UpperY)
                    y -= 160;
                else if (two_per_row == 2 && y < LowerY)
                    y += 160;
                else
                {
                    two_per_row++;
                    if (two_per_row <= 2)
                    {
                        x += StreetWidth + 200;
                        y = UpperY;
                    }
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
            RPoint.X = (int)(RPoint.X * ScaleFactor);
            RPoint.Y = (int)(RPoint.Y * ScaleFactor);

            return RPoint;
        }
        public Size ConvertToSimSize(Size RSize)
        {
            return new Size((int)(RSize.Width * ScaleFactor), (int)(RSize.Height * ScaleFactor));
        }
        public Point ConvertToRealPoint(Point Point)
        {
            Point.X = (int)Math.Ceiling(Point.X / ScaleFactor);
            Point.Y = (int)Math.Ceiling(Point.Y / ScaleFactor);

            return Point;
        }
        public Size ConvertToRealSize(Size Size)
        {
            return new Size((int)Math.Ceiling(Size.Width / ScaleFactor), (int)Math.Ceiling(Size.Height / ScaleFactor));
        }
    }
}
