using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using FloorSimulation.StaticComponents.Hubs;
using System.Linq;

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

        // Real size: 5000 cm x 5000 cm
        public const int RealFloorWidth = 5000; //cm
        public const int RealFloorHeight = 5000; //cm
        public const float ScaleFactor = 0.25f; //((Height of window - 40) / RealFloorHeight) - (800 / 2000 = 0.4)
        public Layout layout;

        public List<DanishTrolley> TrolleyList; // A list with all the trolleys that are on the floor.
        public List<Hub> HubList; // A list with all the hubs that are on the floor (starthub: 0, shophubs >= 1)
        public List<StartHub> STHubs;
        public List<BufferHub> BuffHubs;
        public List<FullTrolleyHub> FTHubs;
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
        public Floor(Point PanelLocation, MainDisplay di, ReadData rd)
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
            //layout = new SLayoutDayId(this, rd);
            layout = new SLayoutDayIdBuffhub(this, rd);

            TrolleyList = new List<DanishTrolley>();
            HubList = new List<Hub>();
            STHubs = new List<StartHub>();
            FTHubs = new List<FullTrolleyHub>();

            FirstWW = new WalkWay(new Point(0, 0), new Size(RealFloorWidth, RealFloorHeight), this, DevTools_: false);

            FirstHarry = new LangeHarry(0, this, FirstWW, new Point(4500, 1700));

            DistrList = new List<Distributer>();
            FirstDistr = new Distributer(0, this, FirstWW, Rpoint_: new Point(4000, 3000));
            SecondDistr = new Distributer(1, this, FirstWW, Rpoint_: new Point(4100, 3000));
            ThirdDistr = new Distributer(2, this, FirstWW, Rpoint_: new Point(4200, 3000));
            FourthDistr = new Distributer(3, this, FirstWW, Rpoint_: new Point(4300, 3000));
            FiveDistr = new Distributer(4, this, FirstWW, Rpoint_: new Point(4400, 3000));
            SixDistr = new Distributer(5, this, FirstWW, Rpoint_: new Point(4500, 3000));
            SevenDistr = new Distributer(6, this, FirstWW, Rpoint_: new Point(4600, 3000));
            EightDistr = new Distributer(7, this, FirstWW, Rpoint_: new Point(4700, 3000));
            DistrList.Add(FirstDistr);
            DistrList.Add(SecondDistr);
            DistrList.Add(ThirdDistr);
            DistrList.Add(FourthDistr);
            DistrList.Add(FiveDistr);
            DistrList.Add(SixDistr);
            DistrList.Add(SevenDistr);
            DistrList.Add(EightDistr);

            TrHub = new TruckHub("Truck Hub", 6, new Point(4230, 700), this);
            HubList.Add(TrHub);

            BuffHubs = new List<BufferHub>();

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

        public void PlaceShops(List<ShopHub> Shops)
        {
            layout.PlaceShops(Shops, 840, 4070);
        }

        public void PlaceStartHubs()
        {
            layout.PlaceStartHubs();
        }

        public void PlaceBuffHubs()
        {
            layout.PlaceBuffHubs();
        }

        public void PlaceFullTrolleyHubs()
        {
            layout.PlaceFullTrolleyHubs();
        }

        public void DistributeTrolleys(List<DanishTrolley> dtList)
        {
            layout.DistributeTrolleys(dtList);
        }

        /// <summary>
        /// Returns the closes FullTrolley Hub to the distributer
        /// </summary>
        public FullTrolleyHub ClosestFTHub(Distributer db)
        {
            FullTrolleyHub ClosestHub = FTHubs[0];
            foreach (FullTrolleyHub FThub in FTHubs)
                if (Math.Abs(db.RDPoint.X - FThub.RFloorPoint.X) < Math.Abs(db.RDPoint.X - ClosestHub.RFloorPoint.X))
                    ClosestHub = FThub;

            return ClosestHub;
        }

        public StartHub GetStartHub(Distributer db)
        {
            return layout.GetStartHub(db);
        }

        public BufferHub GetBuffHubFull(Distributer db)
        {
            return layout.GetBuffHubFull(db);
        }

        public BufferHub GetBuffHubOpen(Distributer db)
        {
            return layout.GetBuffHubOpen(db);
        }

        public int TotalUndistributedTrolleys()
        {
            return STHubs.Select(sh => sh.TotalUndistributedTrolleys()).Sum();
        }

        public bool StartHubsEmpty()
        {
            return STHubs.All(sh => sh.StartHubEmpty);
        }

        public FullTrolleyHub HasFullTrolleyHubFull(int MinimumTrolleys)
        {
            foreach (FullTrolleyHub Hub in FTHubs) 
                if(Hub.AmountOfTrolleys() >= MinimumTrolleys)
                    return Hub;
            return null;
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
