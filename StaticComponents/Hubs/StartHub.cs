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

        public void InitFirstTrolley()
        {
            //HubTrolleys[0].PlantList.Add(new plant(floor.HubList[7]));
            //HubTrolleys[0].PlantList.Add(new plant(floor.HubList[3]));
            //HubTrolleys[0].PlantList.Add(new plant(floor.HubList[3]));
            //HubTrolleys[0].PlantList.Add(new plant(floor.HubList[12]));
            HubTrolleys[0].PlantList.Add(new plant(floor.HubList[8]));

            HubTrolleys[1].PlantList.Add(new plant(floor.HubList[14]));
            HubTrolleys[2].PlantList.Add(new plant(floor.HubList[3]));
            HubTrolleys[3].PlantList.Add(new plant(floor.HubList[10]));
            HubTrolleys[4].PlantList.Add(new plant(floor.HubList[8]));
        }
    }
}
