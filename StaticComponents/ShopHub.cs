using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    /// <summary>
    /// A hub where the trolleys of a shop are placed.
    /// Used to interact with distributers to obtain plants and switch out trolleys.
    /// trolleys (in horizontal position) are placed vertical below each other.
    /// 
    /// 2 carts real size = 200 x 200. 1 cart real size = 200 x 100
    /// </summary>
    internal class ShopHub
    {
        internal List<DanishTrolley> HubTrolleys;
        string name;
        int id;
        private Point RFloorPoint;   //Real point on the floor. In cm.
        private Size RHubSize;       //Real size of the hub. In cm.
        private Point FloorPoint;
        private Size HubSize;
        Floor floor;
        private int max_trolleys;   //How many trolleys can be placed in this hub

        public ShopHub(string name_, int id_, Point FPoint_, Floor floor_, int initial_trolleys = 0)
        {
            name = name_;
            id = id_;
            RFloorPoint = FPoint_;
            RHubSize = new Size(200, 200);
            floor = floor_;

            max_trolleys = RHubSize.Height / 100;
            HubTrolleys = new List<DanishTrolley>();
            InitTrolleys(initial_trolleys);

            FloorPoint = floor.ConvertToSimPoint(RFloorPoint); //Scaled up the Real Hub Floor Point to the SimPoint
            HubSize = floor.ConvertToSimSize(RHubSize); //Scaled up the Real Hub Size to the SimSize

        }

        /// <summary>
        /// Assigns the trolleys in the hub from top to bottom.
        /// 10cm space in every dimension.
        /// </summary>
        /// <param name="initial_trolleys"></param>
        public void InitTrolleys(int initial_trolleys)
        {
            if (initial_trolleys > max_trolleys)
                throw new ArgumentException("Can't add more trolleys to this shop hub.");

            int UpperY = FloorPoint.Y; //Start from the top of the hub, and keep track of where to place the trolley.
            int Rslack = 20; //The real slack in all dimensions.
            int slack = (int)(Rslack * Floor.ScaleFactor);
            for (int i = 0; i < initial_trolleys; i++)
            {
                DanishTrolley DT = new DanishTrolley(i);

                int trolleyY = UpperY + slack; 
                UpperY += trolleyY + DT.TrolleySize.Height + slack;
                int trolleyX = FloorPoint.X + slack; 

                DT.TeleportTrolley(new Point(trolleyX, trolleyY));
                HubTrolleys.Add(DT);
            }

        }

        public override string ToString()
        {
            return "This is a ShopHub: \n\tName: " + this.name + " \n\tID: " + this.id;
        }

        /// <summary>
        /// Draw the components (trolleys) to the screen.
        /// optionally draw the outline of the hub for better visualization.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="DrawOutline"></param>
        public void DrawHub(Graphics g, bool DrawOutline = false)
        {
            //outline
            if (DrawOutline)
            {
                g.DrawRectangle(floor.BPen, new Rectangle(FloorPoint, HubSize));
            }

            //Trolleys
            foreach (DanishTrolley DT in HubTrolleys)
            {
                DT.DrawObject(g);
            }
        }
    }
}
