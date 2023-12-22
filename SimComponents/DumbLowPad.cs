using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace FloorSimulation
{
    internal class DumbLowPad : Agent
    {
        public const float RIDESPEED = 110f; // cm/s
        public const int LPTileWidth = 7;
        public const int LPTileHeight = 8;
        public const int TrolleyTileWidth = 6;
        public const int TrolleyTileHeight = 14;

        public int TILEWIDTH = LPTileWidth;
        public int TILEHEIGHT = LPTileHeight;

        public LowPadAccessHub LPAHub;

        public DumbLowPad(int id_, Floor floor_, WalkWay WW_, Point Rpoint_ = default, bool IsVertical_ = true, int MaxWaitedTicks_ = 100) :
            base(id_, floor_, WW_, "LowPad", RIDESPEED, Rpoint_, IsVertical_, MaxWaitedTicks_)
        {
            MainTask = new DumbLowPadTask(this, floor, "");

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

        public override void TakeTrolleyIn(DanishTrolley t)
        {
            WW.unfill_tiles(RPoint, GetRSize());
            WW.unfill_tiles(trolley.RPoint, GetRSize());

            trolley.RPoint = WW.GetTile(trolley.RPoint).Rpoint;
            trolley = t;
            RPoint = t.RPoint;

            TILEWIDTH = TrolleyTileWidth;
            TILEHEIGHT = TrolleyTileHeight;
        }

        public override DanishTrolley GiveTrolley()
        {
            TILEWIDTH = LPTileWidth;
            TILEHEIGHT = LPTileHeight;

            return base.GiveTrolley();
        }

        public override Size GetRSize(bool OnlyAgentSize = false)
        {
            if (trolley != null)
                return trolley.GetRSize();
            else return base.GetRSize();
        }

        public override void TickWalk()
        {
            ticktravel += travel_dist_per_tick * floor.SpeedMultiplier;
            while (ticktravel > WalkWay.WALK_TILE_WIDTH)
            {
                WW.unfill_tiles(RPoint, GetRSize());
                if (MainTask.LowpadDeltaX == -1)
                {
                    if (WW.WWC.DumbLPClearanceLeft(this))
                        RPoint.X -= 10;
                    else
                    {
                        //try to grab a new trolley from the starthub
                        if(trolley == null)
                        {
                            trolley = floor.STHubs[0].DumbLowPadPickUp(this);
                            if(trolley != null)
                            {
                                TakeTrolleyIn(trolley);
                                MainTask.LowpadDeltaX = 0;
                                MainTask.LowpadDeltaY = -1;
                            }
                        }
                    }
                        
                }
                else if (MainTask.LowpadDeltaX == 1)
                {
                    if (WW.WWC.DumbLPClearanceRight(this))
                        RPoint.X += 10;
                }
                else if (MainTask.LowpadDeltaY == -1)
                {
                    if (WW.WWC.DumbLPClearanceUp(this))
                        RPoint.Y -= 10;
                }
                else if (MainTask.LowpadDeltaY == 1)
                {
                    if (WW.WWC.DumbLPClearanceDown(this))
                        RPoint.Y += 10;
                }

                WW.fill_tiles(RPoint, GetRSize(), this);
                if (trolley != null)
                {
                    trolley.RPoint = RPoint;
                    trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);
                }
                else
                    SimPoint = floor.ConvertToSimPoint(RPoint);

                floor.layout.LPDriveLines.HitDriveLine(this);
                ticktravel -= WalkWay.WALK_TILE_WIDTH;
            }
        }

        public void HitAccessHub()
        {
            if(LPAHub != default && trolley != null)
            {
                if(trolley.TargetRegions.Count == 1 && trolley.TargetRegions[0] == LPAHub)
                {
                    LPAHub.TakeVTrolleyIn(GiveTrolley());
                    MainTask.LowpadDeltaX = LPAHub.HasLeftAccess ? -1 : 1;
                }
                else if(trolley.TargetRegions.Count == 1)
                {
                    ;
                }
                else
                {
                    LPAHub.TakeVTrolleyIn(trolley);
                    trolley.ContinueDistribution = false;
                }
            }
        }

        public void FinishedRegion()
        {
            if(LPAHub != default)
            {
                LPAHub.Targeted = false;
                LPAHub = default;
            }
        }
    }
}
