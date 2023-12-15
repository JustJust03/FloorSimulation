using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation
{
    internal class LowPadAccessHub : Hub
    {
        public List<ShopHub> shops = new List<ShopHub>();
        WalkTile DbAccesspoint;

        WalkTile LowerAccesspoint;

        public LowPadAccessHub(string name_, int id_, Point FPoint_, Floor floor_, Size s, List<ShopHub> shops_) : 
            base(name_, id_, FPoint_, floor_, s, vertical_trolleys: true)
        {
            shops = shops_;
            if (shops[0].HasLeftAccess)
                DbAccesspoint = WW.GetTile(new Point(RFloorPoint.X + RHubSize.Width + 40, RFloorPoint.Y));
            else
                DbAccesspoint = WW.GetTile(new Point(RFloorPoint.X - 40, RFloorPoint.Y));
        }

        public override List<WalkTile> OpenSpots(Agent agent)
        {
            return new List<WalkTile> {WW.GetTile(RFloorPoint), LowerAccesspoint};
        }

        public WalkTile DbOpenSpots()
        {
            return DbAccesspoint;
        }
    }
}
