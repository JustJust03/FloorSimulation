using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FloorSimulation
{
    internal class FinishedDistribution
    {
        public bool Completed = false;
        public string DistributionDate;
        public string Layout;
        public string TotalTime;


        public void DistributionCompleted(Floor floor)
        {
            Completed = true;
            MainDisplay m = floor.Display;
            if (m.isSimulating)
            {
                m.timer.Stop();
                m.ss_button.Text = "Start";
                m.isSimulating = false;
            }

            DistributionDate = floor.Display.date;
            Layout = floor.Layout;
            TotalTime = floor.ElapsedSimTime.ToString(@"hh\:mm\:ss");
            WriteFile();
        }

        public void WriteFile()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);

            string FilePath = Program.rootfolder + @"\Results\data.json";
            File.WriteAllText(FilePath, json);  
        }

    }
}
