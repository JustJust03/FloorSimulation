using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Linq;

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
        private Size VRTrolleySize; //Is the Real size in cm.
        private Size VTrolleySize; //Sim trolley size
        //Horizontal
        private Size HRTrolleySize; //Is the Real size in cm.
        private Size HTrolleySize; //Sim trolley size

        public int id;
        public string TransactieId;
        private Floor floor;
        public bool IsVertical;
        public bool AccessOnTopLeft;
        public bool IsInTransport;
        public int Units; //Every unit of a plant, 1 plant can contain 12 units (it was on a tray)
        public readonly int MaxUnits = 100; //100 for now, find a better value
        public int NStickers = 2;
        public readonly int MaxStickers = 20;
        public int TotalStickers = 2;
        public const int MaxTotalStickers = 22 ; //22

        public const float TrolleyTravelSpeed = 50f; //cm/s

        public List<plant> PlantList;
        public List<LowPadAccessHub> TargetRegions;

        /// <summary>
        /// Constructer initializing the variables
        /// Use the image to generate the sim and real sizes.
        /// </summary>
        public DanishTrolley(int id_, Floor floor_, Point RPoint_ = default, bool IsVertical_ = false, string transactieId_ = null)
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
            IsInTransport = false;
            TransactieId = transactieId_;
            PlantList = new List<plant>();

            if (RPoint != default)
                TeleportTrolley(RPoint);
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
        /// Return the real size of trolley. 
        /// According to horizontal or vertical orientation.
        /// </summary>
        public Size GetRSize()
        {
            if (IsVertical)
                return VRTrolleySize;
            else
                return HRTrolleySize;
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

        public bool TakePlantIn(plant p)
        {
            NStickers++;
            TotalStickers++;
            Units += p.units;
            PlantList.Add(p);
            return IsFull();
        }

        public void RotateTrolley()
        {
            IsVertical = !IsVertical;
        }

        public plant PeekFirstPlant()
        {
            if (PlantList.Count > 0) 
                return PlantList[0];
            else 
                return null;
        }

        public plant GiveFirstPlant()
        {
            plant p = PlantList[0]; 
            PlantList.RemoveAt(0);
            return p;
        }

        public plant GiveFirstPlantInRegion(LowPadAccessHub Regionhub)
        {
            plant p2 = PlantList.FirstOrDefault(p => Regionhub.shops.Contains(p.DestinationHub));
            PlantList.Remove(p2);
            if (!PlantList.Any(p => Regionhub.shops.Contains(p.DestinationHub)))
                TargetRegions.Remove(Regionhub);
            return p2;
        }

        public bool IsFull()
        {
            if (TotalStickers >= MaxTotalStickers)
                return true;
            return false;
        }

        public static string FullDetection()
        {
            return "Vol bij aantal Stickers";
        }

        public override string ToString()
        {
            return TransactieId + " plants: " + PlantList.Count;
        }

        //Not used anymore
        public void SwitchPlants(int FirsIndex, int SecondeIndex)
        {
            plant p = PlantList[FirsIndex];
            PlantList[FirsIndex] = PlantList[SecondeIndex];
            PlantList[SecondeIndex] = p;
        }

        public bool FinishedRegion(LowPadAccessHub RegionHub)
        {
            if (TargetRegions.Contains(RegionHub))
                return false;
            return true;
        }
        // TODO: Create a function that assigns every new trolley an unique id.
    }
}
