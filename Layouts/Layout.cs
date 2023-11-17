using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal abstract class Layout
    {
        protected Floor floor;
        protected ReadData RData;
        public abstract int NTrolleysInShop { get; set; }

        public Layout(Floor floor_, ReadData rData)
        {
            floor = floor_;
            RData = rData;
        }

        public abstract void PlaceShops(List<ShopHub> Shops, int UpperY, int LowerY);

        public abstract void SortPlantLists(List<DanishTrolley> dtList);

        public abstract void DistributeTrolleys(List<DanishTrolley> dtList);

        public abstract StartHub GetStartHub(Distributer db);

        public abstract BufferHub GetBuffHub(Distributer db);

        public virtual void PlaceFullTrolleyHubs()
        {
            floor.HubList = floor.HubList.Concat(floor.FTHubs).ToList();
        }

        public virtual void PlaceStartHubs()
        {
            floor.HubList = floor.HubList.Concat(floor.STHubs).ToList();
        }

        public virtual void PlaceBuffHubs()
        {
            floor.HubList = floor.HubList.Concat(floor.BuffHubs).ToList();
        }
    }
}
