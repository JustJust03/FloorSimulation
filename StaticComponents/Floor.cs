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
        public readonly Pen BluePen = new Pen(Color.Blue);
        public readonly Pen YellowPen = new Pen(Color.Yellow);
        public int Ticks = 0;
        public double MilisecondsPerTick;
        public TimeSpan ElapsedSimTime = TimeSpan.Zero;
        public int SpeedMultiplier;
        public FinishedDistribution FinishedD;
        public Random rand;

        // Real size: 4000 cm x 4000 cm
        public const int RealFloorWidth = 5000; //cm
        public const int RealFloorHeight = 4000; //cm
        public const float ScaleFactor = 0.25f; //((Height of window - 40) / RealFloorHeight) - (800 / 2000 = 0.4)
        public string Layout;

        public List<DanishTrolley> TrolleyList; // A list with all the trolleys that are on the floor.
        public List<Hub> HubList; // A list with all the hubs that are on the floor (starthub: 0, shophubs >= 1)
        public StartHub FirstStartHub;
        public BufferHub BuffHub;
        public List<FullTrolleyHub> FTHubs;
        public FullTrolleyHub FTHub0;
        public FullTrolleyHub FTHub1;
        public FullTrolleyHub FTHub2;
        public FullTrolleyHub FTHub3;
        public TruckHub TrHub;
        public List<Distributer> DistrList; // A list with all the distributers that are on the floor.
        public Distributer FirstDistr;
        public Distributer SecondDistr;
        public Distributer ThirdDistr;
        public Distributer FourthDistr;
        public Distributer FiveDistr;
        public Distributer SixDistr;
        public Distributer SevenDistr;
        public Distributer EightDistr;
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
            FinishedD = new FinishedDistribution(this);
            rand = new Random(0);

            TrolleyList = new List<DanishTrolley>();
            HubList = new List<Hub>();

            FirstWW = new WalkWay(new Point(0, 0), new Size(RealFloorWidth, RealFloorHeight), this, DevTools_: false);
            FirstStartHub = new StartHub("Start hub", 0, new Point(200, 3750), this, vertical_trolleys_: true);
            HubList.Add(FirstStartHub);
            FirstHarry = new LangeHarry(0, this, FirstWW, new Point(4500, 1700));

            DistrList = new List<Distributer>();
            FirstDistr = new Distributer(0, this, FirstWW, Rpoint_: new Point(600, 70));
            SecondDistr = new Distributer(1, this, FirstWW, Rpoint_: new Point(100, 70));
            ThirdDistr = new Distributer(2, this, FirstWW, Rpoint_: new Point(1500, 70));
            FourthDistr = new Distributer(3, this, FirstWW, Rpoint_: new Point(2400, 70));
            FiveDistr = new Distributer(4, this, FirstWW, Rpoint_: new Point(600, 1500));
            SixDistr = new Distributer(5, this, FirstWW, Rpoint_: new Point(600, 2200));
            SevenDistr = new Distributer(6, this, FirstWW, Rpoint_: new Point(600, 3000));
            EightDistr = new Distributer(7, this, FirstWW, Rpoint_: new Point(600, 3600));
            DistrList.Add(FirstDistr);
            DistrList.Add(SecondDistr);
            DistrList.Add(ThirdDistr);
            DistrList.Add(FourthDistr);
            DistrList.Add(FiveDistr);
            DistrList.Add(SixDistr);
            DistrList.Add(SevenDistr);
            DistrList.Add(EightDistr);

            FTHubs = new List<FullTrolleyHub>();
            FTHub0 = new FullTrolleyHub("Full Trolley Hub", 2, new Point(400, 440), this, new Size(200, 3000));
            FTHub1 = new FullTrolleyHub("Full Trolley Hub", 3, new Point(1350, 440), this, new Size(200, 3000));
            FTHub2 = new FullTrolleyHub("Full Trolley Hub", 4, new Point(2300, 440), this, new Size(200, 3000));
            FTHub3 = new FullTrolleyHub("Full Trolley Hub", 5, new Point(3250, 440), this, new Size(200, 3000));
            FTHubs.Add(FTHub0);
            FTHubs.Add(FTHub1);
            FTHubs.Add(FTHub2);
            FTHubs.Add(FTHub3);
            foreach (FullTrolleyHub h in FTHubs)
                HubList.Add(h);

            BuffHub = new BufferHub("Buffer hub", 1, new Point(0, 40), this);
            TrHub = new TruckHub("Truck Hub", 6, new Point(3980, 500), this);
            HubList.Add(BuffHub);
            HubList.Add(TrHub);



            this.Paint += PaintFloor;
            this.Invalidate();
        }

        public void TickButton(object sender, EventArgs e)
        {
            Ticks += SpeedMultiplier;
            ElapsedSimTime = ElapsedSimTime.Add(TimeSpan.FromMilliseconds(MilisecondsPerTick * SpeedMultiplier));
            foreach (Distributer d in DistrList)
                d.Tick();
            Display.Invalidate();
            Invalidate();
        }

        public void DrawOccupiance(object sender, EventArgs e)
        {
            FirstWW.DevTools = !FirstWW.DevTools;
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
            foreach (Distributer d in DistrList)
                if (!d.IsOnHarry)
                    d.DrawObject(g);
        }

        public void PlaceShops(List<ShopHub> Shops, string shape = "S-Patern")
        {
            int UpperY = 440;
            int LowerY = 3480; // This diff should be devisable by the height of a shop (160)
            int StreetWidth = 600;
            Layout = shape;

            if (!(shape == "S-Patern"))
                throw new Exception("Haven't implemented another layout than the S layout");

            int x = 0;
            int y = LowerY;
            int two_per_row = 1; //Keeps track of how many cols are placed without space between them
            int placed_shops_in_a_row = 0;
            for (int i = 0; i < Shops.Count;  i++) 
            { 
                ShopHub Shop = Shops[i];
                Shop.TeleportHub(new Point(x, y));

                placed_shops_in_a_row++;
                if (placed_shops_in_a_row == 9)
                {
                    placed_shops_in_a_row = 0;
                    if (two_per_row == 1)
                        y -= 320;
                    else
                        y += 320;
                }

                if (two_per_row == 1 && y > UpperY)
                    y -= 160;
                else if (two_per_row == 2 && y < LowerY)
                    y += 160;
                else
                {
                    placed_shops_in_a_row = 0;
                    two_per_row++;
                    if (two_per_row <= 2)
                    {
                        x += StreetWidth + 200;
                        y = UpperY;
                    }
                    else
                    {
                        y -= 320; // Because the middle section was placed at the bottom
                        x += 160;
                        two_per_row = 1;
                    }
                }

                HubList.Add(Shop);
            }
        }

        /// <summary>
        /// Returns the closes FullTrolley Hub to the distributer
        /// </summary>
        public FullTrolleyHub ClosestFTHub(Distributer db)
        {
            FullTrolleyHub ClosestHub = FTHubs[0];
            foreach (FullTrolleyHub FThub in FTHubs)
                if (Math.Abs(db.RDPoint.X + 150 - FThub.RFloorPoint.X) < Math.Abs(db.RDPoint.X + 150 - ClosestHub.RFloorPoint.X))
                    ClosestHub = FThub;

            return ClosestHub;
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
