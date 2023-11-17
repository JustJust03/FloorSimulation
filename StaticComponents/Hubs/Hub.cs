using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace FloorSimulation
{
    /// <summary>
    /// Standard hub for all places (ShopHub, StartHub, EndHub)
    /// </summary>
    internal class Hub
    {
        protected List<DanishTrolley> HubTrolleys;
        protected string name;
        public int id;
        public Point RFloorPoint;    //Real point on the floor. In cm.
        protected Point FloorPoint;
        public Size RHubSize;        //Real size of the hub. In cm.
        protected Size HubSize;
        protected Floor floor;
        protected WalkWay WW;
        protected int max_trolleys;     //How many trolleys can be placed in this hub
        protected int Rslack = 20;
        public bool HasLeftAccess = false;
        public bool VerticalTrolleys;
        protected List<WalkTile> Accesspoint;

        public Hub(string name_, int id_, Point FPoint_, Floor floor_, Size RHubSize_, int initial_trolleys = 0, 
                   bool vertical_trolleys = false)
        {
            name = name_;
            id = id_;
            RFloorPoint = FPoint_;
            floor = floor_;
            WW = floor.FirstWW;
            RHubSize = RHubSize_;
            VerticalTrolleys = vertical_trolleys;

            HubTrolleys = new List<DanishTrolley>();
            Accesspoint = new List<WalkTile>
            {
                null
            };
            DanishTrolley DummyTrolley = new DanishTrolley(433, floor, IsVertical_: true);
            if (vertical_trolleys)
            {
                max_trolleys = RHubSize.Width / (Rslack + DummyTrolley.GetRSize().Width);
                InitVTrolleys(initial_trolleys);
            }
            else
            {
                max_trolleys = RHubSize.Height / Rslack + DummyTrolley.GetRSize().Height;
                InitHTrolleys(initial_trolleys);
            }

            FloorPoint = floor.ConvertToSimPoint(RFloorPoint); //Scaled up the Real Hub Floor Point to the SimPoint
            HubSize = floor.ConvertToSimSize(RHubSize); //Scaled up the Real Hub Size to the SimSize
        }

        /// <summary>
        /// Assigns the trolleys in the hub from top to bottom.
        /// 10cm space in every dimension.
        /// </summary>
        /// <param name="initial_trolleys"></param>
        private void InitHTrolleys(int initial_trolleys)
        {
            if (initial_trolleys > max_trolleys)
                throw new ArgumentException("Can't add more trolleys to this shop hub.");

            int UpperY = RFloorPoint.Y; //Start from the top of the hub, and keep track of where to place the trolley.
            for (int i = 0; i < initial_trolleys; i++)
            {
                DanishTrolley DT = new DanishTrolley(i, floor, IsVertical_: false);

                int trolleyY = UpperY + Rslack; 
                UpperY += DT.GetRSize().Height + Rslack;
                int trolleyX = RFloorPoint.X + Rslack; 

                DT.TeleportTrolley(new Point(trolleyX, trolleyY));
                WW.fill_tiles(DT.RPoint, DT.GetRSize());
                HubTrolleys.Add(DT);
            }
        }

        private void InitVTrolleys(int initial_trolleys)
        {
            if (initial_trolleys > max_trolleys)
                throw new ArgumentException("Can't add more trolleys to this shop hub.");

            int LeftX = RFloorPoint.X; //Start from the left of the hub, and keep track of where to place the trolley.
            for (int i = 0; i < initial_trolleys; i++)
            {
                DanishTrolley DT = new DanishTrolley(i, floor, IsVertical_: true);

                int trolleyX = LeftX + Rslack; 
                LeftX += DT.GetRSize().Width + Rslack;
                int trolleyY = RFloorPoint.Y + Rslack; 

                DT.TeleportTrolley(new Point(trolleyX, trolleyY));
                WW.fill_tiles(DT.RPoint, DT.GetRSize());
                HubTrolleys.Add(DT);
            }

        }

        public override string ToString()
        {
            return "This is a Hub: \n\tName: " + this.name + " \n\tID: " + this.id;
        }

        /// <summary>
        /// Draw the components (trolleys) to the screen.
        /// optionally draw the outline of the hub for better visualization.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="DrawOutline"></param>
        public virtual void DrawHub(Graphics g, bool DrawOutline = false)
        {
            //outline
            
                g.DrawRectangle(floor.BPen, new Rectangle(FloorPoint, HubSize));

            //Trolleys
            foreach (DanishTrolley DT in HubTrolleys)
                DT.DrawObject(g);
        }

        /// <summary>
        /// Teleports the hub to a new point
        /// Also teleports his trolleys with it and it's occupiances
        /// </summary>
        public virtual void TeleportHub(Point NewRPoint)
        {
            List<Point> DiffPoints = new List<Point>(); //distance from top left of shophub to each trolley
            foreach(DanishTrolley t in HubTrolleys)
            {
                DiffPoints.Add(new Point(t.RPoint.X - RFloorPoint.X, t.RPoint.Y - RFloorPoint.Y));
                WW.unfill_tiles(t.RPoint, t.GetRSize());
            }

            RFloorPoint = NewRPoint;
            FloorPoint = floor.ConvertToSimPoint(RFloorPoint); 

            for (int i = 0; i < DiffPoints.Count; i++)
            {
                HubTrolleys[i].TeleportTrolley(new Point(RFloorPoint.X + DiffPoints[i].X, RFloorPoint.Y + DiffPoints[i].Y));
                WW.fill_tiles(HubTrolleys[i].RPoint, HubTrolleys[i].GetRSize());
            }
        }
        
        /// <summary>
        /// Peeks at the first available trolley in this hub's trolley list 
        /// </summary>
        public virtual DanishTrolley PeekFirstTrolley()
        {
            if (HubTrolleys.Count == 0) return null;
                return HubTrolleys[0];
        }

        public virtual DanishTrolley GetRandomTrolley()
        {
            int r = floor.rand.Next(0, AmountOfTrolleys());
            return HubTrolleys[r];
        }

        /// <summary>
        /// Takes the first trolley from the hub trolley list and deletes it.
        /// AgentRPoint is only used by bufferhub to check which trolley it should give.
        /// </summary>
        public virtual DanishTrolley GiveTrolley(Point AgentRPoint = default)
        {
            if (HubTrolleys.Count == 0) return null;
            DanishTrolley FirstTrolley = HubTrolleys[0];
            HubTrolleys.RemoveAt(0);

            return FirstTrolley;
        }

        /// <summary>
        /// Takes in the trolley in it's trolley list.
        /// </summary>
        public virtual void TakeVTrolleyIn(DanishTrolley t, Point DeliveryPoint = default)
        {
            if (HubTrolleys.Count + 1 == max_trolleys)//Max trolleys reached, give error
                throw new ArgumentException("Can't add more trolleys to this shop hub.");
            HubTrolleys.Add(t);
        }

        /// <summary>
        /// Takes in the trolley in it's trolley list.
        /// </summary>
        public virtual void TakeHTrolleyIn(DanishTrolley t, Point DeliveryPoint = default)
        {
            if (HubTrolleys.Count + 1 == max_trolleys)//Max trolleys reached, give error
                throw new ArgumentException("Can't add more trolleys to this shop hub.");
            HubTrolleys.Add(t);
        }

        /// <summary>
        /// Returns all the open spots where a new trolley could be placed
        /// Should only be used by the bufferhub and the FullTrolleyHub
        /// This is for both vertical and horizontal hubs
        /// </summary>
        public virtual List<WalkTile> OpenSpots(Distributer DButer)
        {
            return null;
        }

        /// <summary>
        /// Returns all the spots where trolleys are placed
        /// Should only be called by LangeHarry
        /// Should only be used by the bufferhub and the FullTrolleyHub
        /// This is for both vertical and horizontal hubs
        /// </summary>
        public virtual List<WalkTile> FilledSpots(Distributer DButer)
        {
            return null;
        }

        public int GetIndexOfTrolley(DanishTrolley dt)
        {
            return HubTrolleys.IndexOf(dt);
        }

        /// <summary>
        /// Checks if the trolley that became full is the upper trolley.
        /// If not, swap the two trolleys.
        /// </summary>
        public void SwapIfOtherTrolley(DanishTrolley dt)
        {
            int i = GetIndexOfTrolley(dt);
            if(i != 0)
            {
                DanishTrolley dtToSwap = HubTrolleys[i];
                HubTrolleys[i] = HubTrolleys[0];
                HubTrolleys[0] = dtToSwap;

                Point dtpToSwap = HubTrolleys[i].RPoint;
                HubTrolleys[i].RPoint = HubTrolleys[0].RPoint;
                HubTrolleys[0].RPoint = dtpToSwap;

                HubTrolleys[i].SimPoint = floor.ConvertToSimPoint(HubTrolleys[i].RPoint);
                HubTrolleys[0].SimPoint = floor.ConvertToSimPoint(HubTrolleys[0].RPoint);
            }
        }

        public virtual int AmountOfTrolleys()
        {
            return HubTrolleys.Count();
        }
    }
}
