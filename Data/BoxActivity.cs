using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class BoxActivity
    {
        public DateTime Datum {  get; set; }
        public DateTime Tijd {  get; set; }
        public int Lgst_aantal_stickers { get; set; }
        public string Relatie_nr_ {  get; set; }
        public string Relatie_collicode {  get; set; }
        public string Beladings_aantallen {  get; set; }
        public string Product_omschrijving_1 { get; set; }
        public string Transactieid { get; set; }
        public string Opmerking5 {  get; set; }
        public int Entry {  get; set; }
        public DateTime Date { get; set; }
        public ShopHub Destination { get; set; }

        public ShopHub InitActivity(Dictionary<string, ShopHub> DestPlusDayToHub)
        {
            Beladings_aantallen = Beladings_aantallen.Replace("[", "").Replace("]", "");
            Date = new DateTime(Datum.Year, Datum.Month, Datum.Day, Tijd.Hour, Tijd.Minute, Tijd.Second);
            Destination = DestPlusDayToHub[Relatie_collicode + "-" + Opmerking5];
            return Destination;
        }

        public int GetUnits()
        {
            return int.Parse(Beladings_aantallen.Split('x')[1]) * int.Parse(Beladings_aantallen.Split('x')[0]);
        }
    }

    internal class BoxActivityMap : ClassMap<BoxActivity>
    {
        public BoxActivityMap()
        {
            Map(m => m.Datum).Index(0);
            Map(m => m.Tijd).Index(1);
            Map(m => m.Lgst_aantal_stickers).Index(2);
            Map(m => m.Relatie_nr_).Index(3);
            Map(m => m.Relatie_collicode).Index(4);
            Map(m => m.Beladings_aantallen).Index(5);
            Map(m => m.Product_omschrijving_1).Index(6);
            Map(m => m.Transactieid).Index(7);
            Map(m => m.Opmerking5).Index(8);
            Map(m => m.Entry).Index(9);
        }
    }

}
