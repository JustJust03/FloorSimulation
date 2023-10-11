using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    /// <summary>
    /// Buffer hub, all empty trolleys will be moved toward this buffer
    /// </summary>
    internal class BufferHub: Hub
    {
        public DanishTrolley[] Trolleyarr;
        private DanishTrolley DummyTrolley;
        private WalkTile[] HubAccessPoints; //The Points in the hub where you can drop of trolleys
        private int[] HubAccessPointsX; //The Points in the hub where you can drop of trolleys

        public BufferHub(string name_, int id_, Point FPoint_, Floor floor_, WalkWay ww_, int initial_trolleys = 0, bool vertical_trolleys_ = true) : 
            base(name_, id_, FPoint_, floor_, ww_, new Size(2000, 200), initial_trolleys: initial_trolleys, vertical_trolleys: vertical_trolleys_)
        {
            DummyTrolley = new DanishTrolley(433, floor, IsVertical_: true);
            if (vertical_trolleys_)
                Trolleyarr = new DanishTrolley[RHubSize.Width / (Rslack + DummyTrolley.VRTrolleySize.Width)];
            else
                Trolleyarr = new DanishTrolley[RHubSize.Height / (Rslack + DummyTrolley.HRTrolleySize.Height)];


            //TODO: This ALWAYS puts the distributer at the top of the trolley
            HubAccessPoints = new WalkTile[Trolleyarr.Length];
            HubAccessPointsX = new int[Trolleyarr.Length];
            for (int i = 0; i < Trolleyarr.Length; i++)
            {
                int trolleyX = RFloorPoint.X + Rslack + i * (Rslack + DummyTrolley.VRTrolleySize.Width); //this point + how far in the line it is
                int trolleyY = RFloorPoint.Y + Rslack - floor.FirstDistr.RDistributerSize.Height;

                HubAccessPoints[i] = WW.GetTile(new Point(trolleyX, trolleyY));
                HubAccessPointsX[i] = HubAccessPoints[i].Rpoint.X;
            }
        }

        /// <summary>
        /// To which tile should the distributer walk to drop off this trolley.
        /// </summary>
        public override List<WalkTile> VOpenSpots(Distributer DButer)
        {
            List<WalkTile> OpenSpots = new List<WalkTile>();
            
            for (int i = 0; i < Trolleyarr.Length; i++) 
                if (Trolleyarr[i] == null)
                    OpenSpots.Add(HubAccessPoints[i]);

            return OpenSpots;
        }

        


        
        /// <summary>
        /// Takes a trolley in at the right index in the trolley array.
        /// Uses the distributer point to dertermine where this trolley is placed.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="t">The WalkTile the distributer is standing on</param>
        public override void TakeVTrolleyIn(DanishTrolley dt, Point AgentRPoint)
        {
            int ArrIndex = Array.IndexOf(HubAccessPointsX, AgentRPoint.X);
            Trolleyarr[ArrIndex] = dt;
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
