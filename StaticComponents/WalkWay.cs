using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace FloorSimulation
{

    /// <summary>
    /// The place where the distrubters will be travelling trough the floor
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

        public WalkWay(Point RP, Size RS, Floor floor_)  
        {
            floor = floor_;
            RPointWW = RP;
            PointWW = floor.ConvertToSimPoint(RP);
            RSizeWW = RS;
            SizeWW = floor.ConvertToSimSize(RS);

            WWBrush = new SolidBrush(Color.Gray);
        }

        public void DrawObject(Graphics g)
        {
            g.FillRectangle(WWBrush, new Rectangle(PointWW, SizeWW));
        }




    }
}
