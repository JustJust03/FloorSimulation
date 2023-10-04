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

        private DijkstraWalkWays DWW;
        public WalkWay WW;

        
        public Distributer(int id_, Floor floor_, WalkWay WW_, Point Rpoint_ = default)
        {
            id = id_;
            floor = floor_;
            RDPoint = Rpoint_;
            WW = WW_;
            DWW = new DijkstraWalkWays(WW);

            DistributerIMG = Image.FromFile(Program.rootfolder + @"\SimImages\Distributer.png");
            RDistributerSize = new Size(DistributerIMG.Width, DistributerIMG.Height);

            DistributerSize = floor.ConvertToSimSize(RDistributerSize);
            DPoint = floor.ConvertToSimPoint(RDPoint);
        }

        public void DrawObject(Graphics g)
        {
            g.DrawImage(DistributerIMG, new Rectangle(DPoint, DistributerSize));
        }

        /// <summary>
        /// Makes the distributer walk towards the target tile using a shortest path algorithm.
        /// </summary>
        /// <param name="target_tile"></param>
        public void TravelTo(WalkTile target_tile)
        {
            DWW.RunAlgo(WW.WalkTileList[0][0], WW.WalkTileList[2][15]);
        }
    }
}
