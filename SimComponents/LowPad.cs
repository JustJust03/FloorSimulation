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

        public LowPad(int id_, Floor floor_, WalkWay WW_, Point Rpoint_ = default, bool IsVertical_ = true, int MaxWaitedTicks_ = 100):
            base(id_, floor_, WW_, "LowPad", Rpoint_, IsVertical_, MaxWaitedTicks_)
        {
            MainTask = new LowPadTask(this, floor, "TakeFullTrolley");
        }

        public override void TakeTrolleyIn(DanishTrolley t)
        {
            throw new NotImplementedException();    
        }
    }
}
