using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FloorSimulation
{
    /// <summary>
    /// The main plant class. 
    /// These should only be in trolley's plant lists.
    /// </summary>
    internal class plant
    {
        public int id = 0; //TODO: create a function to assign unique id's just like the trolleys
        public string name;//Assign plant names here 
        public ShopHub DestinationHub;
        public const int ReorderTime = 27000; //ms
        //public const int ReorderTime = 3; //ms
        public int units;

        public plant(ShopHub desthub, int units_, string name_ = "Plant_Name_Here") 
        { 
            DestinationHub = desthub;
            units = units_;
            name = name_;
        }

        public override string ToString()
        {
            return name + " Destination: " + DestinationHub.ToString();
        }
    }
}
