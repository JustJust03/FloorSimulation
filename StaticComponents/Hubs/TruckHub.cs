﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation.StaticComponents.Hubs
{
    internal class TruckHub: Hub
    {
        public DanishTrolley[,] Trolleyarr;
        private DanishTrolley DummyTrolley;
        private WalkTile[,] HubAccessPoints; //The Points in the hub where you can drop of trolleys
        private int[] HubAccessPointsX; //The Points in the hub where you can drop of trolleys
        private int[] HubAccessPointsY; //The Points in the hub where you can drop of trolleys
        private int NRows;
        private int NTrolleysInRow;
        private int YSlack = 20;

        public int TrolleysExported = 0;

        public TruckHub(string name_, int id_, Point FPoint_, Floor floor_, int initial_trolleys_ = 0) :
            base(name_, id_, FPoint_, floor_, new Size(700, 350), initial_trolleys: initial_trolleys_, vertical_trolleys:true)
        {
            DummyTrolley = new DanishTrolley(-1, floor, IsVertical_: true);
            NRows = RHubSize.Height / (DummyTrolley.GetRSize().Height + YSlack);
            NTrolleysInRow = RHubSize.Width / DummyTrolley.GetRSize().Width;
            Trolleyarr = new DanishTrolley[NRows, NTrolleysInRow];


            HubAccessPoints = new WalkTile[NRows, NTrolleysInRow];
            HubAccessPointsX = new int[NTrolleysInRow];
            HubAccessPointsY = new int[NRows];
            for (int Row = 0; Row < NRows; Row++)
            {
                int trolleyY = RFloorPoint.Y + Row * (DummyTrolley.GetRSize().Height + YSlack);
                for(int coli = 0; coli < NTrolleysInRow; coli++)
                {
                    int trolleyX = RFloorPoint.X + (coli - LangeHarry.MaxTrolleysPerHarry + 2) * DummyTrolley.GetRSize().Width - floor.FirstHarry.GetRSize().Width; //this point + how far in the line it is

                    HubAccessPoints[Row, coli] = WW.GetTile(new Point(trolleyX, trolleyY));
                    HubAccessPointsX[coli] = HubAccessPoints[Row, coli].Rpoint.X;
                }
                HubAccessPointsY[Row] = HubAccessPoints[Row, 0].Rpoint.Y;
            }
        }

        /// <summary>
        /// Returns only the most right open spots in each row
        /// </summary>
        public override List<WalkTile> OpenSpots(Distributer DButer)
        {
            List<WalkTile> OpenSpots = new List<WalkTile>();
            
            for (int rowi = 0; rowi < NRows; rowi++) 
                for(int coli = NTrolleysInRow - 1; coli >= 0; coli--)
                    if (Trolleyarr[rowi, coli] == null)
                    {
                        OpenSpots.Add(HubAccessPoints[rowi, coli]);
                        break;
                    }

            return OpenSpots;
        }

        /// <summary>
        /// Uses the distributer and harrys point to dertermine where this trolley is placed.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="t">The WalkTile the distributer is standing on</param>
        public override void TakeVTrolleyIn(DanishTrolley dt, Point AgentRPoint)
        {
            int ArrIndexX = Array.IndexOf(HubAccessPointsX, AgentRPoint.X);
            int ArrIndexY = Array.IndexOf(HubAccessPointsY, AgentRPoint.Y);
            Trolleyarr[ArrIndexY, ArrIndexX] = dt;
            if (ArrIndexX == 0) //Export trolleys away
            {
                Point p = dt.RPoint;
                Size s = new Size(RHubSize.Width, dt.GetRSize().Height);
                WW.unfill_tiles(p, s);
                for (int i = 0; i < NTrolleysInRow; i++)
                {
                    Trolleyarr[ArrIndexY, i] = null;
                }
                TrolleysExported += NTrolleysInRow;
            }
        }

        public override void DrawHub(Graphics g, bool DrawOutline = false)
        {
            //outline
            if (DrawOutline)
                g.DrawRectangle(floor.BPen, new Rectangle(FloorPoint, HubSize));

            //Trolleys
            foreach (DanishTrolley DT in Trolleyarr)
                if(DT != null)
                    DT.DrawObject(g);
        }

        public override int AmountOfTrolleys()
        {
            int count = 0;
            for (int rowi = 0; rowi < NRows; rowi++) 
                for(int coli = NTrolleysInRow - 1; coli >= 0; coli--)
                    if (Trolleyarr[rowi, coli] != null)
                        count++;
            return count;
        }
    }
}
