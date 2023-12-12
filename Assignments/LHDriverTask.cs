using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation.Assignments
{
    internal class LHDriverTask : Task
    {
        Floor floor;
        private Distributer DButer;
        private LangeHarry Harry;
        private int WaitedTicks = 0;

        public readonly List<string> InitGoals = new List<string> //Only distributeplants should be in this.
        {
            "EmptySmallBuffHub",
            "FillSmallBuffHub",
            "TravelToStartTile"
        };

        public LHDriverTask(Distributer DButer_, DanishTrolley trolley_ = default): 
            base(null, trolley_)
        {
            DButer = DButer_;
            floor = DButer.floor;
            DButer.MountHarry(floor.FirstHarry);
            Harry = DButer.Harry;
        }

        public override void PerformTask()
        {
            if (!InTask)
                GetNewGoal();
            else if (InTask && Travelling)
                DButer.TickWalk();

            if (Waiting)
            {
                if (DButer.route != null)
                {
                    WaitedTicks = 0;
                    Waiting = false;
                }
                WaitedTicks++;
                if (WaitedTicks > 100)
                {
                    WaitedTicks = 0;
                    DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
                    Waiting = false;
                    TargetWasSaveTile = true;
                }
                return;
            }
        }

        private void GetNewGoal()
        {
            InTask = true;
            Travelling = true;

            if (Harry.TrolleyList.Count > 0 && Harry.TrolleyList[0].PlantList.Count > 0)
            {
                TargetHub = DButer.floor.HasFullTrolleyHubFull(4) ?? TargetHub;
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null)
                    Waiting = true;
                return;
            }

            TargetHub = DButer.floor.HasFullSmallBufferHub(6);
            if (TargetHub != null)
            {
                Goal = "EmptySmallBuffHub";
                if(!DButer.Harry.IsVertical)
                    DButer.RotateDistributerAndHarry();
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if(DButer.route != null)
                    return;
            }

            if(DButer.floor.HasEmptySmallBufferHub(0) != null)
            {
                TargetHub = floor.BuffHubs[floor.BuffHubs.Count -1];
                if(TargetHub.AmountOfTrolleys() == 0)
                {
                    if(DButer.RPoint != DButer.SavePoint)
                    {
                        Goal = "TravelToStartTile";
                        DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
                        if(DButer.route != null)
                            return;
                    }
                }
                else
                {
                    Goal = "FillSmallBuffHub";
                    DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                    if(DButer.route != null)
                        return;
                }
            }

            TargetHub = DButer.floor.HasFullTrolleyHubFull(4);
            if (TargetHub != null)
            {
                Goal = "LHTakeFinishedTrolley";
                if(!Harry.IsVertical)
                    DButer.RotateDistributerAndHarry();
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if(DButer.route != null)
                    return;
            }

            if(DButer.RPoint != DButer.SavePoint)
            {
                Goal = "TravelToStartTile";
                DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
                if(DButer.route != null)
                    return;
            }
            InTask = false;
            Travelling = false;
        }

        public override void RouteCompleted()
        {
            if (Goal == "EmptySmallBuffHub")
                EmptySmallBuffHub();
            else if (Goal == "LHDeliverEmptyTrolleys")
                LHDeliverEmptyTrolleys();
            else if (Goal == "FillSmallBuffHub")
                FillSmallBuffHub();
            else if (Goal == "LHTakeFinishedTrolley")
                LHTakeFinishedTrolley();
            else if (Goal == "LHDeliverFinishedTrolleys")
                LHDeliverFinishedTrolleys();
            else if (Goal == "TravelToStartTile")
                TravelToStartTile();
            else
                throw new Exception("Task was not recognised1");
        }

        public override void FailRoute()
        {
            if (InitGoals.Contains(Goal))
                GetNewGoal();
            else if (TargetIsOpenSpots.Contains(Goal))
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
            else if (TargetIsFilledSpots.Contains(Goal))
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer)); //This needs to stay in!

            if (DButer.route == null)
                Waiting = true;
            else if (DButer.route.Count == 0)
                RouteCompleted();
            else
                DButer.TickWalk();
        }

        public override void DistributionCompleted()
        {
            throw new NotImplementedException();
        }

        private void EmptySmallBuffHub()
        {
            if (Harry.IsVertical == TargetHub.VerticalTrolleys)
                DButer.RotateDistributerAndHarry();

            Trolley = TargetHub.GiveTrolleyToHarry(DButer.RPoint);
            if (Trolley == null)
            {
                FailRoute();
                return;
            }
            Harry.TakeTrolleyIn(Trolley);

            if (TargetHub.AmountOfTrolleys() > 0 && Harry.TrolleyList.Count < LangeHarry.MaxTrolleysPerHarry)
            {
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null) //Route was not possible at this point
                    FailRoute();
            }
            else
            {
                TargetHub = floor.HasEmptySmallBufferHub(0);
                if(TargetHub == null)
                    TargetHub = DButer.floor.BuffHubs[DButer.floor.BuffHubs.Count - 1];
                if (!Harry.IsVertical)
                    DButer.RotateDistributerAndHarry();

                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer)); //Automaticaly checks if dbuter is on harry, which it is.
                Goal = "LHDeliverEmptyTrolleys";
                if (DButer.route == null) //Route was not possible
                    FailRoute();
            }

            InTask = true;
            Travelling = true;
        }

        private void FillSmallBuffHub()
        {
            if (Harry.IsVertical == TargetHub.VerticalTrolleys)
                DButer.RotateDistributerAndHarry();

            Trolley = TargetHub.GiveTrolleyToHarry(DButer.RPoint);
            if (Trolley == null)
            {
                FailRoute();
                return;
            }
            Harry.TakeTrolleyIn(Trolley);

            if (TargetHub.AmountOfTrolleys() > 0 && Harry.TrolleyList.Count < LangeHarry.MaxTrolleysPerHarry)
            {
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null) //Route was not possible at this point
                    FailRoute();
            }
            else
            {
                TargetHub = floor.HasEmptySmallBufferHub(0);
                if(TargetHub == null)
                    TargetHub = DButer.floor.BuffHubs[DButer.floor.BuffHubs.Count - 1];
                if (!Harry.IsVertical)
                    DButer.RotateDistributerAndHarry();

                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer)); //Automaticaly checks if dbuter is on harry, which it is.
                Goal = "LHDeliverEmptyTrolleys";
                if (DButer.route == null) //Route was not possible
                    FailRoute();
            }

            InTask = true;
            Travelling = true;

        }

        private void LHDeliverEmptyTrolleys()
        {
            if (Harry.IsVertical == TargetHub.VerticalTrolleys)
                DButer.RotateDistributerAndHarry();
            DanishTrolley t = Harry.DropTrolley();
            if (TargetHub.VerticalTrolleys)
                TargetHub.LHTakeVTrolleyIn(t, DButer.RPoint);
            else
                TargetHub.LHTakeHTrolleyIn(t, DButer.RPoint);



            if (Harry.TrolleyList.Count < 1)
            {
                Goal = "TravelToStartTile";
                InTask = false;
                Travelling = false;
            }
            else
            {
                if(TargetHub.AmountOfTrolleys() == 4)
                {
                    TargetHub = floor.HasEmptySmallBufferHub(0);
                    if(TargetHub == null)
                        TargetHub = floor.BuffHubs[floor.BuffHubs.Count - 1];
                }

                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
                if (DButer.route == null) //Route was not possible
                    FailRoute();
            }
        }

        private void LHTakeFinishedTrolley()
        {
            Harry.TakeTrolleyIn(TargetHub.GiveTrolley(DButer.RPoint));

            if (TargetHub.AmountOfTrolleys() > 0 && Harry.TrolleyList.Count < LangeHarry.MaxTrolleysPerHarry)
            {
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null) //Route was not possible at this point
                    FailRoute();
            }
            else
            {
                TargetHub = DButer.floor.TrHub;
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
                Goal = "LHDeliverFinishedTrolleys";
                if (DButer.route == null) //Route was not possible
                    FailRoute();
            }

            InTask = true;
            Travelling = true;
        }

        private void LHDeliverFinishedTrolleys()
        {
            if (Harry.IsVertical == TargetHub.VerticalTrolleys)
                DButer.RotateDistributerAndHarry();
            DanishTrolley t = Harry.DropTrolley();
            TargetHub.TakeVTrolleyIn(t, DButer.RPoint);


            if (Harry.TrolleyList.Count < 1)
            {
                Goal = "TravelToStartTile";
                InTask = false;
                Travelling = false;
            }
            else
            {
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
                if (DButer.route == null) //Route was not possible
                    FailRoute();
            }

            InTask = true;
            Travelling = true;
        }

        private void TravelToStartTile()
        {
            GetNewGoal();
        }
    }
}
