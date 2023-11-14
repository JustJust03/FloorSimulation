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
        private int NRows;
        private int NTrolleysInRow;

        public BufferHub(string name_, int id_, Point FPoint_, Floor floor_, int initial_trolleys = 0, bool vertical_trolleys_ = true) :
            base(name_, id_, FPoint_, floor_, new Size(floor_.FirstWW.RSizeWW.Width - 200, 600), initial_trolleys: 0, vertical_trolleys: vertical_trolleys_)
        {
            DummyTrolley = new DanishTrolley(-1, floor, IsVertical_: true);
            if (vertical_trolleys_)
            {
                NRows = RHubSize.Height / 200;
                NTrolleysInRow = RHubSize.Width / (Rslack + DummyTrolley.GetRSize().Width);
                Trolleyarr = new DanishTrolley[NRows, NTrolleysInRow];
            }
            else
                throw new Exception("Horizontal bufferhub not implemented yet");


            //TODO: This ALWAYS puts the distributer at the top of the trolley
            HubAccessPoints = new WalkTile[NRows, NTrolleysInRow];
            HubAccessPointsX = new int[NTrolleysInRow];
            HubAccessPointsY = new int[NRows];
            for (int y = 0; y < NRows; y++)
            {
                int trolleyY = RFloorPoint.Y + y * 200;
                for (int i = 0; i < NTrolleysInRow; i++)
                {
                    int trolleyX = RFloorPoint.X + Rslack + i * (Rslack + DummyTrolley.GetRSize().Width); //this point + how far in the line it is

                    HubAccessPoints[y, i] = WW.GetTile(new Point(trolleyX, trolleyY));
                    HubAccessPointsX[i] = HubAccessPoints[y, i].Rpoint.X;
                }
                HubAccessPointsY[y] = HubAccessPoints[y, 0].Rpoint.Y;
            }

            //Creates initial empty trolleys to the bufferhub
            for (int i = Trolleyarr.Length - initial_trolleys; i < Trolleyarr.Length; i++)
            {
                Point p = HubAccessPoints[0, i].Rpoint;
                DanishTrolley t = new DanishTrolley(100 + i, floor, p, true);
                WW.fill_tiles(t.RPoint, t.GetRSize());
                Trolleyarr[0, i] = t;
            }
        }

        /// <summary>
        /// To which tile should the distributer walk to drop off this trolley.
        /// </summary>
        public override List<WalkTile> OpenSpots(Distributer DButer)
        {
            List<WalkTile> OpenSpots = new List<WalkTile>();

            for (int coli = NTrolleysInRow - 1; coli >= 0; coli--)
                for (int rowi = 0; rowi < NRows; rowi++)
                    if (Trolleyarr[rowi, coli] == null)
                    {
                        OpenSpots.Add(HubAccessPoints[rowi, coli]);
                        break;
                    }

            return OpenSpots;
        }

        /// <summary>
        /// To which tile should the distributer walk to take an empty trolley.
        /// </summary>
        public override List<WalkTile> FilledSpots(Distributer DButer)
        {
            List<WalkTile> CSpots = new List<WalkTile>();

            for (int rowi = 0; rowi < NRows; rowi++)
                for (int coli = NTrolleysInRow - 1; coli >= 0; coli--)
                    if (Trolleyarr[rowi, coli] != null)
                    {
                        Point p = HubAccessPoints[rowi, coli].Rpoint;
                        p.Y += 180;
                        CSpots.Add(WW.GetTile(p));
                    }

            if(CSpots.Count == 0) 
            {
                SpawnEmptyTrolleys(5);
                return FilledSpots(DButer);
            }

            return CSpots;
        }

        public void SpawnEmptyTrolleys(int amnt = 5)
        {
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
            int ArrIndexx = Array.IndexOf(HubAccessPointsX, AgentRPoint.X);
            int ArrIndexy = Array.IndexOf(HubAccessPointsY, AgentRPoint.Y - 180); //This 20 is because of the extra height when the dbuter is rotated.
            if (ArrIndexx == -1 || ArrIndexy == -1) return null;
            DanishTrolley t = Trolleyarr[ArrIndexy, ArrIndexx];
            Trolleyarr[ArrIndexy, ArrIndexx] = null;

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
            int ArrIndexy = Array.IndexOf(HubAccessPointsY, AgentRPoint.Y); //The lenght of the trolley
            Trolleyarr[ArrIndexy, ArrIndexx] = dt;
            dt.Units = 0;
            dt.IsVertical = true;
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
    }
}
