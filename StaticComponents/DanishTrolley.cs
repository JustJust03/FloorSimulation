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
        public Point panellocation; //Where in the panel Should the trolley be drawn.
        public Point reallocation;
        private Image TrolleyIMG;

        public DanishTrolley()
        {
            panellocation = new Point(400, 400);
            TrolleyIMG = Image.FromFile(Program.rootfolder + @"\SimImages\DanishTrolleyTransparent.png");
        }

        /// <summary>
        /// Draws the trolley object according to it's panellocation and resizes it using ScaleFactor.
        /// </summary>
        /// <param name="g"></param>
        public void DrawObject(Graphics g)
        {
            g.DrawImage(TrolleyIMG, panellocation.X, panellocation.Y, 
                        TrolleyIMG.Width * Floor.ScaleFactor,
                        TrolleyIMG.Height * Floor.ScaleFactor);
        }
    }
}
