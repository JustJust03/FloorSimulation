using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    /// <summary>
    /// When a trolley from the shop hub is full, this is where de trolleys will be placed.
    /// Lange Harry will pick these up in one go when there are enough full trolleys.
    /// </summary>
    internal class FullTrolleyHub: Hub
    {
        public DanishTrolley[] Trolleyarr;
        private DanishTrolley DummyTrolley;
        private Distributer DummyDistributer;
        private WalkTile[] HubAccessPoints; //The Points in the hub where you can drop of trolleys
        private int[] HubAccessPointsY; //The Points in the hub where you can drop of trolleys
        private WalkTile[] HarryHubAccessPoints; //The Points in the hub where Lange Harry can pick up trolleys
        private int[] HarryHubAccessPointsY; //The Points in the hub where you can drop of trolleys

        /// <summary>
        /// The full trolley hub is usually placed inbetween the street
        /// </summary>
        public FullTrolleyHub(string name_, int id_, Point FPoint_, Floor floor_, Size RHubSize_, int initial_trolleys = 0, bool vertical_trolleys_ = false) : 
            base(name_, id_, FPoint_, floor_, RHubSize_, initial_trolleys: initial_trolleys, vertical_trolleys: vertical_trolleys_)
        {
            DummyTrolley = new DanishTrolley(-1, floor, IsVertical_: false);
            DummyDistributer = new Distributer(-1, floor, WW, IsVertical_: false);
            if (vertical_trolleys_)
                Trolleyarr = new DanishTrolley[RHubSize.Width / (Rslack + DummyTrolley.GetRSize().Width)];
            else
                Trolleyarr = new DanishTrolley[RHubSize.Height / (Rslack + DummyTrolley.GetRSize().Height)];


            HubAccessPoints = new WalkTile[Trolleyarr.Length];
            HubAccessPointsY = new int[Trolleyarr.Length];
            int trolleyX = RFloorPoint.X + RHubSize.Width / 2  - (DummyTrolley.GetRSize().Width / 2 + DummyDistributer.GetRDbuterSize().Width - 10); //Place the trolley exactly in the middle

            for (int i = 0; i < Trolleyarr.Length; i++)
            {
                int trolleyY = RFloorPoint.Y + Rslack + i * (Rslack + DummyTrolley.GetRSize().Height);

                HubAccessPoints[i] = WW.GetTile(new Point(trolleyX, trolleyY));
                HubAccessPointsY[i] = HubAccessPoints[i].Rpoint.Y;
            }

            HarryHubAccessPoints = new WalkTile[Trolleyarr.Length];
            HarryHubAccessPointsY = new int[Trolleyarr.Length];
            trolleyX = RFloorPoint.X + RHubSize.Width / 2  - (DummyTrolley.GetRSize().Width / 2); //Place the trolley exactly in the middle
            for (int i = 0; i < Trolleyarr.Length; i++)
            {
                int trolleyY = RFloorPoint.Y + (i + 1) * (Rslack + DummyTrolley.GetRSize().Height) + 10;

                HarryHubAccessPoints[i] = WW.GetTile(new Point(trolleyX, trolleyY));
                HarryHubAccessPointsY[i] = HarryHubAccessPoints[i].Rpoint.Y;
            }
        }

        /// <summary>
        /// To which tile should the distributer walk to drop off this trolley.
        /// </summary>
        public override List<WalkTile> OpenSpots(Distributer DButer)
        {
            List<WalkTile> OpenSpots = new List<WalkTile>();
            
            for (int i = 0; i < Trolleyarr.Length; i++) 
                if (Trolleyarr[i] == null)
                    OpenSpots.Add(HubAccessPoints[i]);

            return OpenSpots;
        }

        /// <summary>
        /// To which tile should LangeHarry drive to pick finished trolleys up
        /// </summary>
        public override List<WalkTile> FilledSpots(Distributer DButer)
        {
            List<WalkTile> FilledSpots = new List<WalkTile>();
            
            for (int i = 0; i < Trolleyarr.Length; i++) 
                if (Trolleyarr[i] != null)
                    FilledSpots.Add(HarryHubAccessPoints[i]);

            return FilledSpots;
        }
        
        /// <summary>
        /// Takes a trolley in at the right index in the trolley array.
        /// Uses the distributer point to dertermine where this trolley is placed.
        /// </summary>
        /// <param name="dt"></param>
        public override void TakeHTrolleyIn(DanishTrolley dt, Point AgentRPoint)
        {
            int ArrIndex = Array.IndexOf(HubAccessPointsY, AgentRPoint.Y);
            Trolleyarr[ArrIndex] = dt;
        }

        /// <summary>
        /// Gives a trolley away to lange harry.
        /// Uses the real point of the distributer riding the Lange harry
        /// </summary>
        /// <param name="AgentRPoint"></param>
        /// <returns></returns>
        public override DanishTrolley GiveTrolley(Point AgentRPoint = default)
        {
            int ArrIndex = Array.IndexOf(HarryHubAccessPointsY, AgentRPoint.Y);
            if (ArrIndex == -1) return null;
            DanishTrolley t = Trolleyarr[ArrIndex];
            Trolleyarr[ArrIndex] = null;
            WW.unfill_tiles(t.RPoint, t.GetRSize());

            return t;
        }

        public override void DrawHub(Graphics g, bool DrawOutline = false)
        {
            //outline
            if (DrawOutline)
                g.DrawRectangle(floor.BPen, new Rectangle(FloorPoint, HubSize));

            //Trolleys
            foreach (DanishTrolley DT in Trolleyarr)
                if(DT != null)
                    DT.DrawObject(g);
        }

        public override int AmountOfTrolleys()
        {
            int i = 0;
            foreach (DanishTrolley dt in Trolleyarr)
                if (dt != null)
                    i++;
            return i;
        }
    }
}
