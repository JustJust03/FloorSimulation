﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation.Layouts
{
    internal class NewLPSlayout : LowPadSlayoutBuffhub
    {
        public int LowerY = 6100;
        int ShopWidth;
        int ShopHeight;

        public NewLPSlayout(Floor floor_, ReadData rData) : base(floor_, rData)
        {
            RealFloorWidth = 5500;
            RealFloorHeight = 7600;
            StreetWidth = 950;
            ForcedShopHeight = 200;
            UseStickersForFull = false;
            CombineTrolleys = true;

            UpperY = 2100;
            NshopsPerDbuter = new int[] { 7, 7, 7, 6, 6, 7, 7, 6, 6, 6, 6, 7, 7, 6, 6, 6, 6, 7, 7, 6, 6 }; //21
        }

        public override void LPPlaceShopsSemiDumb(List<ShopHub> Shops, int UpperY_, int LowerY_)
        {
            UpperY = UpperY_;
            int y = LowerY;
            int x = ShopStartX;
            int two_per_row = 1; //Keeps track of how many cols are placed without space between them
            int placed_shops_in_a_row = 0;
            ShopWidth = Shops[0].RHubSize.Width;
            ShopHeight = Shops[0].RHubSize.Height;
            int[] MaxPerRow = MaxShopsPerRow();
            int RegionsPlaced = 0;
            int rowi = 0;

            for (int i = 0; i < Shops.Count; i++)
            {
                ShopHub Shop = Shops[i];
                if (two_per_row == 2)
                    Shop.HasLeftAccess = true;
                Shop.TeleportHub(new Point(x, y));


                placed_shops_in_a_row++;
                if(placed_shops_in_a_row == NshopsPerDbuter[RegionsPlaced] && two_per_row == 1)
                {
                    y -= ShopHeight * (MaxPerRow[rowi] - NshopsPerDbuter[RegionsPlaced]);

                    RegionsPlaced++;
                    rowi++;
                    placed_shops_in_a_row = 0;
                }
                else if(placed_shops_in_a_row == NshopsPerDbuter[RegionsPlaced] && two_per_row == 2)
                {
                    if(rowi > 0)
                        y += ShopHeight * (MaxPerRow[rowi - 1] - NshopsPerDbuter[RegionsPlaced + 1]);

                    RegionsPlaced++;
                    rowi--;
                    placed_shops_in_a_row = 0;
                }

                //Add corners when you get to a new col, or if this was the last col on the right
                if (rowi == 3 || rowi == -1 || (i == Shops.Count - 1 && two_per_row == 1))
                {
                    if (Shop.HasLeftAccess)
                        ShopCornersX.Add(Shop.RFloorPoint.X);
                    else
                        ShopCornersX.Add(Shop.RFloorPoint.X + Shop.RHubSize.Width);
                    if (i == Shops.Count - 1)
                        ShopCornersX.Add(Shop.RFloorPoint.X + StreetWidth);
                }

                if (rowi == -1 || rowi == 3 && i < Shops.Count - 1) //Ended this column. now skip to the right.
                {
                    if (rowi == -1)
                        rowi = 0;
                    else if (rowi == 3)
                        rowi = 2;

                    two_per_row++;
                    if (two_per_row <= 2)
                    {
                        UpperY = y + ShopHeight;
                        x += StreetWidth;
                        y += ShopHeight * (MaxPerRow[rowi] - NshopsPerDbuter[RegionsPlaced]);
                    }
                    else
                    {
                        LowestY = y + ShopHeight;
                        x += 160;
                        two_per_row = 1;
                    }
                }
                else if (two_per_row == 1)
                    y -= ShopHeight;
                else if (two_per_row == 2)
                    y += ShopHeight;

                floor.HubList.Add(Shop);
            }
        }

        public override void LPPlaceShops(List<ShopHub> Shops, int UpperY_, int LowerY_)
        {
            int y = LowerY;
            int x = ShopStartX;
            int two_per_row = 1; //Keeps track of how many cols are placed without space between them
            int placed_shops_in_a_row = 0;
            ShopWidth = Shops[0].RHubSize.Width;
            ShopHeight = Shops[0].RHubSize.Height;
            int Regioni = 3;

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
                if (FirstColFinished && placed_shops_in_a_row == NshopsPerDbuter[Regioni])
                {
                    Regioni++;
                    placed_shops_in_a_row = 0;
                    if (two_per_row == 1)
                        y -= ShopHeight;
                    else
                        y += ShopHeight;
                }

                if (two_per_row == 1 && y > UpperY)
                    y -= ShopHeight;
                else if (two_per_row == 2 && y <= LowerY)
                    y += ShopHeight;
                else
                {
                    /*
                    if (FirstColFinished && i < Shops.Count - 1 && y > LowerY)
                    {
                        floor.HubList.Add(Shop);
                        i++;
                        Shop = Shops[i];
                        if (two_per_row == 2)
                            Shop.HasLeftAccess = true;
                        Shop.TeleportHub(new Point(x, y));
                    }
                    */

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
                        LowestY = y;
                        if (two_per_row == 3)
                            Shop.HasLeftAccess = true;
                        x += ShopWidth;
                        two_per_row = 1;
                        y -= ShopHeight;
                    }
                }

                floor.HubList.Add(Shop);
            }
        }
    }
}
