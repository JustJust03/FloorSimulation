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
        public int StickersToReceive; //While reading the data, the amount of stickers this shop gets is updated.

        public bool DrawRegions = false;
        public bool RegionStartOrEnd = false;

        /// <summary>
        /// Shop hub has a standard size: (200cm x 200cm)
        /// Usually horizontal trolleys
        /// </summary>
        public ShopHub(string name_, int id_, Point FPoint_, Floor floor_, Size s, int initial_trolleys = 0, string ColliPlusDay_ = null, bool HorizontalTrolleys_ = true) : 
            base(name_, id_, FPoint_, floor_, s, initial_trolleys: initial_trolleys, vertical_trolleys: !HorizontalTrolleys_)
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

        public override List<WalkTile> OpenSpots(Agent agent)
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
            WW.unfill_tiles(t.RPoint, t.GetRSize());
            if(HasLeftAccess)
                t.RPoint = new Point(RFloorPoint.X + Rslack, RFloorPoint.Y + Rslack);
            else
            {
                t.RPoint = new Point(RFloorPoint.X + Rslack, RFloorPoint.Y + Rslack);
            }
            WW.fill_tiles(t.RPoint, t.GetRSize());
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
            {
                if(HasLeftAccess) //Lower access
                    Accesspoint[0] = WW.GetTile(new Point(NewRPoint.X + 10, NewRPoint.Y + RHubSize.Height));
                else //Upper access
                    Accesspoint[0] = WW.GetTile(new Point(NewRPoint.X + 10, NewRPoint.Y - 40));
            }
            else
            {
                if (HasLeftAccess)
                    Accesspoint[0] = WW.GetTile(new Point(NewRPoint.X - 40, NewRPoint.Y + 10));
                else
                    Accesspoint[0] = WW.GetTile(new Point(NewRPoint.X + RHubSize.Width, NewRPoint.Y - 10));
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
            if (DrawRegions)
            {
                if(RegionStartOrEnd) 
                    g.DrawRectangle(floor.RedOrangePen, new Rectangle(FloorPoint, HubSize));
                else
                    g.DrawRectangle(floor.BPen, new Rectangle(FloorPoint, HubSize));

            }
            //outline
            else if (DrawOutline)
            {
                if(day == "DI")
                    g.DrawRectangle(floor.YellowPen, new Rectangle(FloorPoint, HubSize));
                else if(day == "DI_2")
                    g.DrawRectangle(floor.LightYellowPen, new Rectangle(FloorPoint, HubSize));
                else if (day == "WO")
                    g.DrawRectangle(floor.BluePen, new Rectangle(FloorPoint, HubSize));
                else if (day == "DO")
                    g.DrawRectangle(floor.GreenPen, new Rectangle(FloorPoint, HubSize));
                else if (day == "VR")
                    g.DrawRectangle(floor.PurplePen, new Rectangle(FloorPoint, HubSize));
                else if (day == "VR_2")
                    g.DrawRectangle(floor.PinkPen, new Rectangle(FloorPoint, HubSize));
                else if (day == "ZO")
                    g.DrawRectangle(floor.RedOrangePen, new Rectangle(FloorPoint, HubSize));
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
