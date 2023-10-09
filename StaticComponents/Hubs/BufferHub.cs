using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    /// <summary>
    /// Buffer hub, all empty trolleys will be moved toward this buffer
    /// </summary>
    internal class BufferHub: Hub
    {

        public BufferHub(string name_, int id_, Point FPoint_, Floor floor_, WalkWay ww_, int initial_trolleys = 0, bool vertical_trolleys_ = true) : 
            base(name_, id_, FPoint_, floor_, ww_, new Size(2000, 200), initial_trolleys: initial_trolleys, vertical_trolleys: vertical_trolleys_)
        {

        }

        /// <summary>
        /// To which tile should the distributer walk to drop off this trolley.
        /// </summary>
        public WalkTile OpenSpot(DanishTrolley t)
        {
            //TODO: This only works for vertical buffer zones.
            //TODO: 1 THIS should make sure you can't add to much trolleys
            int x = (HubTrolleys.Count() * t.VRTrolleySize.Width) + (20 * HubTrolleys.Count);
            int y = FloorPoint.Y + 20;
            return WW.GetTile(new Point(x, y));
        }
    }
}
