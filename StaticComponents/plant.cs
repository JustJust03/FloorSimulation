using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    /// <summary>
    /// The main plant class. 
    /// These should only be in trolley's plant lists.
    /// </summary>
    internal class plant
    {
        public int id = 0; //TODO: create a function to assign unique id's just like the trolleys
        public string name = "Plant_Name_Here"; //Assign plant names here 
        public Hub DestinationHub;
        public int ReorderTime = 1000; //ms

        public plant(Hub desthub) 
        { 
            DestinationHub = desthub;
        }
    }
}
