using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace FloorSimulation
{
    /// <summary>
    /// The Usable floor in AllGreen.
    /// This is a panel which hosts all objects that are on the floor in allgreen (Distributers, trolleys...).
    /// This does not necessarily indicate the size of the working streets.
    /// </summary>
    internal class Floor : SmoothPanel
    {
        MainDisplay Display;
        readonly Color FloorColor = Color.FromArgb(255, 180, 180, 180); //Concrete gray
        public readonly Pen BPen = new Pen(Color.Black);

        // Real size: 4000 cm x 4000 cm
        public const int RealFloorWidth = 2000; //cm
        public const int RealFloorHeight = 2000; //cm
        public const float ScaleFactor = 0.4f; //(RealFloorHeight / Height of window) - (800 / 2000 = 0.4)


        private List<DanishTrolley> TrolleyList; // A list with all the trolleys that are on the floor.
        private ShopHub FirstShop;

        /// <summary>
        /// Sets the pixel floor size by using the ScaleFactor.
        /// </summary>
        /// <param name="PanelLocation">Where should the panel be drawn from (topleft)</param>
        /// <param name="di">On which display is this being drawn</param>
        public Floor(Point PanelLocation, MainDisplay di)
        {
            Display = di;

            Size PixelFloorSize = new Size((int)(RealFloorWidth * ScaleFactor),
                                           (int)(RealFloorHeight * ScaleFactor));
            this.Location = PanelLocation;
            this.Size = PixelFloorSize;
            this.BackColor = FloorColor;

            TrolleyList = new List<DanishTrolley>();

            FirstShop = new ShopHub("IKEA", 0, new Point(0, 0), this, 2);

            this.Paint += PaintTrolleys;
            this.Invalidate();
        }

        /// <summary>
        /// Paints all trolleys that are in the trolleylist, thus all trolleys that are on the floor.
        /// </summary>
        public void PaintTrolleys(object obj, PaintEventArgs pea)
        {
            Graphics g = pea.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach (DanishTrolley t in TrolleyList)
            {
                t.DrawObject(g);
            }

            FirstShop.DrawHub(g, true);
        }

        public Point ConvertToSimPoint(Point RPoint)
        {
            return new Point((int)(RPoint.X * ScaleFactor), (int)(RPoint.Y * ScaleFactor));
        }
        public Size ConvertToSimSize(Size RSize)
        {
            return new Size((int)(RSize.Width * ScaleFactor), (int)(RSize.Height * ScaleFactor));
        }
    }
}
