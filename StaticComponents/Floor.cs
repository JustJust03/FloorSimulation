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
        public readonly Pen LightYellowPen = new Pen(Color.LightYellow);
        public readonly Pen GreenPen = new Pen(Color.Green);
        public readonly Pen PurplePen = new Pen(Color.Purple);
        public readonly Pen PinkPen = new Pen(Color.Pink);
        public readonly Pen GrayPen = new Pen(Color.DarkSlateGray);
        public readonly Pen RedOrangePen = new Pen(Color.OrangeRed);
        public int Ticks = 0;
        public double MilisecondsPerTick;
        public TimeSpan ElapsedSimTime = TimeSpan.Zero;
        public int SpeedMultiplier;
        public FinishedDistribution FinishedD;
        public Random rand;

        // Real size: 5000 cm x 5000 cm
        public const float ScaleFactor = 0.10f; //((Height of window - 40) / RealFloorHeight) - (800 / 2000 = 0.4)
        public Layout layout;

        public bool TickingHeatMap = false;
        public const int NDistributers = 21;
        public const int SecondsToFullOperation = 240; //How long to wait before all distributers are running
        public int OperationalInterval; //How long to wait between distributers

        public List<DanishTrolley> TrolleyList; // A list with all the trolleys that are on the floor.
        public List<Hub> HubList; // A list with all the hubs that are on the floor (starthub: 0, shophubs >= 1)
        public List<StartHub> STHubs;
        public List<BufferHub> BuffHubs;
        public List<FullTrolleyHub> FTHubs;
        public List<LowPadAccessHub> LPHubs;
        public TruckHub TrHub;
        public List<Distributer> DistrList; // A list with all the distributers that are on the floor.
        public Distributer LHDriver;
        public List<LowPad> LPList;
        public List<Distributer> TotalDistrList;
        public List<LowPad> TotalLPList;
        public LangeHarry FirstHarry;
        public WalkWay FirstWW;
        public WalkWayHeatMap WWHeatMap;

        public Dictionary<ShopHub, LowPadAccessHub> ShopHubPerRegion;
        public Dictionary<Point, LowPadAccessHub> AccessPointPerRegion;

        /// <summary>
        /// Sets the pixel floor size by using the ScaleFactor.
        /// </summary>
        /// <param name="PanelLocation">Where should the panel be drawn from (topleft)</param>
        /// <param name="di">On which display is this being drawn</param>
        public Floor(Point PanelLocation, MainDisplay di, ReadData rd)
        {
            Display = di;

            //layout = new SLayoutDayId(this, rd);
            layout = new SLayoutDayIdBuffhub(this, rd);
            //layout = new SLayoutDayIdBuffhub2Streets(this, rd);
            //layout = new SLayoutDayId2Streets(this, rd);
            //layout = new KortereVerdeelstraatSlayout(this, rd);
            //layout = new KortereVerdeelstraatSlayoutSmartStart(this, rd);

            //layout = new LowPadSlayoutBuffhub(this, rd);


            Size PixelFloorSize = new Size((int)(layout.RealFloorWidth * ScaleFactor),
                                           (int)(layout.RealFloorHeight * ScaleFactor));
            this.Location = PanelLocation;
            this.Size = PixelFloorSize;
            this.BackColor = FloorColor;
            MilisecondsPerTick = (1.0 / Program.TICKS_PER_SECOND) * 1000;
            FinishedD = new FinishedDistribution(this);
            rand = new Random(0);

            TrolleyList = new List<DanishTrolley>();
            HubList = new List<Hub>();
            STHubs = new List<StartHub>();
            FTHubs = new List<FullTrolleyHub>();
            LPHubs = new List<LowPadAccessHub>();

            ShopHubPerRegion = new Dictionary<ShopHub, LowPadAccessHub>();
            AccessPointPerRegion = new Dictionary<Point, LowPadAccessHub>();

            FirstWW = new WalkWay(new Point(0, 0), new Size(layout.RealFloorWidth, layout.RealFloorHeight), this, DevTools_: false);
            WWHeatMap = new WalkWayHeatMap(FirstWW, this);

            FirstHarry = new LangeHarry(0, this, FirstWW, new Point(FirstWW.RSizeWW.Width - 500, 1700));

            DistrList = new List<Distributer>();
            LPList = new List<LowPad>();

            TotalDistrList = new List<Distributer>();
            TotalLPList = new List<LowPad>();

            layout.PlaceDistributers(NDistributers, new Point(FirstWW.RSizeWW.Width - 1000, 2000));
            if(layout.NLowpads > 0)
                OperationalInterval = SecondsToFullOperation / layout.NLowpads;
            else
                OperationalInterval = SecondsToFullOperation / NDistributers;

            TrHub = new TruckHub("Truck Hub", 6, new Point(FirstWW.RSizeWW.Width - 770, 900), this);
            HubList.Add(TrHub);

            BuffHubs = new List<BufferHub>();

            this.Paint += PaintFloor;
            this.Invalidate();
        }

        public void TickButton(object sender, EventArgs e)
        {
            Ticks += SpeedMultiplier;
            ElapsedSimTime = ElapsedSimTime.Add(TimeSpan.FromMilliseconds(MilisecondsPerTick * SpeedMultiplier));

            if ((int)ElapsedSimTime.TotalSeconds <= SecondsToFullOperation + 2)
                AddAgent((int)ElapsedSimTime.TotalSeconds);

            foreach (Distributer d in DistrList)
                d.Tick();
            if (LHDriver != null)
                LHDriver.Tick();

            foreach (LowPad lp in LPList)
                lp.Tick();

            if (TickingHeatMap)
                WWHeatMap.TickHeatMap();

            Display.Invalidate();
            Invalidate();
        }

        private void AddAgent(int seconds)
        {
            if (layout.NLowpads > 0)
                AddLowPad(seconds);
            else
                AddDistr(seconds);
        }

        private void AddLowPad(int seconds)
        {
            if(TotalLPList.Count ==  0) 
                return;
            if(OperationalInterval == 0)
            {
                LPList = TotalLPList.ToList();
                TotalLPList.Clear();
                return;
            }
            int TargetAmntLP = seconds / OperationalInterval;
            if (TargetAmntLP > layout.NLowpads)
                TargetAmntLP = layout.NLowpads;
            if(LPList.Count < TargetAmntLP)
            {
                LPList.Add(TotalLPList[0]);
                TotalLPList.RemoveAt(0);
                AddLowPad(seconds);
            }
        }

        private void AddDistr(int seconds)
        {
            if(TotalDistrList.Count ==  0) 
                return;
            if(OperationalInterval == 0)
            {
                DistrList = TotalDistrList.ToList();
                TotalDistrList.Clear();
                return;
            }
            int TargetAmntDistr = seconds / OperationalInterval;
            if (TargetAmntDistr > NDistributers)
                TargetAmntDistr = NDistributers;
            if(DistrList.Count < TargetAmntDistr)
            {
                DistrList.Add(TotalDistrList[0]);
                TotalDistrList.RemoveAt(0);
                AddDistr(seconds);
            }
        }

        public void DrawOccupiance(object sender, EventArgs e)
        {
            FirstWW.DevTools = !FirstWW.DevTools;
            if (!TickingHeatMap)
                Invalidate();
        }

        public void DrawHeatMap(object sender, EventArgs e)
        {
            if (TickingHeatMap)
            {
                FirstWW.DrawHeatMap = !FirstWW.DrawHeatMap;
                Invalidate();
            }
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
            foreach (LowPad lp in LPList)
                lp.DrawObject(g);
        }

        public void PlaceShops(List<ShopHub> Shops)
        {
            layout.PlaceShops(Shops, 1040, FirstWW.RSizeWW.Height - 730);
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

        public void AssignRegions(List<DanishTrolley> dtList)
        {
            layout.AssignRegionsToTrolleys(dtList);
        }

        /// <summary>
        /// Returns the closes FullTrolley Hub to the distributer
        /// </summary>
        public FullTrolleyHub ClosestFTHub(Distributer db)
        {
            FullTrolleyHub ClosestHub = FTHubs[0];
            foreach (FullTrolleyHub FThub in FTHubs)
                if (!FTHubs[0].VerticalTrolleys && Math.Abs(db.RPoint.X - FThub.RFloorPoint.X) < Math.Abs(db.RPoint.X - ClosestHub.RFloorPoint.X))
                    ClosestHub = FThub;
                else if (FTHubs[0].VerticalTrolleys && Math.Abs(db.RPoint.Y - FThub.RFloorPoint.Y) < Math.Abs(db.RPoint.Y - ClosestHub.RFloorPoint.Y))
                    ClosestHub = FThub;

            return ClosestHub;
        }

        public StartHub GetStartHub(Agent agent)
        {
            return layout.GetStartHub(agent);
        }

        public BufferHub GetBuffHubFull(Agent agent)
        {
            return layout.GetBuffHubFull(agent);
        }

        public BufferHub GetBuffHubOpen(Agent agent)
        {
            return layout.GetBuffHubOpen(agent);
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

        public BufferHub HasFullSmallBufferHub(int MinimumTrolleys) 
        {
            foreach (BufferHub Hub in BuffHubs)
                if(Hub.name == "Buffer hub")
                    continue;
                else if(Hub.AmountOfTrolleys() >= MinimumTrolleys)
                    return Hub;
            return null;
        }

        public BufferHub HasEmptySmallBufferHub(int MaximumTrolleys) 
        {
            foreach (BufferHub Hub in BuffHubs)
                if(Hub.name == "Buffer hub")
                    continue;
                else if(Hub.AmountOfTrolleys() <= MaximumTrolleys)
                    return Hub;
            return null;
        }

        /// <summary>
        /// Counts the amount of full trolleys in the FullTrolley hubs and the truck hub.
        /// </summary>
        /// <returns></returns>
        public int FullTrolleysOnFloor()
        {
            int trolleys = 0;
            foreach(FullTrolleyHub t in FTHubs)
                trolleys += t.AmountOfTrolleys();
            trolleys += TrHub.AmountOfTrolleys();

            return trolleys;
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
