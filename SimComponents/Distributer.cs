using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace FloorSimulation
{
    /// <summary>
    /// Main distibuter class.
    /// The agent which walks trough the floor and distributes the plants for the trolley.
    /// </summary>
    internal class Distributer
    {
        private Image DistributerIMG;
        private Point RDPoint; // Real distributer point
        private Point DPoint; // Sim distributer point
        private Size RDistributerSize; //Is the real size in cm.
        private Size DistributerSize; 
        public int id;
        private Floor floor;
        
        public Distributer(int id_, Floor floor_, Point Rpoint_ = default)
        {
            id = id_;
            floor = floor_;
            RDPoint = Rpoint_;

            DistributerIMG = Image.FromFile(Program.rootfolder + @"\SimImages\Distributer.png");
            RDistributerSize = new Size(DistributerIMG.Width, DistributerIMG.Height);

            DistributerSize = floor.ConvertToSimSize(RDistributerSize);
            DPoint = floor.ConvertToSimPoint(RDPoint);
        }

        public void DrawObject(Graphics g)
        {
            g.DrawImage(DistributerIMG, new Rectangle(DPoint, DistributerSize));
        }
    }
}
