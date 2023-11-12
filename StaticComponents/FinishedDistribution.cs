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
                m.ControlPanel.timer.Stop();
                m.ControlPanel.ss_button.Text = "Start";
                m.isSimulating = false;
            }

            DistributionDate = floor.Display.date;
            Layout = floor.layout.ToString();
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
                TotalVerdeelTijd = TotalVerdeelTijd.Add(d.MainTask.AInfo.VerdeelTijd);
                TotalWachtTijd = TotalWachtTijd.Add(d.MainTask.AInfo.WachtTijd);
                TotalVerspilTijd = TotalVerspilTijd.Add(d.MainTask.AInfo.VerspilTijd);

                JObject dbuter_info = new JObject
                {
                    {"ID", d.id },
                    {"VerdeelTijd", d.MainTask.AInfo.VerdeelTijd.ToString(@"hh\:mm\:ss")},
                    {"WachtTijd", d.MainTask.AInfo.WachtTijd.ToString(@"hh\:mm\:ss")},
                    {"VerspilTijd", d.MainTask.AInfo.VerspilTijd.ToString(@"hh\:mm\:ss")}
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
            string FilePath = Program.rootfolder + @"\Results\" + floor.Display.SaveFileBase + ".json";
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

    internal class AnalyzeInfo
    {
        public Distributer DButer;
        public Task MTask;
        public int TickMs;

        public TimeSpan VerdeelTijd;
        public TimeSpan WachtTijd; 
        public TimeSpan HerindeelTijd;
        public TimeSpan NieuwBordTijd;
        public TimeSpan LaagBijTijd;
        public TimeSpan TransportVerdeelTijd;
        public TimeSpan NewShopTrolleyTijd;
        public TimeSpan TakeNewFullTrolleyTijd;
        public TimeSpan OtherTijd;
        public TimeSpan FillerTijd; //Wordt alleen aan toegevoegd wanneer je niet in een task bent.

        public TimeSpan VerspilTijd;
        public TimeSpan TrolleyTime;

        public ref TimeSpan CurrentTask
        {
            get
            {
                if (!MTask.InTask)
                    return ref FillerTijd;
                if (MTask.Waiting)
                    return ref WachtTijd;

                if(MTask.Travelling)
                    switch (MTask.Goal)
                    {
                        case "DistributePlants":
                            return ref TransportVerdeelTijd;

                        case "PushTrolleyAway":
                            return ref NewShopTrolleyTijd;
                        case "TakeFinishedTrolley":
                            return ref NewShopTrolleyTijd;
                        case "DeliverFullTrolley":
                            return ref NewShopTrolleyTijd;
                        case "TakeEmptyTrolley":
                            return ref NewShopTrolleyTijd;
                        case "MoveEmptyTrolleyDown":
                            return ref NewShopTrolleyTijd;
                        case "DeliverEmptyTrolleyToShop":
                            return ref NewShopTrolleyTijd;
                        case "TakeOldTrolley":
                            return ref NewShopTrolleyTijd;

                        case "TakeFullTrolley":
                            return ref TakeNewFullTrolleyTijd;
                        case "DeliveringEmptyTrolley":
                            return ref TakeNewFullTrolleyTijd;

                        default:
                            return ref OtherTijd;
                    }

                if(MTask.Goal == "DistributePlants")
                {
                    switch (DButer.SideActivity)
                    {
                        case "Bord":
                            return ref NieuwBordTijd;
                        case "Laag":
                            return ref LaagBijTijd;
                        case "Her":
                            return ref HerindeelTijd;
                        default:
                            return ref VerdeelTijd;
                    }
                }

                return ref OtherTijd;
            }
        }

        public int VerdeelFreq = 0;
        public int WachtFreq = 0;
        public int HerindeelFreq = 0;
        public int NieuwBordFreq = 0;
        public int LaagBijFreq = 0;
        public int TransportVerdeelFreq = 0;
        public int NewEmptyTrolleyFreq = 0;
        public int NewFullTrolleyFreq = 0;
        public int NTrolleysDistributed = 0;

        private string OldGoal;

        public AnalyzeInfo(Distributer dbuter_, Task MTask_, int Tickms_)
        {
            DButer = dbuter_;
            MTask = MTask_;
            TickMs = Tickms_;
        }

        public void TickAnalyzeInfo(int TickMultiplier)
        {
            CurrentTask = CurrentTask.Add(TimeSpan.FromMilliseconds(TickMs * TickMultiplier));
        }
    }
}
