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
        // Real size: 4000 cm x 4000 cm
        public const int RealFloorWidth = 4000; //cm
        public const int RealFloorHeight = 4000; //cm
        public const float ScaleFactor = 0.2f;

        private List<DanishTrolley> TrolleyList; // A list with all the trolleys that are on the floor.

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

            TrolleyList = new List<DanishTrolley>
            {
                new DanishTrolley()
            };

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
        }
    }
}
