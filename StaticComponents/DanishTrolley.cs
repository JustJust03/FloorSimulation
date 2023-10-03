using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace FloorSimulation
{
    /// <summary>
    /// Standard class to indicate Trolleys.
    /// Can be drawn.
    /// </summary>
    internal class DanishTrolley
    {
        public Point SimPoint; //Where in the panel Should the trolley be drawn.
        public Point RPoint;
        private Image TrolleyIMG;
        public int id;
        public Size TrolleySize; //Is the Real size in cm.

        public DanishTrolley(int id_, Point RPoint_ = default)
        {
            RPoint = RPoint_;

            id = id_;

            TrolleyIMG = Image.FromFile(Program.rootfolder + @"\SimImages\DanishTrolleyTransparent.png");
            TrolleySize = new Size((int)(TrolleyIMG.Width * Floor.ScaleFactor), 
                                   (int)(TrolleyIMG.Height * Floor.ScaleFactor));
        }

        /// <summary>
        /// Draws the trolley object according to it's panellocation and resizes it using ScaleFactor.
        /// </summary>
        /// <param name="g"></param>
        public void DrawObject(Graphics g)
        {
            g.DrawImage(TrolleyIMG, new Rectangle(SimPoint, TrolleySize));
        }

        /// <summary>
        /// This function 'teleports' the trolley to a new point.
        /// This should only be used when initializing the trolley, and SimPoint was previously default.
        /// </summary>
        /// <param name="p"></param>
        public void TeleportTrolley(Point p)
        {
            SimPoint = p;
        }

        // TODO: Create a function that assigns every new trolley an unique id.
    }
}
