using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace FloorSimulation
{
    /// <summary>
    /// Start hub. This is where the distributers get their trolleys to distribute them.
    /// </summary>
    internal class StartHub : Hub
    {
        private List<DanishTrolley> UndistributedTrolleys;
        public bool StartHubEmpty;
        public int MaxStartHubTrolleys = 8;

        public StartHub(string name_, int id_, Point FPoint_, Size s_, Floor floor_, int initial_trolleys_ = 0, bool vertical_trolleys_ = true) : 
            base(name_, id_, FPoint_, floor_, s_, initial_trolleys:initial_trolleys_, vertical_trolleys:vertical_trolleys_)
        {
            UndistributedTrolleys = new List<DanishTrolley>();
            StartHubEmpty = true;
            if (!vertical_trolleys_)
                MaxStartHubTrolleys = 6;
        }


        public void AddUndistributedTrolleys(List<DanishTrolley> UT)
        {
            if (UT.Count == 0)
                StartHubEmpty = true;
            else
                StartHubEmpty = false;  

            UndistributedTrolleys = UndistributedTrolleys.Union(UT).ToList();

            int take_amount = Math.Min(MaxStartHubTrolleys, UndistributedTrolleys.Count);
            HubTrolleys = UndistributedTrolleys.Take(take_amount).ToList();
            UndistributedTrolleys.RemoveRange(0, take_amount);

            PlaceTrolleys();
        }

        public void AddUndistributedTrolleys(DanishTrolley UT)
        {
            StartHubEmpty = false;
            if (HubTrolleys.Count < MaxStartHubTrolleys)
            {
                HubTrolleys.Add(UT);
                PlaceTrolleys();
            }
            else
                UndistributedTrolleys.Add(UT);
        }

        public override DanishTrolley PeekFirstTrolley()
        {
            if (HubTrolleys.Count == 0 && UndistributedTrolleys.Count > 0)
            {
                int take_amount = Math.Min(MaxStartHubTrolleys, UndistributedTrolleys.Count);
                HubTrolleys = UndistributedTrolleys.Take(take_amount).ToList();
                UndistributedTrolleys.RemoveRange(0, take_amount);

                PlaceTrolleys();
            }

            return base.PeekFirstTrolley();
        }

        public override DanishTrolley GiveTrolley(Point AgentRPoint = default)
        {
            if (UndistributedTrolleys.Count == 0 && HubTrolleys.Count == 1)
                StartHubEmpty = true;
            floor.Display.InvalInfo();
            return base.GiveTrolley(AgentRPoint);
        }

        private void PlaceTrolleys()
        {
            int LeftX = RFloorPoint.X; //Start from the left of the hub, and keep track of where to place the trolley.
            int UpperY = RFloorPoint.Y;
            int Rslack = 20; //The real slack in all dimensions.
            for (int i = 0; i < HubTrolleys.Count; i++)
            {
                DanishTrolley t = HubTrolleys[i];
                t.IsVertical = VerticalTrolleys;

                int trolleyX = LeftX + Rslack; 
                int trolleyY = UpperY + Rslack;

                if(VerticalTrolleys)
                    LeftX += t.GetRSize().Width + Rslack;
                else
                    UpperY += t.GetRSize().Height + Rslack;

                t.TeleportTrolley(new Point(trolleyX, trolleyY));
                WW.fill_tiles(t.RPoint, t.GetRSize());
            }
        }

        public int TotalUndistributedTrolleys()
        {
            return UndistributedTrolleys.Count + HubTrolleys.Count;
        }

        public DanishTrolley DumbLowPadPickUp(DumbLowPad dlp)
        {
            if (HubTrolleys.Count == 0)
                return null;
            DanishTrolley t = HubTrolleys[HubTrolleys.Count - 1];
            //if (WW.GetTile(new Point(t.RPoint.X + t.GetRSize().Width, t.RPoint.Y + 20)) == WW.GetTile(dlp.RPoint) ||
            //    WW.GetTile(new Point(t.RPoint.X + t.GetRSize().Width, t.RPoint.Y + 20)).TileRight() == WW.GetTile(dlp.RPoint))
            if (dlp.RPoint.Y - 20 == t.RPoint.Y && dlp.RPoint.X > t.RPoint.X && dlp.RPoint.X < t.RPoint.X + 67)
            {
                floor.Display.InvalInfo();
                HubTrolleys.RemoveAt(HubTrolleys.Count - 1);

                if (HubTrolleys.Count == 0 && UndistributedTrolleys.Count > 0)
                {
                    HubTrolleys = UndistributedTrolleys.Take(1).ToList();
                    UndistributedTrolleys.RemoveRange(0, 1);
                    PlaceTrolleys();
                }
                else if(HubTrolleys.Count == 0 && UndistributedTrolleys.Count == 0)
                    StartHubEmpty = true;

                return t;
            }

            return null;
        }
    }
}
