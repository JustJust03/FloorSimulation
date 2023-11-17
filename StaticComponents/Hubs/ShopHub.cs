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
    internal class ShopHub : Hub
    {
        public string ColliPlusDay;
        public string day;

        /// <summary>
        /// Shop hub has a standard size: (200cm x 200cm)
        /// Usually horizontal trolleys
        /// </summary>
        public ShopHub(string name_, int id_, Point FPoint_, Floor floor_, Size s, int initial_trolleys = 0, string ColliPlusDay_ = null) : 
            base(name_, id_, FPoint_, floor_, s, initial_trolleys: initial_trolleys)
        {
            ColliPlusDay = ColliPlusDay_;
            int nstrips = ColliPlusDay.Split('-').Length;
            day = ColliPlusDay.Split('-')[nstrips - 1];
        }

        public override string ToString() 
        {
            return "Name: " + this.name + " \n\tID: " + this.id + "\n\tDay: " + day;
        }

        public override DanishTrolley PeekFirstTrolley()
        {
            return HubTrolleys[0];
        }

        public override List<WalkTile> OpenSpots(Distributer DButer)
        {
            return new List<WalkTile> { Accesspoint[0] };
        }

        public override DanishTrolley GiveTrolley(Point AgenRPoint = default)
        {
            DanishTrolley FirstTrolley = HubTrolleys[0];
            HubTrolleys[0] = null;
            return FirstTrolley;
        }

        public override void TakeVTrolleyIn(DanishTrolley t, Point DeliveryPoint = default)
        {
            if (HubTrolleys[0] != null)//Max trolleys reached, give error
                throw new ArgumentException("The first trolley place wasn't empty");
            HubTrolleys[0] = t;
        }

        public override void TakeHTrolleyIn(DanishTrolley t, Point DeliveryPoint = default)
        {
            if (HubTrolleys[0] != null)//Max trolleys reached, give error
                throw new ArgumentException("The first trolley place wasn't empty");
            HubTrolleys[0] = t;
        }


        public override void TeleportHub(Point NewRPoint)
        {
            //Update Accesspoints
            if (VerticalTrolleys)
                throw new NotImplementedException("Implement access points for shop hubs first!");
            else
            {
                if (HasLeftAccess)
                    Accesspoint[0] = WW.GetTile(new Point(NewRPoint.X - 40, NewRPoint.Y + 20));
                else
                    Accesspoint[0] = WW.GetTile(new Point(NewRPoint.X + RHubSize.Width, NewRPoint.Y + 20));
            }
            base.TeleportHub(NewRPoint);
        }

        public void RotateAndTeleportHub(Point NewRPoint)
        {
            RHubSize = new Size(RHubSize.Height, RHubSize.Width);
            HubSize = floor.ConvertToSimSize(RHubSize); //Scaled up the Real Hub Size to the SimSize
            VerticalTrolleys = !VerticalTrolleys;
            foreach(DanishTrolley dt in HubTrolleys)
                dt.RotateTrolley();

            TeleportHub(NewRPoint);
        }

        /// <summary>
        /// Draw the components (trolleys) to the screen.
        /// optionally draw the outline of the hub for better visualization.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="DrawOutline"></param>
        public override void DrawHub(Graphics g, bool DrawOutline = false)
        {
            //outline
            if (DrawOutline)
            {
                if(day == "DI")
                    g.DrawRectangle(floor.YellowPen, new Rectangle(FloorPoint, HubSize));
                else if (day == "WO")
                    g.DrawRectangle(floor.BluePen, new Rectangle(FloorPoint, HubSize));
                else
                    g.DrawRectangle(floor.BPen, new Rectangle(FloorPoint, HubSize));
            }

            //Trolleys
            foreach (DanishTrolley DT in HubTrolleys)
                if(DT != null)
                    DT.DrawObject(g);
        }

    }
}
