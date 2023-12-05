using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation
{
    internal class LowPadAccessHub : Hub
    {
        public LowPadAccessHub(string name_, int id_, Point FPoint_, Floor floor_, Size s) : 
            base(name_, id_, FPoint_, floor_, s, vertical_trolleys: true)
        { 

        }


    }
}
