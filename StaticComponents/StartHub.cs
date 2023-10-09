using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    /// <summary>
    /// Start hub. This is where the distributers get their trolleys to distribute them.
    /// </summary>
    internal class StartHub: Hub
    {

        public StartHub(string name_, int id_, Point FPoint_, Floor floor_, WalkWay ww_, int initial_trolleys_ = 0, bool vertical_trolleys_ = true) : 
            base(name_, id_, FPoint_, floor_, ww_, new Size(400, 200), initial_trolleys:initial_trolleys_, vertical_trolleys:vertical_trolleys_)
        {
        }

        /// <summary>
        /// Takes the first trolley from the hub trolley list and deletes it.
        /// </summary>
        public DanishTrolley TakeTrolley()
        {
            DanishTrolley FirstTrolley = HubTrolleys[0];
            HubTrolleys.RemoveAt(0);

            return FirstTrolley;
        }
    }
}
