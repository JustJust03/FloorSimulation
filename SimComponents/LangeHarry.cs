using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation
{
    internal class LangeHarry
    {
        private Image HHarryIMG;
        private Image VHarryIMG;
        public Point SimPoint; //Where in the panel Should Lange Harry be drawn.
        public Point RPoint;
        //Vertical
        private Size VRHarrySize; //Is the Real size in cm.
        private Size VHarrySize; //Sim trolley size
        //Horizontal
        private Size HRHarrySize; //Is the Real size in cm.
        private Size HHarrySize; //Sim trolley size

        public int id;
        private Floor floor;
        private WalkWay WW;
        public bool IsVertical;
        public bool IsInUse;
        public Distributer DButer;

        public List<DanishTrolley> TrolleyList;

        /// <summary>
        /// Constructer initializing the variables
        /// Use the image to generate the sim and real sizes.
        /// Places the distributer at the top left, but draws it at the divers seat.
        /// </summary>
        public LangeHarry(int id_, Floor floor_, WalkWay WW_, Point RPoint_ = default, bool IsVertical_ = true)
        {
            RPoint = RPoint_;
            WW = WW_;
            id = id_;
            floor = floor_;

            VHarryIMG = Image.FromFile(Program.rootfolder + @"\SimImages\LangeHarry_vertical.png");
            HHarryIMG = Image.FromFile(Program.rootfolder + @"\SimImages\LangeHarry_horizontal.png");

            VRHarrySize = new Size(VHarryIMG.Width, VHarryIMG.Height);
            VHarrySize = floor.ConvertToSimSize(VRHarrySize);
            HRHarrySize = new Size(HHarryIMG.Width, HHarryIMG.Height);
            HHarrySize = floor.ConvertToSimSize(HRHarrySize);

            IsVertical = IsVertical_;
            IsInUse = false;
            TrolleyList = new List<DanishTrolley>();

            if(RPoint != default)
                TeleportHarry(RPoint);

            WW.fill_tiles(RPoint, GetRSize());
        }

        /// <summary>
        /// Draws the LangeHarry object according to it's panellocation and resizes it using ScaleFactor.
        /// Also calls the trolleys which are on LangeHarry to be drawn.
        /// And calls the Distributer to draw on LangeHarry's seat.
        /// </summary>
        public void DrawObject(Graphics g)
        {
            if (IsVertical)
            {
                g.DrawImage(VHarryIMG, new Rectangle(SimPoint, VHarrySize));
                if(DButer != null)
                {
                    Point Rp = new Point(RPoint.X + VRHarrySize.Width / 2 - DButer.VRDistributerSize.Width / 2,
                                         RPoint.Y + 171);
                    DButer.DrawObject(g, floor.ConvertToSimPoint(Rp));
                }
            }
            else
            {
                g.DrawImage(HHarryIMG, new Rectangle(SimPoint, HHarrySize));
                if(DButer != null)
                {
                    Point Rp = new Point(RPoint.X,
                                         RPoint.Y + HRHarrySize.Height / 2 - DButer.HRDistributerSize.Height / 2);
                    DButer.DrawObject(g, floor.ConvertToSimPoint(Rp));
                }
            }

            foreach(DanishTrolley dt in TrolleyList)
                dt.DrawObject(g);
        }
        
        /// <summary>
        /// Return the real size of trolley. 
        /// According to horizontal or vertical orientation.
        /// </summary>
        public Size GetRSize()
        {
            if (IsVertical)
                return VRHarrySize;
            else
                return HRHarrySize;
        }

        /// <summary>
        /// This function 'teleports' LangeHarry to a new point.
        /// This should only be used when initializing LangeHarry, and SimPoint was previously default.
        /// </summary>
        public void TeleportHarry(Point Rp)
        {
            RPoint = Rp;
            SimPoint = floor.ConvertToSimPoint(Rp);
        }

        public bool TakeTrolleyIn(DanishTrolley dt)
        {
            TrolleyList.Add(dt);
            if (IsVertical)
            {
                dt.RPoint = new Point(RPoint.X, RPoint.Y + (3 - TrolleyList.Count()) * 57);
                dt.SimPoint = floor.ConvertToSimPoint(dt.RPoint);
            }
            else
                throw new Exception("Harry can't take in tolleys in horizontal mode yet");
            return TrolleyList.Count() >= 3;
        }

        public List<DanishTrolley> DropTrolleys()
        {
            List<DanishTrolley> dtlist = TrolleyList;
            TrolleyList.Clear();
            return dtlist;
        }
        
        public DanishTrolley DropTrolley()
        {
            DanishTrolley t = TrolleyList[TrolleyList.Count - 1];

            if (t.IsVertical)
                t.RPoint.X += 57 * (4 - TrolleyList.Count);
            else
                t.RPoint.Y -= 57 * (4 - TrolleyList.Count);
            t.SimPoint = floor.ConvertToSimPoint(t.RPoint);
            WW.fill_tiles(t.RPoint, t.GetRSize());

            TrolleyList.RemoveAt(TrolleyList.Count - 1);
            return t;
        }
        
        public void TravelHarry()
        {
            WW.unfill_tiles(RPoint, GetRSize());
            RPoint = DButer.RDPoint;
            SimPoint = DButer.DPoint;
            WW.fill_tiles(RPoint, GetRSize(), DButer);

            for (int i = 0; i < TrolleyList.Count; i++)
            {
                if (IsVertical)
                {
                    DanishTrolley t = TrolleyList[i];
                    t.RPoint = new Point(RPoint.X, RPoint.Y + 57 * (2 - i));
                    t.SimPoint = floor.ConvertToSimPoint(t.RPoint);
                }
                else
                {
                    DanishTrolley t = TrolleyList[i];
                    t.RPoint = new Point(RPoint.X + 53 + 57 * i, RPoint.Y);
                    t.SimPoint = floor.ConvertToSimPoint(t.RPoint);
                }
            }
        }

        public void RotateTrolleys()
        {
            for (int i = 0; i < TrolleyList.Count; i++)
            {
                DanishTrolley t = TrolleyList[i];
                t.IsVertical = !t.IsVertical;
                if (t.IsVertical)
                    t.RPoint = new Point(RPoint.X + GetRSize().Width - 57 * (3 - i), RPoint.Y);
                else
                    throw new Exception("YOU SHOULDN'T ROTATE THIS TO HORIZONTAL WHEN THERE ARE TROLLEYS ON HARRY");
            }
        }
    }
}
