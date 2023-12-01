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
        private Floor floor;

        public RegionDistributerTask(Distributer db_) 
        { 
            DButer = db_;
            floor = DButer.floor;
        }

        public override void PerformTask()
        {
            throw new NotImplementedException();
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
