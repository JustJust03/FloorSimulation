using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Data;

namespace FloorSimulation
{
    internal abstract class Task
    {
        public DanishTrolley Trolley;
        public Hub TargetHub;
        public bool TargetWasSaveTile = false;

        public AnalyzeInfo AInfo;

        public readonly List<string> VerspillingTasks = new List<string>
        {
            "DeliveringEmptyTrolley",
            "TakeEmptyTrolley",
            "MoveEmptyTrolleyDown",
            "DeliverEmptyTrolleyToShop"
        };

        public bool InTask = false;
        public bool Travelling = false;
        public bool Waiting = false; // true when an agent is waiting for a possible route to it's destination.
        public bool InSideActivity = false;
        public string Goal; // "TakeFullTrolley", "DistributePlants", "DeliveringEmptyTrolley", "PushTrolleyAway", "TakeFinishedTrolley", "DeliverFullTrolley", "TakeLangeHarry", "TakeEmptyTrolley", "DeliverEmptyTrolleyToShop", "TakeOldTrolley"

        public readonly List<string> TargetIsHubGoals = new List<string> //Only distributeplants should be in this.
        {
            "DistributePlants"
        };
        public readonly List<string> TargetIsOldWalktile = new List<string>
        {
            "DeliverEmptyTrolleyToShop",
            "PushTrolleyAway"
        };
        public readonly List<string> TargetIsOpenSpots = new List<string>
        {
            "LHDeliverFinishedTrolleys",
            "LHDeliverEmptyTrolleys",
            "DeliverFullTrolley"
        };
        public readonly List<string> TargetIsFilledSpots = new List<string>
        {
            "LHTakeFinishedTrolley",
            "LHTakeEmptyTrolley"
        };
        public readonly List<string> TargetIsHarry = new List<string>
        {
            "TakeLangeHarry"
        };
        public readonly List<string> TargetIsTilesDownTile = new List<string>
        {
            "MoveEmptyTrolleyDown"
        };
        public readonly List<string> TargetIsStartHubs = new List<string>
        {
            "TakeFullTrolley"
        };
        public readonly List<string> TargetIsFullBuffHub = new List<string>
        {
            "TakeEmptyTrolley"
        };
        public readonly List<string> TargetIsOpenBuffHub = new List<string>
        {
            "DeliveringEmptyTrolley",
        };

        public Task(string Goal_, DanishTrolley trolley_ = default)
        {
            Trolley = trolley_;
            Goal = Goal_;
        }

        public abstract void PerformTask();
        public abstract void RouteCompleted();
        public abstract void FailRoute();
        public abstract void DistributionCompleted();
    }
}
