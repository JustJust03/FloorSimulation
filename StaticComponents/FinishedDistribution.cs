using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FloorSimulation
{
    /// <summary>
    /// The main class that handles when the distribution simulation is finished.
    /// Writes information about the run to a file.
    /// </summary>
    internal class FinishedDistribution
    {
        public bool Completed = false;
        public string DistributionDate;
        public string Layout;
        public string TotalTime;
        private Floor floor;

        public FinishedDistribution(Floor Floor)
        {
            floor = Floor;
        }

        /// <summary>
        /// When the distribution is completed this function is called.
        /// It stops the simulation and saves some variables for the output file.
        /// </summary>
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

        /// <summary>
        /// Check if the distribution is completed.
        /// True when the starthub is empty and all distributers are stuck on the task TakeFullTrolley
        /// </summary>
        /// <returns></returns>
        public bool CheckFinishedDistribution()
        {
            if (!floor.FirstStartHub.StartHubEmpty)
                return false;

            foreach (Distributer d in floor.DistrList)
                if (!(d.MainTask.Goal == "TakeFullTrolley"))
                    return false; // One of the distributers is not trying to take a new trolley i.e. Is not finished with the distribution cycle.

            DistributionCompleted();
            return true;
        }

        /// <summary>
        /// Writes information about the run to a file.
        /// </summary>
        public void WriteFile()
        {
            JObject TotalData = JObject.FromObject(this);
            TimeSpan TotalVerdeelTijd = new TimeSpan();
            TimeSpan TotalWachtTijd = new TimeSpan();
            TimeSpan TotalVerspilTijd = new TimeSpan();
            TimeSpan TotalTimeTimes = TimeSpan.FromTicks((long)(floor.ElapsedSimTime.Ticks * floor.DistrList.Count));

            JObject JOdistributer = new JObject();
            foreach (Distributer d in floor.DistrList)
            {
                TotalVerdeelTijd = TotalVerdeelTijd.Add(d.VerdeelTijd);
                TotalWachtTijd = TotalWachtTijd.Add(d.WachtTijd);
                TotalVerspilTijd = TotalVerspilTijd.Add(d.VerspilTijd);

                JObject dbuter_info = new JObject
                {
                    {"ID", d.id },
                    {"VerdeelTijd", d.VerdeelTijd.ToString(@"hh\:mm\:ss")},
                    {"WachtTijd", d.WachtTijd.ToString(@"hh\:mm\:ss")},
                    {"VerspilTijd", d.VerspilTijd.ToString(@"hh\:mm\:ss")}
                };
                JOdistributer.Add("Distributer " + (d.id + 1), dbuter_info);
            }
            JObject DistrTotals = new JObject
            {
                {"VerdeelTijd", SafeDivisionPercentage(TotalVerdeelTijd.Ticks, TotalTimeTimes.Ticks)},
                {"WachtTijd", SafeDivisionPercentage(TotalWachtTijd.Ticks, TotalTimeTimes.Ticks)},
                {"VerspilTijd", SafeDivisionPercentage(TotalVerspilTijd.Ticks, TotalTimeTimes.Ticks) }
            };
            JOdistributer.Add("Totals", DistrTotals);

            JObject JOdistributers = new JObject();
            JOdistributers.Add("Distributers", JOdistributer);


            TotalData.Merge(JOdistributers);

            string json = TotalData.ToString();
            string FilePath = Program.rootfolder + @"\Results\TEST1.json";
            File.WriteAllText(FilePath, json);  
        }

        /// <summary>
        /// a / b
        /// return 0 when b is 0 and rounds the answer.
        /// </summary>
        private double SafeDivisionPercentage(double a, double b)
        {
            if (b == 0)
                return 0;
            else return Math.Round((a / b) * 100, 2);
        }
    }
}
