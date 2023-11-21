using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

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

        public virtual void PlaceDistributers(int amount, Point StartPoint)
        {
            Distributer db;
            int y = StartPoint.Y;
            int x = StartPoint.X;

            for(int i  = 0; i < amount; i++)
            {
                db = new Distributer(0, floor, floor.FirstWW, Rpoint_: new Point(x, y));
                floor.DistrList.Add(db);
                x += 100;
                if (x > floor.FirstWW.RSizeWW.Width - 200)
                {
                    x = StartPoint.X;
                    y += 100;
                    if(y > floor.FirstWW.RSizeWW.Height - 200)
                        break;
                }
            }
        }

        public abstract void SortPlantLists(List<DanishTrolley> dtList);

        public abstract void DistributeTrolleys(List<DanishTrolley> dtList);

        public abstract StartHub GetStartHub(Distributer db);

        public abstract BufferHub GetBuffHubOpen(Distributer db);
        public abstract BufferHub GetBuffHubFull(Distributer db);

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
