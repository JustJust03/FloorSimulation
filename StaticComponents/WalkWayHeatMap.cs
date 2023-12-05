using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FloorSimulation
{
    internal class WalkWayHeatMap
    {
        WalkWay WW;
        Floor floor;
        int NewTimeInterval; //When this time gets passed, update the heatmap

        public WalkWayHeatMap(WalkWay WW_, Floor floor_)
        {
            WW = WW_;
            floor = floor_;
        }

        public void TickHeatMap()
        {
            if(floor.ElapsedSimTime.TotalSeconds > NewTimeInterval)
            {
                NewTimeInterval += 1;
                if (!floor.StartHubsEmpty())
                {
                    foreach(Distributer db in floor.DistrList)
                        if(db.MainTask.Travelling)
                            WW.GetTile(db.RPoint).visits += 100;
                }
                else
                {
                    foreach(Distributer db in floor.DistrList)
                        if(db.MainTask.Travelling && db.MainTask.Goal != "TakeFullTrolley")
                            WW.GetTile(db.RPoint).visits += 100;

                }
            }
        }

        public int UpdateAverages()
        {
            foreach (List<WalkTile> l in WW.WalkTileList)
                foreach (WalkTile t in l)
                    t.UpdateAverageVisits();
            int[] SortedArray = WW.WalkTileList.SelectMany(row => row.Select(obj => obj.AverageVisits)).ToArray();
            Array.Sort(SortedArray);
            Array.Reverse(SortedArray);
            int Max = SortedArray[100];
            ;
            return Max;
        }
    }
}
