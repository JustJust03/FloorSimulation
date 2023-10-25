using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class HubData
    {
        public string No_ {  get; set; }
        public string Search_Name {  get; set; }
        public string Code_collistikkers {  get; set; }
        public int Zoeknaam2 {  get; set; }
        public string ColliPlusDay {  get; set; }

        public HubData Clone()
        {
            return new HubData
            {
                No_ = this.No_,
                Search_Name = this.Search_Name,
                Code_collistikkers = this.Code_collistikkers,
                Zoeknaam2 = this.Zoeknaam2,
                ColliPlusDay = this.ColliPlusDay
            };
        }
    }

    internal class HubDataMap : ClassMap<HubData>
    {
        public HubDataMap()
        {
            Map(m => m.No_).Index(0);
            Map(m => m.Search_Name).Index(1);
            Map(m => m.Code_collistikkers).Index(2);
            Map(m => m.Zoeknaam2).Index(3);
        }
    }

}
