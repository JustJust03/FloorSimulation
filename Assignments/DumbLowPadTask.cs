using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class DumbLowPadTask : Task
    {
        Floor floor;
        DumbLowPad LP;

        public const bool ContinueBeforeDistribution = false; //Travel to the next hub as soon as the distributer takes the plant.

        public DumbLowPadTask(DumbLowPad LP_, Floor floor_, string Goal_, DanishTrolley trolley_ = default) :
            base(Goal_, trolley_)
        {
            LP = LP_;
            floor = floor_;
        }

        public override void PerformTask()
        {
            if(LowpadDeltaX != 0 || LowpadDeltaY != 0)
                LP.TickWalk();
            else
            {
                if(LP.trolley != null && LP.trolley.ContinueDistribution && LP.LPAHub != null)
                {
                    LP.LPAHub.GiveTrolley();
                    LowpadDeltaX = LP.LPAHub.HasLeftAccess ? -1 : 1;
                }
            }
        }

        public override void RouteCompleted()
        {
            throw new NotImplementedException();
        }

        public override void DistributionCompleted()
        {
            throw new NotImplementedException();
        }

        public override void FailRoute()
        {
            throw new NotImplementedException();
        }
    }
}
