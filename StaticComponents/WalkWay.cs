using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace FloorSimulation
{

    /// <summary>
    /// The place where the distributers will be travelling trough the floor
    /// Is used for path finding and collission detection.
    /// </summary>
    internal class WalkWay
    {
        private Floor floor;
        public Point RPointWW;
        public Point PointWW;
        public Size RSizeWW;
        public Size SizeWW;

        private Brush WWBrush;
        public List<Distributer> DistrList;
        public List<List<WalkTile>> WalkTileList; //call by WalkTileList[x][y]
        public int WalkTileListWidth;
        public int WalkTileListHeight;

        public const int WALK_TILE_WIDTH = 10; //cm
        public const int WALK_TILE_HEIGHT = 10; //cm

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RP"></param>
        /// <param name="RS">Should always be devisable by Walk tile width and height</param>
        /// <param name="floor_"></param>
        public WalkWay(Point RP, Size RS, Floor floor_)  
        {
            floor = floor_;
            RPointWW = RP;
            PointWW = floor.ConvertToSimPoint(RP);
            RSizeWW = RS;
            SizeWW = floor.ConvertToSimSize(RS);

            WalkTileListWidth = RSizeWW.Width / WALK_TILE_WIDTH;
            WalkTileListHeight = RSizeWW.Height / WALK_TILE_HEIGHT;

            WWBrush = new SolidBrush(Color.Gray);
            InitWalkTiles();
            ;
        }

        public void DrawObject(Graphics g)
        {
            g.FillRectangle(WWBrush, new Rectangle(PointWW, SizeWW));
        }

        /// <summary>
        /// Initializes the walktiles and assigns the default values to them.
        /// Should only be run on initiation.
        /// </summary>
        public void InitWalkTiles()
        {
            WalkTileList = new List<List<WalkTile>>();    

            for (int x = 0; x < WalkTileListWidth; x++)
            {
                List<WalkTile> col = new List<WalkTile>(WalkTileListHeight); 

                for (int y = 0; y < WalkTileListHeight; y++)
                {
                    Point Rp = new Point(RPointWW.X + x * WALK_TILE_WIDTH,
                                         RPointWW.Y + y * WALK_TILE_HEIGHT);
                    Point p = floor.ConvertToSimPoint(Rp);

                    col.Add(new WalkTile(x, y, p, Rp, false, this));
                }
                WalkTileList.Add(col);
            }

            UpdateClearances();
        }

        /// <summary>
        /// Updates all the clearances of all the tiles in the walktile list
        /// </summary>
        public void UpdateClearances()
        {
            // TODO: Make this smarter and faster.
            foreach(List<WalkTile> col in WalkTileList)
            {
                foreach(WalkTile tile in col)
                {
                    tile.UpdateClearance();
                }
            }
        }
    }
}
