using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    /// <summary>
    /// A hub where the trolleys of a shop are placed.
    /// Used to interact with distributers to obtain plants and switch out trolleys.
    /// trolleys (in horizontal position) are placed vertical below each other.
    /// 
    /// 2 carts real size = 200 x 200. 1 cart real size = 200 x 100
    /// </summary>
    internal class ShopHub : Hub
    {

        /// <summary>
        /// Shop hub has a standard size: (200cm x 200cm)
        /// Usually horizontal trolleys
        /// </summary>
        public ShopHub(string name_, int id_, Point FPoint_, Floor floor_, WalkWay ww_,
            int initial_trolleys = 0) : 
            base(name_, id_, FPoint_, floor_, ww_, new Size(160, 160), initial_trolleys: initial_trolleys)
        {

        }


    }
}
