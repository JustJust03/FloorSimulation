using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation
{
    internal class KortereVerdeelstraatSlayout : SLayout
    {
        int[] Shoplength = { 7, 8, 8, 7, 7, 8, 8, 7, 7, 8, 8, 7, 7, 8, 7, 7, 7, 7};
        int LeftX = 600;
        int LowerY = 6300;
        int TussenWidth = 600;
        List<int> ShopCornersY = new List<int>();
        int ShopWidth;
        int ShopHeight;
        int BuffhubWidth = 600;

        public KortereVerdeelstraatSlayout(Floor floor_, ReadData RD_) : base(floor_, RD_) 
        {
            HorizontalShops = false;
            RealFloorHeight = 7600;
            RealFloorWidth = 5500;
            StreetWidth = 950;
            ForcedShopWidth = 200;
        }  

        public void InitShopCorners()
        {
            int y = LowerY + ShopHeight;
            for (int i = 0; i < Shoplength.Length / 2; i++) 
            { 
                if(i % 2 == 0)
                {
                    ShopCornersY.Add(y);
                    y -= ShopHeight * 2;
                }
                else
                {
                    ShopCornersY.Add(y);
                    y -= StreetWidth;
                }
            }

            ShopCornersX.Add(LeftX);
            ShopCornersX.Add(LeftX + ShopWidth * (Shoplength[0] + Shoplength[1]) + TussenWidth);
        }

        public override string ToString()
        {
            return "S-Layout Shorter streets (9).";
        }

        public override void PlaceShops(List<ShopHub> Shops, int _, int __)
        {
            int RowsPlaced = 0;
            int PlacedShopsInARow = 0;
            bool LowerAccess = true;
            int x = LeftX;
            int y = LowerY;
            ShopWidth = Shops[0].RHubSize.Width;
            ShopHeight = Shops[0].RHubSize.Height;

            for(int i =  0; i < Shops.Count; i++)
            {
                ShopHub shop = Shops[i];
                shop.HasLeftAccess = LowerAccess;
                shop.TeleportHub(new Point(x, y));
                if (LowerAccess)
                    x += ShopWidth;
                else
                    x -= ShopWidth;

                PlacedShopsInARow++;
                if (Shoplength[RowsPlaced] == PlacedShopsInARow)
                {
                    if (RowsPlaced % 4 == 0)// left to right, halfway
                    {
                        //if (Shoplength[RowsPlaced] == )

                        x += TussenWidth;
                    }
                    else if (RowsPlaced % 4 == 2)// right to left, halfway
                    {
                        if (Shoplength[RowsPlaced] == 7)
                            x -= ShopWidth; //this row was 1 shorter then normal.

                        x -= TussenWidth;
                    }
                    else if (RowsPlaced % 4 == 1) // rightpoint, move shopheight up.
                    {
                        x -= ShopWidth;
                        y -= ShopHeight;
                        LowerAccess = false;
                    }
                    else if (RowsPlaced % 4 == 3)// Leftpoint, move streetwidth up.
                    {
                        x += ShopWidth;
                        y -= StreetWidth + ShopHeight;
                        LowerAccess = true;
                    }

                    PlacedShopsInARow = 0;
                    RowsPlaced++;
                }

                floor.HubList.Add(shop);
            }

            InitShopCorners();
        }


        public override void PlaceFullTrolleyHubs()
        {
            int FullTrolleyHubHeight = 200;

            Point FirstPoint = new Point(LeftX + BuffhubWidth + 300, ShopCornersY[0] + 300);
            Point SecondPoint = new Point(ShopCornersX[1] - Shoplength[1] * ShopWidth, ShopCornersY[0] + 300);
            floor.FTHubs.Add(new FullTrolleyHub("Full Trolley Hub", 0, FirstPoint, floor, new Size(ShopCornersX[1] - Shoplength[1] * ShopWidth - 200 - FirstPoint.X, FullTrolleyHubHeight), vertical_trolleys_: true));
            floor.FTHubs.Add(new FullTrolleyHub("Full Trolley Hub", 0, SecondPoint, floor, new Size(Shoplength[1] * ShopWidth - 300 - BuffhubWidth, FullTrolleyHubHeight), vertical_trolleys_: true));

            for (int i = 1; i < ShopCornersY.Count - 1; i += 2)
            {
                int y = ((ShopCornersY[i] + ShopCornersY[i + 1]) / 2) - (FullTrolleyHubHeight / 2);

                FirstPoint = new Point(LeftX + BuffhubWidth + 300, y);
                SecondPoint = new Point(ShopCornersX[1] - Shoplength[1] * ShopWidth, y);

                floor.FTHubs.Add(new FullTrolleyHub("Full Trolley Hub", 0, FirstPoint, floor, new Size(Shoplength[0] * ShopWidth - BuffhubWidth - 300, FullTrolleyHubHeight), vertical_trolleys_: true));
                floor.FTHubs.Add(new FullTrolleyHub("Full Trolley Hub", 0, SecondPoint, floor, new Size(Shoplength[1] * ShopWidth - 300 - BuffhubWidth, FullTrolleyHubHeight), vertical_trolleys_: true));
            }

            base.PlaceFullTrolleyHubs();
        }

        public override void PlaceBuffHubs()
        {
            int BuffHubHeight = 200;

            floor.BuffHubs.Add(new BufferHub("Small buffer hub", 0, new Point(ShopCornersX[0], ShopCornersY[0] + 300),new Size(BuffhubWidth, BuffHubHeight), floor, vertical_trolleys_: true));
            floor.BuffHubs.Add(new BufferHub("Small buffer hub", 0, new Point(ShopCornersX[1] - BuffhubWidth, ShopCornersY[0] + 300),new Size(BuffhubWidth, BuffHubHeight), floor, vertical_trolleys_: true));

            for (int i = 1; i < ShopCornersY.Count - 1; i += 2)
            {
                int y = ((ShopCornersY[i] + ShopCornersY[i + 1]) / 2) - (BuffHubHeight / 2);

                floor.BuffHubs.Add(new BufferHub("Small buffer hub", 1 + i, new Point(ShopCornersX[0], y),new Size(BuffhubWidth, BuffHubHeight), floor, vertical_trolleys_: true));
                floor.BuffHubs.Add(new BufferHub("Small buffer hub", 1 + i, new Point(ShopCornersX[1] - BuffhubWidth, y),new Size(BuffhubWidth, BuffHubHeight), floor, vertical_trolleys_: true));
            }

            floor.BuffHubs.Add(new BufferHub("Buffer hub", 1, new Point(300, 40), new Size(floor.FirstWW.RSizeWW.Width - 500, 600), floor));
            floor.HubList = floor.HubList.Concat(floor.BuffHubs).ToList();
        }

        public override void PlaceStartHubs()
        {
            int CornersYDone = 0;
            int y = ShopCornersY[0] + 100;
            int x = LeftX - 200;

            for (int i = 0; i < Shoplength.Length; i++)
            {
                floor.STHubs.Add(new StartHub("Start hub", i, new Point(x, y), new Size(200, 470), floor, vertical_trolleys_: false));
                if(i % 4 == 0 && i < Shoplength.Length - 1) //Left of the first col
                    x += Shoplength[0] * ShopWidth + TussenWidth;
                else if(i % 4 == 1 && i < Shoplength.Length - 1) //Left of the second col (In the middle path)
                {
                    x += Shoplength[1] * ShopWidth + 200;
                    y = ShopCornersY[++CornersYDone] - 570;
                }
                else if(i % 4 == 2 && i < Shoplength.Length - 1) //Right of the second col
                    x -= Shoplength[1] * ShopWidth + TussenWidth;
                else if(i % 4 == 3 && i < Shoplength.Length - 1) //Right of the first col (in the middle path)
                {
                    x = LeftX - 200;
                    y = ShopCornersY[++CornersYDone] + 100;
                }
            }

            floor.HubList = floor.HubList.Concat(floor.STHubs).ToList();
        }
    }
}
