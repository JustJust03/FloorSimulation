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
        Floor floor;

        public FinishedDistribution(Floor Floor)
        {
            floor = Floor;
        }

        public void DistributionCompleted()
        {
            Completed = true;
            MainDisplay m = floor.Display;
            if (m.isSimulating) //Stop the timer from ticking
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

            string FilePath = Program.rootfolder + @"\Results\0data.json";
            File.WriteAllText(FilePath, json);  
        }

        public bool CheckFinishedDistribution()
        {
            if (!floor.FirstStartHub.StartHubEmpty)
                return false;

            foreach(Distributer d in floor.DistrList)
                if (!(d.MainTask.Goal == "TakeFullTrolley"))
                    return false; // One of the distributers is not trying to take a new trolley i.e. Is not finished with the distribution cycle.

            DistributionCompleted();
            return true;
        }

    }
}
