﻿using System;
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
        public int RealFloorWidth = 5000;
        public int RealFloorHeight = 5000;

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
                db = new Distributer(i, floor, floor.FirstWW, Rpoint_: new Point(x, y), MaxWaitedTicks_: 100 - i);
                floor.TotalDistrList.Add(db);
                x += 200;
                if (x > floor.FirstWW.RSizeWW.Width - 250)
                {
                    x = StartPoint.X;
                    y += 200;
                    if(y > floor.FirstWW.RSizeWW.Height - 250)
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
