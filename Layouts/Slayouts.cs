using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

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

    internal class SLayout : Layout
    {
        private int UpperY;
        private int LowestY;
        private List<int> ShopCornersX = new List<int>(); //Keeps track of the upper corners of the shops in the street. Is used to place FullHubs
        protected int StreetWidth = 800;
        protected int ShopHeight = 170;
        private int NtrolleysPerShop = 2;

        public SLayout(Floor floor_, ReadData rData) : base(floor_, rData) 
        { 

        }

        public override int NTrolleysInShop 
        {
            get { return NtrolleysPerShop; }
            set { NtrolleysPerShop = value; }
        }


        public override void PlaceShops(List<ShopHub> Shops, int UpperY_, int LowerY)
        {
            UpperY = UpperY_;
            int x = 0;
            int y = LowerY;
            int two_per_row = 1; //Keeps track of how many cols are placed without space between them
            int placed_shops_in_a_row = 0;

            bool FirstColFinished = false;
            for (int i = 0; i < Shops.Count;  i++) 
            { 
                ShopHub Shop = Shops[i];
                Shop.TeleportHub(new Point(x, y));
                if (two_per_row == 2)
                    Shop.HasLeftAccess = true;
                
                if(y == UpperY)
                {
                    if (Shop.HasLeftAccess)
                        ShopCornersX.Add(Shop.RFloorPoint.X);
                    else
                        ShopCornersX.Add(Shop.RFloorPoint.X + Shop.RHubSize.Width);
                }

                placed_shops_in_a_row++;
                if (placed_shops_in_a_row == 9 && FirstColFinished)
                {
                    placed_shops_in_a_row = 0;
                    if (two_per_row == 1)
                        y -= 2 * ShopHeight;
                    else
                        y += 2 * ShopHeight;
                }

                if (two_per_row == 1 && y > UpperY)
                    y -= ShopHeight;
                else if (two_per_row == 2 && y < LowerY)
                    y += ShopHeight;
                else
                {
                    FirstColFinished = true;
                    placed_shops_in_a_row = 0;
                    two_per_row++;
                    if (two_per_row <= 2)
                    {
                        x += StreetWidth;
                        y = UpperY;
                    }
                    else
                    {
                        y -= 2 * ShopHeight; // Because the middle section was placed at the bottom
                        LowestY = y + ShopHeight;
                        x += 160;
                        two_per_row = 1;
                    }
                }

                floor.HubList.Add(Shop);
            }
        }

        public override void SortPlantLists(List<DanishTrolley> dtList)
        {
            foreach(DanishTrolley dt in dtList)
            {
                dt.PlantList = dt.PlantList
                    .OrderBy(obj => obj.DestinationHub.id)
                    .ThenBy(obj => obj.DestinationHub.day)
                    .ToList();
            }
        }

        public override void DistributeTrolleys(List<DanishTrolley> dtList)
        {
            floor.STHubs[0].AddUndistributedTrolleys(dtList);
        }

        public override StartHub GetStartHub(Distributer db)
        {
            return floor.STHubs[0];
        }

        public override BufferHub GetBuffHub(Distributer db)
        {
            return floor.BuffHubs[0];
        }

        public override void PlaceFullTrolleyHubs()
        {
            int FullTrolleyHubWidth = 200;
            for (int i = 0; i < ShopCornersX.Count - 1; i += 2)
            {
                int x = ((ShopCornersX[i] + ShopCornersX[i + 1]) / 2) - (FullTrolleyHubWidth / 2);
                floor.FTHubs.Add(new FullTrolleyHub("Full Trolley Hub", 2 + i / 2, new Point(x, UpperY), floor, new Size(FullTrolleyHubWidth, LowestY - UpperY)));
            }

            base.PlaceFullTrolleyHubs();
        }

        public override void PlaceStartHubs()
        {
            floor.STHubs.Add(new StartHub("Start hub", 0, new Point(200, 4570), floor, vertical_trolleys_: true));
            floor.STHubs.Add(new StartHub("Start hub", 1, new Point(1200, 4570), floor, vertical_trolleys_: true));

            base.PlaceStartHubs();
        }

        public override void PlaceBuffHubs()
        {
            floor.BuffHubs.Add(new BufferHub("Buffer hub", 1, new Point(0, 40), floor));

            base.PlaceBuffHubs();
        }
    }

    internal class SLayoutDayId : SLayout
    {
        public SLayoutDayId(Floor floor_, ReadData rData) : base(floor_, rData) 
        {

        }


        public override string ToString()
        {
            return "S-Layout grouped by Day first and Id second";
        }

        public override void PlaceShops(List<ShopHub> Shops, int UpperY, int LowerY)
        {
            Shops = Shops
                .OrderBy(obj => obj.day)
                .ThenBy(obj => obj.id).ToList();
            base.PlaceShops(Shops, UpperY, LowerY);
        }

        public override void SortPlantLists(List<DanishTrolley> dtList)
        {
            foreach(DanishTrolley dt in dtList)
            {
                dt.PlantList = dt.PlantList
                    .OrderBy(obj => obj.DestinationHub.day)
                    .ThenBy(obj => obj.DestinationHub.id)
                    .ToList();
            }
        }

        /// <summary>
        /// Puts all trolleys with the first day in the first hub,
        /// All the trolleys with only plants for the second day are placed in the second hub
        /// </summary>
        public override void DistributeTrolleys(List<DanishTrolley> dtList)
        {
            List<List<DanishTrolley>> TrolleysPerDay = new List<List<DanishTrolley>> ();
            for (int i = 0; i < RData.days.Count; i++)
                TrolleysPerDay.Add(new List<DanishTrolley>());

            foreach(DanishTrolley d in dtList)
            {
                int i = d.PlantList.Select(p => RData.days.IndexOf(p.DestinationHub.day)).Min();
                TrolleysPerDay[i].Add(d);
            }

            for (int i = 0; i <  TrolleysPerDay.Count; i++)
                floor.STHubs[i].AddUndistributedTrolleys(TrolleysPerDay[i]);
            ;
        }

        public override StartHub GetStartHub(Distributer db)
        {
            foreach (StartHub sth in floor.STHubs)
                if (sth.TotalUndistributedTrolleys() > 0)
                    return sth;
            return floor.STHubs[0];
        }
    }

    internal class SLayoutIdDay: SLayout
    {
        public SLayoutIdDay(Floor floor_, ReadData rData) : base(floor_, rData) 
        {

        }

        public override string ToString()
        {
            return "S-Layout grouped by Id first and Day second";
        }
        public override void PlaceShops(List<ShopHub> Shops, int UpperY, int LowerY)
        {
            Shops = Shops
                .OrderBy(obj => obj.id)
                .ThenBy(obj => obj.day).ToList();
            base.PlaceShops(Shops, UpperY, LowerY);
        }
    }

    internal class RechtHoekLayout : Layout
    {
        int StreetWidth = 2000;
        int ShopHeight = 80;
        private int NtrolleysPerShop = 1;

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

        public override BufferHub GetBuffHub(Distributer db)
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


            int ShopsToPlaceInMiddle = StreetWidth / ShopHeight;
            int ShopsToPlaceOnRight = (FirstDayShops.Count - ShopsToPlaceInMiddle) / 2;
            int ShopsToPlaceOnLeft = FirstDayShops.Count - ShopsToPlaceOnRight - ShopsToPlaceInMiddle;

            int x = 800;
            int y = floor.FirstWW.RSizeWW.Height / 2;
            int overali = 0;
            for (int i = 0; i < 1; i++)
            {
                ShopHub Shop = Shops[overali];
                if (Shop.AmountOfTrolleys() == 0)
                Shop.TeleportHub(new Point(x, y));
                y -= ShopHeight;

                floor.HubList.Add(Shop);
                overali++;
            }
        }

        public override void SortPlantLists(List<DanishTrolley> dtList)
        {
            //throw new NotImplementedException();
            return;
        }

        public override void PlaceStartHubs()
        {
            //throw new NotImplementedException();
            return;
        }

        public override void PlaceBuffHubs()
        {
            //throw new NotImplementedException();
            return;
        }
    }
}
