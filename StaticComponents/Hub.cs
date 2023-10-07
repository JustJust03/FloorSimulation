﻿using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    /// <summary>
    /// Standard hub for all places (ShopHub, StartHub, EndHub)
    /// </summary>
    internal class Hub
    {
        protected List<DanishTrolley> HubTrolleys;
        protected string name;
        protected int id;
        protected Point RFloorPoint;    //Real point on the floor. In cm.
        protected Point FloorPoint;
        protected Size RHubSize;        //Real size of the hub. In cm.
        protected Size HubSize;
        protected Floor floor;
        protected int max_trolleys;     //How many trolleys can be placed in this hub
        protected Point RAccessPoint;    //Point from where a distributer can access the hub.
        protected Point AccessPoint;

        public Hub(string name_, int id_, Point FPoint_, Floor floor_, Size RHubSize_, int initial_trolleys = 0, 
                   bool vertical_trolleys = false, Point AccPoint_ = default)
        {
            name = name_;
            id = id_;
            RFloorPoint = FPoint_;
            floor = floor_;
            RHubSize = RHubSize_;   


            HubTrolleys = new List<DanishTrolley>();
            if (vertical_trolleys)
            {
                max_trolleys = RHubSize.Width / 80;
                InitVTrolleys(initial_trolleys);
            }
            else
            {
                max_trolleys = RHubSize.Height / 80;
                InitHTrolleys(initial_trolleys);
            }

            FloorPoint = floor.ConvertToSimPoint(RFloorPoint); //Scaled up the Real Hub Floor Point to the SimPoint
            HubSize = floor.ConvertToSimSize(RHubSize); //Scaled up the Real Hub Size to the SimSize

            //Adds an access point to this hub
            if (AccPoint_ == default) //No accesspoint, default to topleft.
            {
                RAccessPoint = RFloorPoint;
                AccessPoint = FloorPoint;
            }
            else
            {
                RAccessPoint = AccPoint_;
                AccessPoint = floor.ConvertToSimPoint(RAccessPoint);
            }
        }

        /// <summary>
        /// Assigns the trolleys in the hub from top to bottom.
        /// 10cm space in every dimension.
        /// </summary>
        /// <param name="initial_trolleys"></param>
        private void InitHTrolleys(int initial_trolleys)
        {
            if (initial_trolleys > max_trolleys)
                throw new ArgumentException("Can't add more trolleys to this shop hub.");

            int UpperY = RFloorPoint.Y; //Start from the top of the hub, and keep track of where to place the trolley.
            int Rslack = 20; //The real slack in all dimensions.
            for (int i = 0; i < initial_trolleys; i++)
            {
                DanishTrolley DT = new DanishTrolley(i, floor, IsVertical_: false);

                int trolleyY = UpperY + Rslack; 
                UpperY += DT.HRTrolleySize.Height + Rslack;
                int trolleyX = RFloorPoint.X + Rslack; 

                DT.TeleportTrolley(new Point(trolleyX, trolleyY));
                HubTrolleys.Add(DT);
            }
        }

        private void InitVTrolleys(int initial_trolleys)
        {
            if (initial_trolleys > max_trolleys)
                throw new ArgumentException("Can't add more trolleys to this shop hub.");

            int LeftX = RFloorPoint.X; //Start from the left of the hub, and keep track of where to place the trolley.
            int Rslack = 20; //The real slack in all dimensions.
            for (int i = 0; i < initial_trolleys; i++)
            {
                DanishTrolley DT = new DanishTrolley(i, floor, IsVertical_: true);

                int trolleyX = LeftX + Rslack; 
                LeftX += DT.VRTrolleySize.Width + Rslack;
                int trolleyY = RFloorPoint.Y + Rslack; 

                DT.TeleportTrolley(new Point(trolleyX, trolleyY));
                HubTrolleys.Add(DT);
            }

        }

        public override string ToString()
        {
            return "This is a Hub: \n\tName: " + this.name + " \n\tID: " + this.id;
        }

        /// <summary>
        /// Draw the components (trolleys) to the screen.
        /// optionally draw the outline of the hub for better visualization.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="DrawOutline"></param>
        public void DrawHub(Graphics g, bool DrawOutline = false)
        {
            //outline
            if (DrawOutline)
            {
                //access point shifted to make the middle of the circle the exact accesspoint.
                Point shiftedPoint = new Point(AccessPoint.X - 5, AccessPoint.Y - 5);
                g.FillEllipse(new SolidBrush(Color.Red), new Rectangle(shiftedPoint, new Size(10, 10)));
                g.DrawRectangle(floor.BPen, new Rectangle(FloorPoint, HubSize));
            }

            //Trolleys
            foreach (DanishTrolley DT in HubTrolleys)
            {
                DT.DrawObject(g);
            }
        }
    }
}
