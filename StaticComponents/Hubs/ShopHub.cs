﻿using System;
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
        public ShopHub(string name_, int id_, Point FPoint_, Floor floor_, int initial_trolleys = 0, string ColliPlusDay_ = null) : 
            base(name_, id_, FPoint_, floor_, new Size(160, 160), initial_trolleys: initial_trolleys)
        {
            ColliPlusDay = ColliPlusDay_;
            int nstrips = ColliPlusDay.Split('-').Length;
            day = ColliPlusDay.Split('-')[nstrips - 1];
        }

        public override string ToString() 
        {
            return "Name: " + this.name + " \n\tID: " + this.id + "\n\tDay: " + day;
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
                DT.DrawObject(g);
        }

    }
}
