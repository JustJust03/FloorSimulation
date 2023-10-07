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
        private Image HTrolleyIMG;
        private Image VTrolleyIMG;
        public Point SimPoint; //Where in the panel Should the trolley be drawn.
        public Point RPoint;
        //Vertical
        public Size VRTrolleySize; //Is the Real size in cm.
        public Size VTrolleySize; //Sim trolley size
        //Horizontal
        public Size HRTrolleySize; //Is the Real size in cm.
        public Size HTrolleySize; //Sim trolley size

        public int id;
        private Floor floor;
        public bool IsVertical;

        /// <summary>
        /// Constructer initializing the variables
        /// Use the image to generate the sim and real sizes.
        /// </summary>
        public DanishTrolley(int id_, Floor floor_, Point RPoint_ = default, bool IsVertical_ = false)
        {
            RPoint = RPoint_;

            id = id_;
            floor = floor_;

            VTrolleyIMG = Image.FromFile(Program.rootfolder + @"\SimImages\DanishTrolleyTransparent_vertical.png");
            HTrolleyIMG = Image.FromFile(Program.rootfolder + @"\SimImages\DanishTrolleyTransparent_horizontal.png");

            VRTrolleySize = new Size(VTrolleyIMG.Width, VTrolleyIMG.Height);
            VTrolleySize = floor.ConvertToSimSize(VRTrolleySize);
            HRTrolleySize = new Size(HTrolleyIMG.Width, HTrolleyIMG.Height);
            HTrolleySize = floor.ConvertToSimSize(HRTrolleySize);

            IsVertical = IsVertical_;
        }

        /// <summary>
        /// Draws the trolley object according to it's panellocation and resizes it using ScaleFactor.
        /// </summary>
        /// <param name="g"></param>
        public void DrawObject(Graphics g)
        {
            if (IsVertical)
                g.DrawImage(VTrolleyIMG, new Rectangle(SimPoint, VTrolleySize));
            else
                g.DrawImage(HTrolleyIMG, new Rectangle(SimPoint, HTrolleySize));
        }

        /// <summary>
        /// This function 'teleports' the trolley to a new point.
        /// This should only be used when initializing the trolley, and SimPoint was previously default.
        /// </summary>
        /// <param name="p"></param>
        public void TeleportTrolley(Point Rp)
        {
            RPoint = Rp;
            SimPoint = floor.ConvertToSimPoint(Rp);
        }

        // TODO: Create a function that assigns every new trolley an unique id.
    }
}
