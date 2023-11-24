using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Eventing.Reader;

namespace FloorSimulation
{
    /// <summary>
    /// Buffer hub, all empty trolleys will be moved toward this buffer
    /// </summary>
    internal class BufferHub : Hub
    {
        public DanishTrolley[,] Trolleyarr;
        private DanishTrolley DummyTrolley;
        private WalkTile[,] HubAccessPoints; //The Points in the hub where you can drop of trolleys
        private int[] HubAccessPointsX; //The Points in the hub where you can drop of trolleys
        private int[] HubAccessPointsY; //The Points in the hub where you can drop of trolleys
        private WalkTile[] HarryHubAccessPoints; //The Points in the hub where Lange Harry can pick up trolleys
        private int[] HarryHubAccessPointsY; //The Points in the hub where Lange Harry can take in trolleys
        private int NRows;
        private int NTrolleysInRow;

        public BufferHub(string name_, int id_, Point FPoint_, Size s, Floor floor_, int initial_trolleys = 0, bool vertical_trolleys_ = true) :
            base(name_, id_, FPoint_, floor_, s, initial_trolleys: 0, vertical_trolleys: vertical_trolleys_)
        {
            if (vertical_trolleys_)
            {
                DummyTrolley = new DanishTrolley(-1, floor, IsVertical_: true);
                NRows = RHubSize.Height / 200;
                NTrolleysInRow = RHubSize.Width / (5 + DummyTrolley.GetRSize().Width);
                Trolleyarr = new DanishTrolley[NRows, NTrolleysInRow];
            }
            else
            {
                DummyTrolley = new DanishTrolley(-1, floor, IsVertical_: false);
                NRows = RHubSize.Height / (Rslack + DummyTrolley.GetRSize().Height);
                NTrolleysInRow = RHubSize.Width / (Rslack + DummyTrolley.GetRSize().Width);
                Trolleyarr = new DanishTrolley[NRows, NTrolleysInRow];
            }

            HubAccessPoints = new WalkTile[NRows, NTrolleysInRow];
            HubAccessPointsX = new int[NTrolleysInRow];
            HubAccessPointsY = new int[NRows];

            if(vertical_trolleys_) //Big buffer hub
                GenerateVerticalAccessPoints();
            else //Small buffer hub
                GenerateHorizontalAccessPoints();

            //Creates initial empty trolleys to the bufferhub
            for (int i = Trolleyarr.Length - initial_trolleys; i < Trolleyarr.Length; i++)
            {
                Point p = HubAccessPoints[0, i].Rpoint;
                DanishTrolley t = new DanishTrolley(100 + i, floor, p, true);
                WW.fill_tiles(t.RPoint, t.GetRSize());
                Trolleyarr[0, i] = t;
            }
        }

        private void GenerateVerticalAccessPoints()
        {
            for (int y = 0; y < NRows; y++)
            {
                int trolleyY = RFloorPoint.Y + y * 200;
                for (int i = 0; i < NTrolleysInRow; i++)
                {
                    if(i % 20 == 19)
                        continue;
                    int trolleyX = RFloorPoint.X + i * (5 + DummyTrolley.GetRSize().Width); //this point + how far in the line it is

                    HubAccessPoints[y, i] = WW.GetTile(new Point(trolleyX, trolleyY));
                    HubAccessPointsX[i] = HubAccessPoints[y, i].Rpoint.X;
                }
                HubAccessPointsY[y] = HubAccessPoints[y, 0].Rpoint.Y;
            }
        }

        private void GenerateHorizontalAccessPoints()
        {
            int trolleyX;
            for(int x = 0; x < NTrolleysInRow; x++)
            {
                trolleyX = RFloorPoint.X;
                for(int y = 0; y < NRows; y++)
                {
                    int trolleyY = RFloorPoint.Y + Rslack + y * (Rslack + DummyTrolley.GetRSize().Height);

                    HubAccessPoints[y, x] = WW.GetTile(new Point(trolleyX, trolleyY));
                    HubAccessPointsY[y] = HubAccessPoints[y, x].Rpoint.Y;
                }
                HubAccessPointsX[x] = HubAccessPoints[0, x].Rpoint.X;
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

            if (VerticalTrolleys)
            {
                if (DButer.IsOnHarry)
                    return LangeHarryOpenSpots(DButer);
                for (int coli = NTrolleysInRow - 1; coli >= 0; coli--)
                    for (int rowi = 0; rowi < NRows; rowi++)
                        if (Trolleyarr[rowi, coli] == null)
                        {
                            OpenSpots.Add(HubAccessPoints[rowi, coli]);
                            break;
                        }
            }

            else
            {
                for (int rowi = 0; rowi < NRows; rowi++)
                    for (int coli = NTrolleysInRow - 1; coli >= 0; coli--)
                        if (Trolleyarr[rowi, coli] == null)
                        {
                            OpenSpots.Add(HubAccessPoints[rowi, coli]);
                        }
            }

            return OpenSpots;
        }

        private List<WalkTile> LangeHarryOpenSpots(Distributer DButer)
        {
            //Fills the main buffer hub from right to left end up to down.
            List<WalkTile> OpenSpots = new List<WalkTile>();
            int farthest = -1; 
            
            for (int rowi = 0; rowi < NRows; rowi++) 
                for(int coli = NTrolleysInRow - 1; coli >= 0 && coli > farthest; coli--)
                    if (Trolleyarr[rowi, coli] == null && HubAccessPoints[rowi, coli] != null)
                    {
                        WalkTile wt = HubAccessPoints[rowi, coli];
                        WalkTile DownTile = WW.GetTile(new Point(wt.Rpoint.X - 280, wt.Rpoint.Y + 40));

                        OpenSpots.Add(DownTile);
                        farthest = coli;
                        break;
                    }

            return OpenSpots;
        }

        /// <summary>
        /// To which tile should the distributer walk to take an empty trolley.
        /// </summary>
        public override List<WalkTile> FilledSpots(Distributer DButer)
        {
            if (DButer.IsOnHarry)
                return LangeHarryFilledSpots();

            List<WalkTile> CSpots = new List<WalkTile>();

            for (int rowi = 0; rowi < NRows; rowi++)
                for (int coli = NTrolleysInRow - 1; coli >= 0; coli--)
                    if (Trolleyarr[rowi, coli] != null)
                    {
                        Point p = HubAccessPoints[rowi, coli].Rpoint;
                        if (VerticalTrolleys)
                            p.Y += 180;
                        else
                            p.X -= 20;
                        CSpots.Add(WW.GetTile(p));
                    }

            if(CSpots.Count == 0 && name == "Buffer hub") 
            {
                SpawnEmptyTrolleys(5);
                return FilledSpots(DButer);
            } 
            return CSpots;
        }

        private List<WalkTile> LangeHarryFilledSpots()
        {
            List<WalkTile> FilledSpots = new List<WalkTile>();
            
            for (int i = 0; i < Trolleyarr.Length; i++) 
                if (Trolleyarr[i, 0] != null)
                    FilledSpots.Add(HarryHubAccessPoints[i]);

            return FilledSpots;
        }

        public void SpawnEmptyTrolleys(int amnt = 5)
        {
            amnt = Math.Min(amnt, HubAccessPointsX.Length);
            for(int i = 0; i < amnt; i++)
            {
                Point p = new Point(HubAccessPointsX[i], HubAccessPointsY[0]);
                DanishTrolley dt = new DanishTrolley(0, floor, p, true);
                WW.fill_tiles(p, dt.GetRSize());
                Trolleyarr[0, i] = dt;
            }
        }

        public override DanishTrolley PeekFirstTrolley()
        {
            throw new NotImplementedException("THIS SHOULD NOT BE CALLED ANYMORE");
        }

        public override DanishTrolley GiveTrolley(Point AgentRPoint)
        {
            int ArrIndexx;
            int ArrIndexy;
            if (!VerticalTrolleys)
            {
                ArrIndexx = Array.IndexOf(HubAccessPointsX, AgentRPoint.X + 20);
                ArrIndexy = Array.IndexOf(HubAccessPointsY, AgentRPoint.Y); //This 20 is because of the extra height when the dbuter is rotated.
            }
            else
            {
                ArrIndexx = Array.IndexOf(HubAccessPointsX, AgentRPoint.X);
                ArrIndexy = Array.IndexOf(HubAccessPointsY, AgentRPoint.Y - 180); //This 20 is because of the extra height when the dbuter is rotated.
            }

            if (ArrIndexx == -1 || ArrIndexy == -1) return null;
            DanishTrolley t = Trolleyarr[ArrIndexy, ArrIndexx];
            Trolleyarr[ArrIndexy, ArrIndexx] = null;

            return t;
        }

        public override DanishTrolley GiveTrolleyToHarry(Point AgentRPoint)
        {
            int ArrIndex = Array.IndexOf(HarryHubAccessPointsY, AgentRPoint.Y);
            if (ArrIndex == -1 || Trolleyarr[ArrIndex, 0] == null) return null;
            DanishTrolley t = Trolleyarr[ArrIndex, 0];
            Trolleyarr[ArrIndex, 0] = null;
            WW.unfill_tiles(t.RPoint, t.GetRSize());

            return t;
        }

        /// <summary>
        /// Takes a trolley in at the right index in the trolley array.
        /// Uses the distributer point to dertermine where this trolley is placed.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="t">The WalkTile the distributer is standing on</param>
        public override void TakeVTrolleyIn(DanishTrolley dt, Point AgentRPoint)
        {
            int ArrIndexx = Array.IndexOf(HubAccessPointsX, AgentRPoint.X);
            int ArrIndexy = Array.IndexOf(HubAccessPointsY, AgentRPoint.Y + 10); //The lenght of the trolley
            Trolleyarr[ArrIndexy, ArrIndexx] = dt;
            dt.Units = 0;
            dt.NStickers = 2;
            dt.IsVertical = true;
            if (MainBufferFull()) //Remove the first row
            {
                Point p = new Point(RFloorPoint.X, RFloorPoint.Y + (NRows - 1) * 200);
                Size s = new Size(RHubSize.Width, 200);
                floor.FirstWW.unfill_tiles(p, s);
                for (int coli = NTrolleysInRow - 1; coli >= 0; coli--)
                    Trolleyarr[NRows - 1, coli] = null;
            }
        }

        public override void LHTakeVTrolleyIn(DanishTrolley dt, Point AgentRPoint)
        {
            int ArrIndexx = Array.IndexOf(HubAccessPointsX, AgentRPoint.X + 280);
            int ArrIndexy = Array.IndexOf(HubAccessPointsY, AgentRPoint.Y - 40);
            Trolleyarr[ArrIndexy, ArrIndexx] = dt;
            dt.Units = 0;
            dt.NStickers = 2;
            dt.IsVertical = true;
            if (MainBufferFull()) //Remove the first row
            {
                Point p = new Point(RFloorPoint.X, RFloorPoint.Y + (NRows - 1) * 200);
                Size s = new Size(RHubSize.Width, 200);
                floor.FirstWW.unfill_tiles(p, s);
                for (int coli = NTrolleysInRow - 1; coli >= 0; coli--)
                    Trolleyarr[NRows - 1, coli] = null;
            }
        }

        public override void TakeHTrolleyIn(DanishTrolley dt, Point AgentRPoint)
        {
            int ArrIndexx = Array.IndexOf(HubAccessPointsX, AgentRPoint.X + 10);
            int ArrIndexy = Array.IndexOf(HubAccessPointsY, AgentRPoint.Y); //The lenght of the trolley
            Trolleyarr[ArrIndexy, ArrIndexx] = dt;
            dt.Units = 0;
            dt.NStickers = 2;
            dt.IsVertical = false;
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

        private bool MainBufferFull()
        {
            if (name != "Buffer hub")
                return false;
            for (int coli = NTrolleysInRow - 1; coli >= 0; coli--)
                if (Trolleyarr[NRows - 1, coli] == null)
                    return false;
            return true;
        }

        public override int AmountOfTrolleys()
        {
            int count = 0;
            for (int rowi = 0; rowi < NRows; rowi++) 
                for(int coli = NTrolleysInRow - 1; coli >= 0; coli--)
                    if (Trolleyarr[rowi, coli] != null)
                        count++;
            return count;
        }
    }
}
