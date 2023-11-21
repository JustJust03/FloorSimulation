using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation
{
    internal class RechtHoekLayout : Layout
    {
        int StreetWidth = 2500;
        int ShopHeight = 80;
        int ShopWidth = 160;
        private int NtrolleysPerShop = 1;
        int MostLeftX; //Most leftx of the placed shop hubs.
        int MostRightX; //Most rightx used by the shop hubs.
        int UppestY; //Most Up y placed shop hub.
        int LowestY; //Most low y used by the shop hubs.

        public RechtHoekLayout(Floor floor_, ReadData rData) : base(floor_, rData)
        { 

        }

        public override int NTrolleysInShop 
        {
            get { return NtrolleysPerShop; }
            set { NtrolleysPerShop = value; }
        }

        public override string ToString()
        {
            return "RechtHoek: Eerste dag boven";
        }

        public override StartHub GetStartHub(Distributer db)
        {
            //throw new NotImplementedException();
            return null;
        }

        public override BufferHub GetBuffHubOpen(Distributer db)
        {
            //throw new NotImplementedException();
            return null;
        }

        public override BufferHub GetBuffHubFull(Distributer db)
        {
            //throw new NotImplementedException();
            return null;
        }

        public override void DistributeTrolleys(List<DanishTrolley> dtList)
        {
            //throw new NotImplementedException();
            return;
        }

        public override void PlaceFullTrolleyHubs()
        {
            //throw new NotImplementedException();
            return;
        }

        public override void PlaceShops(List<ShopHub> Shops, int UpperY, int LowerY)
        {
            List<ShopHub> FirstDayShops = Shops.Where(s => RData.days.IndexOf(s.day) == 0).ToList();
            List<ShopHub> SecondDayShops = Shops.Where(s => RData.days.IndexOf(s.day) == 1).ToList();

            PlaceShopsPerDay(FirstDayShops, 1);
            SecondDayShops.Reverse();
            PlaceShopsPerDay(SecondDayShops, -1);
            ;
        }

        /// <summary>
        /// z = 1 for the first day (place up), -1 for the second day (place down).
        /// You should reverse the second day shops.
        /// </summary>
        private void PlaceShopsPerDay(List<ShopHub> Shops, int z = 1)
        {
            if (!(z == 1 || z == -1))
                throw new Exception("Z should be either 1 or -1");
            int ShopsToPlaceInMiddle = StreetWidth / ShopHeight;
            int ShopsToPlaceOnRight = (Shops.Count - ShopsToPlaceInMiddle) / 2;
            int ShopsToPlaceOnLeft = Shops.Count - ShopsToPlaceOnRight - ShopsToPlaceInMiddle;

            int x = 600;
            MostLeftX = x;
            int y = floor.FirstWW.RSizeWW.Height / 2 + (-1 * z) * ShopWidth;
            int overali = 0;
            for (int i = overali; i < ShopsToPlaceOnLeft; i++)
            {
                ShopHub Shop = Shops[overali];
                y += (-1 * z) * ShopHeight;
                Shop.TeleportHub(new Point(x, y));

                floor.HubList.Add(Shop);
                overali++;
            }


            int sheight = ShopHeight;
            ShopHeight = ShopWidth;
            ShopWidth = sheight;

            x += ShopHeight;
            if (z == 1)
            {
                y -= ShopHeight;
                UppestY = y;
            }
            else if (z == -1)
                y += ShopWidth;

            for(int i = 0; i < ShopsToPlaceInMiddle; i++)
            {
                ShopHub Shop = Shops[overali];
                Shop.RotateAndTeleportHub(new Point(x, y));
                x += ShopWidth;

                floor.HubList.Add(Shop);
                overali++;
            }

            sheight = ShopHeight;
            ShopHeight = ShopWidth;
            ShopWidth = sheight;

            if(z == 1)
                y += ShopWidth;
            else if (z == -1)
            {
                LowestY = y + ShopWidth;
                y -= ShopHeight;
            }

            for(int i = 0; i < ShopsToPlaceOnRight; i++)
            {
                ShopHub Shop = Shops[overali];
                Shop.TeleportHub(new Point(x, y));
                y += z * ShopHeight;

                floor.HubList.Add(Shop);
                overali++;
            }

            MostRightX = x + ShopWidth;
        }

        public override void SortPlantLists(List<DanishTrolley> dtList)
        {
            //throw new NotImplementedException();
            return;
        }

        public override void PlaceStartHubs()
        {
            Point firstp = new Point(((MostRightX - MostLeftX) / 2) + MostLeftX - 400, floor.FirstWW.RSizeWW.Height / 2 - 200);
            Point secondp = new Point(((MostRightX - MostLeftX) / 2) + MostLeftX - 400, floor.FirstWW.RSizeWW.Height / 2);
            floor.STHubs.Add(new StartHub("Start hub", 0, firstp, floor, vertical_trolleys_: true));
            floor.STHubs.Add(new StartHub("Start hub", 1, secondp, floor, vertical_trolleys_: true));

            base.PlaceStartHubs();
            return;
        }

        public override void PlaceBuffHubs()
        {
            Point firstp = new Point(MostLeftX - 20 - 200, UppestY + ShopWidth);
            Point secondp = new Point(MostLeftX - 20 - 200, UppestY + ShopWidth);
            floor.BuffHubs.Add(new BufferHub("Buffer hub", 0, firstp, new Size(200, floor.FirstWW.RSizeWW.Height / 2 - UppestY - 2 * ShopWidth), floor, vertical_trolleys_:false));

            base.PlaceBuffHubs();
            return;
        }
    }
}
