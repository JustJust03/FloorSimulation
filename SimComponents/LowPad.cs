using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation
{
    internal class LowPad
    {
        private Image HLowPadIMG;
        private Image VLowPadIMG;
        public Point SimPoint; //Where in the panel Should Lange Harry be drawn.
        public Point RPoint;
        //Vertical
        private Size VRLowPadSize; //Is the Real size in cm.
        private Size VLowPadSize; //Sim trolley size
        //Horizontal
        private Size HRLowPadSize; //Is the Real size in cm.
        private Size HLowPadSize; //Sim trolley size

        public int id;
        private Floor floor;
        private WalkWay WW;
        public bool IsVertical;
        public const float LowPadTravelSpeed = 120f; //cm/s
        public int MaxWaitedTicks;
        public Task MainTask;

        public LowPad(int id_, Floor floor_, WalkWay WW_, Point Rpoint_ = default, bool IsVertical_ = true, int MaxWaitedTicks_ = 100)
        {
            id = id_;
            floor = floor_;
            WW = WW_;
            RPoint = Rpoint_;
            IsVertical = IsVertical_;

            VLowPadIMG = Image.FromFile(Program.rootfolder + @"\SimImages\LowPad_vertical.png");
            HLowPadIMG = Image.FromFile(Program.rootfolder + @"\SimImages\LowPad_horizontal.png");

            VRLowPadSize = new Size(VLowPadIMG.Width, VLowPadIMG.Height);
            VLowPadSize = floor.ConvertToSimSize(VRLowPadSize);
            HRLowPadSize = new Size(HLowPadIMG.Width, HLowPadIMG.Height);
            HLowPadSize = floor.ConvertToSimSize(HRLowPadSize);

            MainTask = new LowPadTask(this, floor, "TakeFullTrolley");

            if (RPoint != default)
                TeleportLowPad(RPoint);

            WW.fill_tiles(RPoint, GetRSize());
            MaxWaitedTicks = MaxWaitedTicks_;
        }

        public void Tick()
        {
            MainTask.PerformTask();
        }

        public Size GetRSize()
        {
            if (IsVertical)
                return VLowPadSize;
            else
                return HLowPadSize;
        }

        public void TeleportLowPad(Point Rp)
        {
            RPoint = Rp;
            SimPoint = floor.ConvertToSimPoint(Rp);
        }

        public void DrawObject(Graphics g)
        {
            if (IsVertical)
                g.DrawImage(VLowPadIMG, new Rectangle(SimPoint, VLowPadSize));
            else
                g.DrawImage(VLowPadIMG, new Rectangle(SimPoint, VLowPadSize));
        }
    }
}
