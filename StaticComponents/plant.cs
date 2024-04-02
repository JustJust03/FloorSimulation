﻿using System;
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
        public const int ReorderTime = 24000; //ms
        //public const int ReorderTime = 0; //ms
        public int units;
        public int SingleUnits;
        public int MaxSingleUnits;

        public plant(ShopHub desthub, int units_, int SingleUnits_, int MaxSingleUnits_, string name_ = "Plant_Name_Here") 
        { 
            DestinationHub = desthub;
            units = units_;
            SingleUnits = SingleUnits_;
            MaxSingleUnits = MaxSingleUnits_;
            name = name_;
        }

        public override string ToString()
        {
            return name + " Destination: " + DestinationHub.ToString();
        }
    }
}
