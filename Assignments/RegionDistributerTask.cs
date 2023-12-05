using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class RegionDistributerTask : Task
    {
        private Distributer DButer;
        private FinishedDistribution FinishedD;

        public RegionDistributerTask(Distributer DButer_, string Goal_, FinishedDistribution FinishedD_, DanishTrolley trolley_ = default): 
            base(Goal_, trolley_)
        {
            DButer = DButer_;
            FinishedD = FinishedD_;
        }

        public override void PerformTask()
        {
            return;
            //throw new NotImplementedException();
        }

        public override void RouteCompleted()
        {
            throw new NotImplementedException();
        }

        public override void FailRoute()
        {
            throw new NotImplementedException();
        }

        public override void DistributionCompleted()
        {
            throw new NotImplementedException();
        }
    }
}
