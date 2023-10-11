using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Data;

namespace FloorSimulation
{
    /// <summary>
    /// Class that is used to update clearance
    /// </summary>
    internal class WalkWayClearance
    {
        private WalkWay WW;
        private int WidestObject; //In TileSquares
        private int HeighestObject; //In TileSquares

        public WalkWayClearance(WalkWay WW_, int WidestObject_, int HeighestObject_)
        {
            WW = WW_;
            WidestObject = WidestObject_;
            HeighestObject = HeighestObject_;
            InitClearance();
        }
        
        private void InitClearance() 
        {
            for (int xindex = 0; xindex < WW.WalkTileList.Count; xindex++) 
                for (int yindex = 0; yindex < WW.WalkTileList[0].Count; yindex++)
                {
                    WW.WalkTileList[xindex][yindex].ClearanceR = new Size(WW.WalkTileList.Count - xindex, yindex + 1);
                    WW.WalkTileList[xindex][yindex].ClearanceD = new Size(WW.WalkTileList.Count - xindex, yindex + 1);
                }
        }

        /// <summary>
        /// Updates the clearances of every tile.
        /// This is done so the biggest object that could be on the screen is able to fit trough all the occupied tiles.
        /// </summary>
        public void UpdateTileClearance(WalkTile t)
        {
            int leftx = Math.Min(WidestObject, t.TileX);
            int topy = Math.Min(HeighestObject, t.TileY);

            if(t.occupied)  //just got occupied
                NewOccupation(t, leftx, topy);
            else            //just got unoccupied
                NewUnOccupation(t, leftx, topy);
        }

        /// <summary>
        /// From the tile start unclearing to the top left.
        /// When an occupied tile is found, stop clearing above that tile.
        /// </summary>
        private void NewOccupation(WalkTile t, int leftx, int topy)
        {
            int ClearanceLeft = 1;
            int ClearanceTop = 1;

            int MinX = t.TileX - leftx; //Generally determined by the width of the biggest object
            int MinY = t.TileY - topy;  //Generally determined by the height of the biggest object

            // For the tiles left to this tile, update the right clearance from 0.
            for (int FirstRowX = t.TileX; FirstRowX > MinX; FirstRowX--)
            {
                WalkTile targett = WW.WalkTileList[FirstRowX][t.TileY];
                if (targett == t)
                {
                    t.ClearanceR = new Size(0, 0);
                    continue;
                }
                
                targett.ClearanceR = new Size(ClearanceLeft, 1);
                targett.ClearanceD = new Size(ClearanceLeft, );
                ClearanceLeft++;
            }

            // For the tiles above this tile, update the down clearance from 0.
            for (int FirstRowY = t.TileY; FirstRowY > MinY; FirstRowY--)
            {
                WalkTile targett = WW.WalkTileList[t.TileX][FirstRowY];
                if (targett == t)
                    continue;

                targett.ClearanceD = new Size(1, ClearanceTop);
                ClearanceTop++;
            }


            // For the tiles above this tile, udpate the right clearance from what they had.
            ClearanceLeft = 0;
            ClearanceTop = 0;   
            for (int xindex = t.TileX; xindex > MinX; xindex--) 
            { 
                for (int yindex = t.TileY; yindex > MinY; yindex--)
                {
                    WalkTile targett = WW.WalkTileList[xindex][yindex];
                    if (targett == t)
                        continue;
                    if (targett.occupied) //Occupied tile found, stop clearing top left from this
                        MinY = yindex;

                    targett.ClearanceR = new Size(targett.ClearanceR.Width, ClearanceTop);
                    ClearanceTop++;
                }
                ClearanceTop = 0;
                ClearanceLeft++;
            }

        }

        /// <summary>
        /// From the newly unoccupied tile start clearing to the top left.
        /// When an occupied tile is found, stop clearing above that tile.
        /// </summary>
        private void NewUnOccupation(WalkTile t, int leftx, int topy)
        {
            //TODO: Maybe you can stop clearing faster by stopp when clearing exceeds max object size
            int ClearanceLeft = 1;
            int ClearanceTop = 1;

            int MinX = t.TileX - leftx; //Generally determined by the width of the biggest object
            int MaxY = topy;            //Generally determined by the height of the biggest object

            for (int xindex = t.TileX; xindex > MinX; xindex--) 
            { 
                for (int yindex = t.TileY; yindex < MaxY; yindex++)
                {
                    WalkTile targett = WW.WalkTileList[xindex][yindex];
                    if (targett == t) { }
                    else if (targett.occupied) //Occupied tile found, stop clearing top left from this
                        MaxY = yindex;

                    Size OldClearance = targett.ClearanceR;
                    targett.ClearanceR = new Size(OldClearance.Width + ClearanceLeft, OldClearance.Height + ClearanceTop);
                    ClearanceTop++;
                }
                ClearanceLeft++;
            }
        }
    }
}
