﻿using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    /// <summary>
    /// Start hub. This is where the distributers get their trolleys to distribute them.
    /// </summary>
    internal class StartHub: Hub
    {
        private List<DanishTrolley> UndistributedTrolleys;

        public StartHub(string name_, int id_, Point FPoint_, Floor floor_, int initial_trolleys_ = 0, bool vertical_trolleys_ = true) : 
            base(name_, id_, FPoint_, floor_, new Size(400, 200), initial_trolleys:initial_trolleys_, vertical_trolleys:vertical_trolleys_)
        {
            UndistributedTrolleys = new List<DanishTrolley>();
        }

        public void AddUndistributedTrolleys(List<DanishTrolley> UT)
        {
            UndistributedTrolleys = UndistributedTrolleys.Union(UT).ToList();

            int take_amount = Math.Min(5, UndistributedTrolleys.Count);
            HubTrolleys = UndistributedTrolleys.Take(take_amount).ToList();
            UndistributedTrolleys.RemoveRange(0, take_amount);

            PlaceTrolleys();
        }

        public override DanishTrolley GiveTrolley(Point AgentRPoint = default)
        {
            if(HubTrolleys.Count == 1 && UndistributedTrolleys.Count > 0)
            {
                int take_amount = Math.Min(4, UndistributedTrolleys.Count);
                HubTrolleys = UndistributedTrolleys.Take(take_amount).ToList();
                UndistributedTrolleys.RemoveRange(0, take_amount);

                PlaceTrolleys();
            }
            return base.GiveTrolley(AgentRPoint);
        }

        private void PlaceTrolleys()
        {
            int LeftX = RFloorPoint.X; //Start from the left of the hub, and keep track of where to place the trolley.
            int Rslack = 20; //The real slack in all dimensions.
            for (int i = 0; i < HubTrolleys.Count; i++)
            {
                DanishTrolley t = HubTrolleys[i];
                t.IsVertical = true;

                int trolleyX = LeftX + Rslack; 
                LeftX += t.GetRSize().Width + Rslack;
                int trolleyY = RFloorPoint.Y + Rslack;

                t.TeleportTrolley(new Point(trolleyX, trolleyY));
                WW.fill_tiles(t.RPoint, t.GetRSize());
            }
        }
    }
}
