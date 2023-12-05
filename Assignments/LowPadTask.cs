using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class LowPadTask : Task
    {
        Floor floor;
        LowPad LP;


        public LowPadTask(LowPad LP_, Floor floor_, string Goal_, DanishTrolley trolley_ = default): 
            base(Goal_, trolley_)
        {
            floor = floor_;

        }

        public override void PerformTask()
        {
            /*
            if (!InTask && Goal == "TakeFullTrolley")
            {
                TargetHub = floor.GetStartHub(DButer);
                Trolley = TargetHub.PeekFirstTrolley();
                if (Trolley != null)
                {
                    LowPad.TravelToTrolley(Trolley);
                    if (DButer.route == null) //Route was not possible at this point. Try again later.
                        return;
                    InTask = true;
                    Travelling = true;
                }
                else //When the distribution is finished, travel to your savepoint.
                {
                    DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
                    InTask = true;
                    Travelling = true;
                    TargetWasSaveTile = true;
                }
            }
            */
            
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
