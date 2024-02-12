using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation
{
    internal class NewSlayout : SLayoutDayIdBuffhub
    {
        int LowerY = 6100;
        int ShopWidth;
        int ShopHeight;

        public NewSlayout(Floor floor_, ReadData rData_) : base(floor_, rData_)
        {
            RealFloorWidth = 5500;
            RealFloorHeight = 7600;
            StreetWidth = 950;
            ForcedShopHeight = 200;
            UseStickersForFull = false;
            CombineTrolleys = true;

            UpperY = 2100;
        }

        public override void PlaceShops(List<ShopHub> Shops, int UpperY_, int LowerY_)
        {
            int y = LowerY;
            int x = ShopStartX;
            int two_per_row = 1; //Keeps track of how many cols are placed without space between them
            int placed_shops_in_a_row = 0;
            ShopWidth = Shops[0].RHubSize.Width;
            ShopHeight = Shops[0].RHubSize.Height;

            bool FirstColFinished = false;
            for (int i = 0; i < Shops.Count; i++)
            {
                ShopHub Shop = Shops[i];
                if (two_per_row == 2)
                    Shop.HasLeftAccess = true;
                Shop.TeleportHub(new Point(x, y));

                //Add corners when you get to a new col, or if this was the last col on the right
                if (y == UpperY || (i == Shops.Count - 1 && two_per_row == 1))
                {
                    if (Shop.HasLeftAccess)
                        ShopCornersX.Add(Shop.RFloorPoint.X);
                    else
                        ShopCornersX.Add(Shop.RFloorPoint.X + Shop.RHubSize.Width);
                    if (i == Shops.Count - 1)
                        ShopCornersX.Add(Shop.RFloorPoint.X + StreetWidth);
                }

                placed_shops_in_a_row++;
                if (FirstColFinished && (two_per_row == 1 && placed_shops_in_a_row + 1 == HalfShopsInRow) || (two_per_row == 2 && placed_shops_in_a_row == HalfShopsInRow))
                {
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
                    if (!FirstColFinished)
                        HalfShopsInRow = placed_shops_in_a_row / 2;
                    FirstColFinished = true;
                    placed_shops_in_a_row = 0;
                    two_per_row++;
                    if (two_per_row <= 2)
                    {
                        x += StreetWidth + ShopWidth;
                        y = UpperY;
                    }
                    else
                    {
                        LowestY = y + ShopHeight;
                        x += ShopWidth;
                        two_per_row = 1;
                    }
                }

                floor.HubList.Add(Shop);
            }
        }
    }

    internal class NewSlayout2Streets: SLayoutDayIdBuffhub2Streets
    {
        int LowerY = 6500;
        int ShopWidth;
        int ShopHeight;

        public NewSlayout2Streets(Floor floor_, ReadData rData): base(floor_, rData)
        {
            RealFloorWidth = 5500;
            RealFloorHeight = 7600;
            StreetWidth = 950;
            ForcedShopHeight = 200;
            UseStickersForFull = false;
            CombineTrolleys = true;

            UpperY = 1300;
        }

        public override void PlaceShops(List<ShopHub> Shops, int UpperY_, int LowerY_)
        {
            List<ShopHub> Shops2 = new List<ShopHub>();
            foreach(ShopHub s in Shops)
                Shops2.Add(new ShopHub(s.name + "_Second-street", s.id, s.RFloorPoint, floor, s.RHubSize,
                                       initial_trolleys: 2, ColliPlusDay_: s.ColliPlusDay + "_2"));
            Shops = Shops.Concat(Shops2).ToList();

            int y = LowerY;
            int x = ShopStartX;
            int two_per_row = 1; //Keeps track of how many cols are placed without space between them
            int placed_shops_in_a_row = 0;
            ShopWidth = Shops[0].RHubSize.Width;
            ShopHeight = Shops[0].RHubSize.Height;

            bool FirstColFinished = false;
            for (int i = 0; i < Shops.Count; i++)
            {
                ShopHub Shop = Shops[i];
                if (two_per_row == 2)
                    Shop.HasLeftAccess = true;
                Shop.TeleportHub(new Point(x, y));

                //Add corners when you get to a new col, or if this was the last col on the right
                if (y == UpperY || (i == Shops.Count - 1 && two_per_row == 1))
                {
                    if (Shop.HasLeftAccess)
                        ShopCornersX.Add(Shop.RFloorPoint.X);
                    else
                        ShopCornersX.Add(Shop.RFloorPoint.X + Shop.RHubSize.Width);
                    if (i == Shops.Count - 1)
                        ShopCornersX.Add(Shop.RFloorPoint.X + StreetWidth);
                }

                placed_shops_in_a_row++;
                if (FirstColFinished && (two_per_row == 1 && placed_shops_in_a_row + 1 == HalfShopsInRow) || (two_per_row == 2 && placed_shops_in_a_row == HalfShopsInRow))
                {
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
                    if (!FirstColFinished)
                        HalfShopsInRow = placed_shops_in_a_row / 2;
                    FirstColFinished = true;
                    placed_shops_in_a_row = 0;
                    two_per_row++;
                    if (two_per_row <= 2)
                    {
                        x += StreetWidth + ShopWidth;
                        y = UpperY;
                    }
                    else
                    {
                        LowestY = y + ShopHeight;
                        x += ShopWidth;
                        two_per_row = 1;
                    }
                }

                floor.HubList.Add(Shop);
            }
        }

        public override void PlaceStartHubs()
        {
            floor.STHubs.Add(new StartHub("Start hub", 1, new Point(2080, UpperY - 400), new Size(640, 200), floor, vertical_trolleys_: true));
            floor.STHubs.Add(new StartHub("Start hub", 0, new Point(500, floor.FirstWW.RSizeWW.Height - 250), new Size(640, 200), floor, vertical_trolleys_: true));
            floor.HubList = floor.HubList.Concat(floor.STHubs).ToList();
        }
    }
}
