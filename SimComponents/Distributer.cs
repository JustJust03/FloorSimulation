using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using FloorSimulation.Assignments;

namespace FloorSimulation
{
    /// <summary>
    /// Main distibuter class.
    /// The agent which walks trough the floor and distributes the plants for the trolley.
    /// </summary>
    internal class Distributer : Agent
    {
        public const float OddsOfBord = 0.065f;
        public const float OddsOfLaag = 0.15f;
        public const float OddsOfHer = 0.28f;
        public const int BordTime = 34000; //ms
        public const int LaagTime = 30000; //ms
        public const int HerTime = 16000; //ms

        public const float WALKSPEED = 122f; // cm/s
        public int distributionms_per_tick; // plant distribution per tick in ms
        public int SideActivityMsLeft; // Amount of ticks left of performing the side activity
        private int distributionms = 0; // How many ms have you been distributing
        public LangeHarry Harry;
        public bool TrolleyOnTopLeft; //True when the trolley is to the left or on top of the distributer
        public string SideActivity;

        public LowPadAccessHub[] RegionHubs;
        public plant PlantInHand;

        
        public Distributer(int id_, Floor floor_, WalkWay WW_, Point Rpoint_ = default, bool IsVertical_ = true, int MaxWaitedTicks_ = 100, LowPadAccessHub[] RHubs = null):
            base(id_, floor_, WW_, "Distributer", WALKSPEED, Rpoint_, IsVertical_, MaxWaitedTicks_)
        {
            IsOnHarry = false;
            if(id == -1)
                return;

            distributionms_per_tick = (int)(1000f / Program.TICKS_PER_SECOND);
            RegionHubs = RHubs;

            if (floor.layout.NLowpads == 0 && id != -8)
                MainTask = new DistributerTask(this, "TakeFullTrolley", floor.FinishedD);
            else if(id_ == -8)
                MainTask = new LHDriverTask(this);
            else
                MainTask = new RegionDistributerTask(this, "TravelToLP", floor.FinishedD, RegionHubs);
        }

        /// <summary>
        /// Makes the distributer walk towards LangeHarry using it's accesspoints.
        /// </summary>
        public override void TravelToHarry(LangeHarry Harry)
        {
            route = AWW.RunAlgoDistrToHarry(Harry);
        }

        public override void TravelHarry()
        {
            Harry.TravelHarry();
        }

        public void TickDistribute()
        {
            if(SideActivityMsLeft > 0)
            {
                TickSideActivity();
                return;
            }

            distributionms += distributionms_per_tick * floor.SpeedMultiplier;
            if (distributionms >= plant.ReorderTime)
            {
                distributionms = 0;
                if (RollForSideActivity())
                    return;
                MainTask.DistributionCompleted();
            }
        }

        public void TickSideActivity()
        {
            SideActivityMsLeft -= distributionms_per_tick * floor.SpeedMultiplier;
            if (SideActivityMsLeft <= 0)
            {
                SideActivity = null;
                MainTask.DistributionCompleted();
            }
        }

        /// <summary>
        /// Uses random to determine if you need to perform a side activity.
        /// Adds the amount of time to SideActivityMsLeft.
        /// </summary>
        private bool RollForSideActivity()
        {
            //Nieuwe laag bijzetten
            int r = floor.rand.Next(0, 1000);
            if (r <= OddsOfLaag * 1000)
            {
                SideActivityMsLeft += LaagTime;
                SideActivity = "Laag";
            }
            //De kar herindelen
            r = floor.rand.Next(0, 1000);
            if (r <= OddsOfHer * 1000)
            {
                SideActivityMsLeft += HerTime;
                SideActivity = "Her";
            }

            if (SideActivityMsLeft > 0)
            {
                MainTask.AInfo.UpdateFreq(MainTask.Goal, true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Takes a trolley in.
        /// Changes the occupied_by to this distributer.
        /// </summary>
        /// <param name="t">Trolley to take in</param>
        public override void TakeTrolleyIn(DanishTrolley t)
        {
            ChangeTravelSpeed(DanishTrolley.TrolleyTravelSpeed);
            trolley = t;

            //Rotate the distributer if the trolley to take in is not his orientation
            if (IsVertical != trolley.IsVertical)
            {
                RotateAgentOnly();
                if(trolley.IsVertical && RPoint.Y < trolley.RPoint.Y)
                    TrolleyOnTopLeft = true;
                else if (!trolley.IsVertical && RPoint.X < trolley.RPoint.X)
                    TrolleyOnTopLeft = true;
                else //Switches the trolley and the distributer
                {
                    //TODO: Rebuild your system so trolleys can also be transported from the bottom right.
                    SwitchDistributerTrolley();
                }
            }
            else
            {
                if (RPoint.X < trolley.RPoint.X)
                    TrolleyOnTopLeft = true;
                else
                {
                    SwitchDistributerTrolley();
                    return;
                }
            }

            trolley.IsInTransport = true;

            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());
            TravelTrolley();
            WW.fill_tiles(trolley.RPoint, trolley.GetRSize(), this);
        }

        public void SwitchDistributerTrolley()
        {
            if (trolley.RPoint.X - RPoint.X > 10) //Distributer is on the left of the trolley
                SwitchDistrToRightOfTrolley();
            else if(RPoint.X - trolley.RPoint.X > 10) //Distributer is on the right of the trolley
                SwitchDistrToLeftOfTrolley();
            else if(RPoint.Y - trolley.RPoint.Y > 10) //Distributer is on the bottom of the trolley
                SwitchDistrToTopOfTrolley();
            else if(trolley.RPoint.Y - RPoint.Y > 10)
                SwitchDistrToBotOfTrolley();
        }

        private void SwitchDistrToLeftOfTrolley()
        {
            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());
            WW.unfill_tiles(RPoint, GetRSize(OnlyAgentSize: true));

            TrolleyOnTopLeft = false;
            RPoint = trolley.RPoint;

            trolley.RPoint.X = RPoint.X + GetRSize(OnlyAgentSize: true).Width;
            SimPoint = floor.ConvertToSimPoint(RPoint);
            trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);

            WW.fill_tiles(RPoint, GetRSize());
        }

        private void SwitchDistrToRightOfTrolley()
        {
            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());
            WW.unfill_tiles(RPoint, GetRSize());

            TrolleyOnTopLeft = true;
            trolley.RPoint.X = RPoint.X;

            RPoint.X = trolley.RPoint.X + trolley.GetRSize().Width + 10;
            SimPoint = floor.ConvertToSimPoint(RPoint);
            trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);

            WW.fill_tiles(RPoint, GetRSize(OnlyAgentSize: true));
        }

        private void SwitchDistrToTopOfTrolley()
        {
            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());
            WW.unfill_tiles(RPoint, new Size(HRAgentSize.Width + 10, VRAgentSize.Height));

            TrolleyOnTopLeft = false;
            RPoint = trolley.RPoint;
            
            trolley.RPoint.Y = RPoint.Y + 10;
            SimPoint = floor.ConvertToSimPoint(RPoint);
            trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);

            WW.fill_tiles(RPoint, GetRSize());
        }

        private void SwitchDistrToBotOfTrolley()
        {
            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());
            WW.unfill_tiles(RPoint, GetRSize());

            TrolleyOnTopLeft = true;
            trolley.RPoint = RPoint;
            
            RPoint.Y = trolley.RPoint.Y + trolley.GetRSize().Height + 10;
            SimPoint = floor.ConvertToSimPoint(RPoint);
            trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);

            WW.fill_tiles(RPoint, GetRSize());
        }

        /// <summary>
        /// Leaves the trolley behind at the current point
        /// And teleports the distributer up
        /// </summary>
        /// <returns></returns>
        public override DanishTrolley GiveTrolley()
        {
            ChangeTravelSpeed(WALKSPEED);
            return base.GiveTrolley();
        }

        /// <summary>
        /// Including The trolley if it has a trolley
        /// </summary>
        /// <returns>The Real distributer Size</returns>
        public override Size GetRSize(bool OnlyAgentSize = false)
        {
            if (OnlyAgentSize)
                return base.GetRSize();
            if (IsOnHarry)
                return Harry.GetRSize();
            if (trolley == null)
                return base.GetRSize();
            if(trolley.IsVertical)
                return new Size(Math.Max(VRAgentSize.Width, trolley.GetRSize().Width), VRAgentSize.Height + trolley.GetRSize().Height);
            
            return new Size(HRAgentSize.Width + trolley.GetRSize().Width, Math.Max(HRAgentSize.Height, trolley.GetRSize().Height));
        }

        protected override void RotateAgentOnly()
        {
            WW.unfill_tiles(RPoint, GetRSize(OnlyAgentSize: true));
            IsVertical = !IsVertical;

            SimAgentSize = floor.ConvertToSimSize(GetRSize(OnlyAgentSize: true));
            WW.fill_tiles(RPoint, GetRSize(OnlyAgentSize: true), this);
        }

        public void RotateDistributerAndTrolley()
        {
            WW.unfill_tiles(RPoint, GetRSize());

            IsVertical = !IsVertical;
            trolley.IsVertical = !trolley.IsVertical;

            trolley.RPoint.X = RPoint.X + GetRSize(OnlyAgentSize: true).Width;
            trolley.RPoint.Y = RPoint.Y;
            trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);

            SimAgentSize = floor.ConvertToSimSize(GetRSize(OnlyAgentSize: true));
            WW.fill_tiles(RPoint, GetRSize(), this);
        }

        public void RotateDistributerAndHarry()
        {
            WW.unfill_tiles(RPoint, GetRSize());
            IsVertical = !IsVertical;
            Harry.IsVertical = !Harry.IsVertical;
            SimAgentSize = floor.ConvertToSimSize(GetRSize(OnlyAgentSize: true));

            Harry.RotateTrolleys();

            WW.fill_tiles(RPoint, GetRSize(), this);
        }

        public void MountHarry(LangeHarry Harry_)
        {
            ChangeTravelSpeed(LangeHarry.HarryTravelSpeed);
            if (IsVertical != Harry_.IsVertical)
                RotateAgentOnly();
            Harry = Harry_;

            WW.unfill_tiles(RPoint, GetRSize(OnlyAgentSize: true));
            WW.unfill_tiles(Harry.RPoint, Harry.GetRSize());
            IsOnHarry = true;
            Harry.IsInUse = true;
            Harry.DButer = this;
            RPoint = Harry.RPoint;

            WW.fill_tiles(RPoint, GetRSize(), this);
        }
        
        public void DisMountHarry()
        {
            ChangeTravelSpeed(WALKSPEED);
            WW.unfill_tiles(RPoint, GetRSize());

            Harry.DButer = null;
            Harry.IsInUse = false;
            Harry.IsTargeted = false;
            if (IsVertical)
                RPoint = new Point(Harry.RPoint.X - VRAgentSize.Width, Harry.RPoint.Y + 57 * LangeHarry.MaxTrolleysPerHarry);
            else
                RPoint = new Point(Harry.RPoint.X + 20, Harry.RPoint.Y - HRAgentSize.Height);
            SimPoint = floor.ConvertToSimPoint(RPoint);

            WW.fill_tiles(Harry.RPoint, Harry.GetRSize());
            Harry = null;
            IsOnHarry = false;
            WW.fill_tiles(RPoint, GetRSize(OnlyAgentSize: true), this);
        }

        /// <summary>
        /// Reshuffles the plant list of the trolley to make sure the next plant has a targethub with a trolley.
        /// switches the first plant in the list with the next first plant that has a targethub with a trolley.
        /// Returns true when a reshuffle was possible, false if not.
        /// </summary>
        public bool ReshufflePlants()
        {
            for (int i = 1; i < trolley.PlantList.Count; i++)
                if (trolley.PlantList[i].DestinationHub.PeekFirstTrolley() != null)
                {
                    trolley.SwitchPlants(0, i);
                    return true;
                }
            return false;
        }
    }
}
