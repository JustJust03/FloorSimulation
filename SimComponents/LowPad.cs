using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation
{
    internal class LowPad : Agent
    {
        public const float RIDESPEED = 65f; // cm/s

        public LowPad(int id_, Floor floor_, WalkWay WW_, Point Rpoint_ = default, bool IsVertical_ = true, int MaxWaitedTicks_ = 100):
            base(id_, floor_, WW_, "LowPad", RIDESPEED, Rpoint_, IsVertical_, MaxWaitedTicks_)
        {
            MainTask = new LowPadTask(this, floor, "TakeFullTrolley");
        }

        public override void DrawObject(Graphics g, Point p = default)
        {
            if (trolley != null)
            {
                if (IsVertical)
                    DrawPoint = floor.ConvertToSimPoint(new Point(RPoint.X - 5, RPoint.Y + 30));
                else
                    DrawPoint = floor.ConvertToSimPoint(new Point(RPoint.X + 30, RPoint.Y - 5));
            }
            else
                DrawPoint = SimPoint;

            if(IsVertical)
                g.DrawImage(VAgentIMG, new Rectangle(DrawPoint, SimAgentSize));
            else
                g.DrawImage(HAgentIMG, new Rectangle(DrawPoint, SimAgentSize));

            if(trolley != null)
                trolley.DrawObject(g);
        }

        public override Size GetRSize(bool OnlyAgentSize = false)
        {
            if (trolley != null)
                return trolley.GetRSize();
            else return base.GetRSize();
        }

        public override void TickWalk()
        {
            if (trolley == null)
            {
                base.TickWalk();
                return;
            }

            if (route == null)
            {
                MainTask.FailRoute();
                return;
            }

            if (route.Count > 0)
            {
                ticktravel += travel_dist_per_tick * floor.SpeedMultiplier;
                while (ticktravel > WalkWay.WALK_TILE_WIDTH)
                {
                    WalkTile destination = route[0];

                    WW.WWC.UpdateLocalClearances(this, GetTileSize(), destination);

                    if (!AWW.IsTileAccessible(destination)) //Route failed, there was something occupying the calculated route
                    {
                        ticktravel = 0;
                        MainTask.FailRoute();
                        return;
                    }

                    WW.unfill_tiles(RPoint, GetRSize());
                    RPoint = route[0].Rpoint;
                    WW.fill_tiles(RPoint, GetRSize(), this);
                    trolley.RPoint = RPoint;
                    trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);

                    ticktravel -= WalkWay.WALK_TILE_WIDTH;
                    route.RemoveAt(0);

                    if (route.Count == 0)
                    {
                        ticktravel = 0;
                        MainTask.RouteCompleted();
                        break;
                    }
                }
            }
            else
                MainTask.FailRoute();
        }

        public override void TakeTrolleyIn(DanishTrolley t)
        {
            trolley = t;
            RPoint = t.RPoint;
        }

        public override void TravelToTrolley(DanishTrolley t)
        {
            route = AWW.RunAlgoLowPadToTrolley(t);
        }

        public LowPadAccessHub ClosestRegion(List<LowPadAccessHub> Regions)
        {
            LowPadAccessHub ClosestHub = Regions[0];
            foreach (LowPadAccessHub LPhub in Regions)
                if (!LPhub.Targeted && Math.Abs(RPoint.X - LPhub.RFloorPoint.X) < Math.Abs(RPoint.X - ClosestHub.RFloorPoint.X))
                    ClosestHub = LPhub;
            if (ClosestHub.Targeted) //All regions were targeted already.
                return null;

            ClosestHub.Targeted = true;
            return ClosestHub;
        }
    }
}
