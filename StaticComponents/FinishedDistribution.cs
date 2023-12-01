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
        public string TotalTrolleysDistributed;
        public string TotalTrolleysExported;
        public string DistributersWorking;
        private Floor floor;
        
        private int TotalTrolleysDistr;
        private int TotalTrolleysExp;

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

            TotalTrolleysDistr = 0; //Wordt in the writefile geupdate.
            foreach(Distributer d in floor.DistrList)
                TotalTrolleysDistr += d.MainTask.AInfo.NewFullTrolleyFreq;
            TotalTrolleysExp = floor.TrHub.TrolleysExported + floor.FullTrolleysOnFloor();
            TotalTrolleysDistributed = TotalTrolleysDistr.ToString();
            TotalTrolleysExported = TotalTrolleysExp.ToString();
            DistributersWorking = floor.DistrList.Count.ToString();

            DistributionDate = floor.Display.date;
            Layout = floor.layout.ToString();
            TotalTime = floor.ElapsedSimTime.ToString(@"hh\:mm\:ss");
            WriteFile();
            WriteHeatMap();
        }

        /// <summary>
        /// Check if the distribution is completed.
        /// True when the starthub is empty and all distributers are stuck on the task TakeFullTrolley
        /// </summary>
        /// <returns></returns>
        public bool CheckFinishedDistribution()
        {
            if (!floor.StartHubsEmpty())
                return false;

            foreach (Distributer d in floor.DistrList)
                if (!(d.MainTask.Goal == "TakeFullTrolley"))
                    return false; // One of the distributers is not trying to take a new trolley i.e. Is not finished with the distribution cycle.

            DistributionCompleted();
            return true;
        }

        public void WriteHeatMap()
        {
            int[,] intArray = new int[floor.FirstWW.WalkTileList.Count, floor.FirstWW.WalkTileList[0].Count];

            for(int x = 0; x < floor.FirstWW.WalkTileList.Count; x++)
                for(int y = 0; y < floor.FirstWW.WalkTileList[0].Count; y++)
                    intArray[x, y] = floor.FirstWW.WalkTileList[x][y].visits;

            string json = JsonConvert.SerializeObject(intArray);
            string FilePath = Program.rootfolder + @"\Results\HeatMap Results\" + floor.Display.SaveFileBase + ".json";
            File.WriteAllText(FilePath, json);
        }


        /// <summary>
        /// Writes information about the run to a file.
        /// </summary>
        public void WriteFile()
        {
            JObject TotalData = JObject.FromObject(this);

            JObject JOconstants = new JObject
            {
                {"Verdeeltijd (s)", plant.ReorderTime / 1000},
                {"Loopsnelheid Distributer (km/h)", Math.Round(Distributer.WALKSPEED / 27.7f, 1)},
                {"Loopsnelheid Distributer Kar (km/h)", Math.Round(DanishTrolley.TrolleyTravelSpeed/ 27.7f, 1)},
                {"Loopsnelheid Lange Harry (km/h)", Math.Round(LangeHarry.HarryTravelSpeed / 27.7f, 1)},
                {DanishTrolley.FullDetection(), DanishTrolley.MaxTotalStickers},
                {"Kans op bord (% per sticker)", Distributer.OddsOfBord * 100},
                {"Kans op laag (% per sticker)", Distributer.OddsOfLaag * 100},
                {"Kans op herindelen (% per sticker)", Distributer.OddsOfHer * 100},
                {"Bord tijd (s)", Distributer.BordTime / 1000},
                {"Laag tijd (s)", Distributer.LaagTime / 1000},
                {"Herindeel tijd (s)", Distributer.HerTime / 1000}
            };
            TotalData.Add("Constants", JOconstants);

            TimeSpan TotalVerdeelTijd = new TimeSpan();
            TimeSpan TotalWachtTijd = new TimeSpan();
            TimeSpan TotalHerindeelTijd = new TimeSpan();
            TimeSpan TotalNieuwBordTijd = new TimeSpan();
            TimeSpan TotalLaagBijTijd = new TimeSpan();
            TimeSpan TotalTransportVerdeelTijd = new TimeSpan();
            TimeSpan TotalNewShopTrolleyTijd = new TimeSpan();
            TimeSpan TotalTakeFullTrolleyTijd = new TimeSpan();
            TimeSpan TotalTrolleyTijd = new TimeSpan();

            int TotalVerdeelFreq = 0;
            int TotalWachtFreq = 0;
            int TotalHerindeelFreq = 0;
            int TotalNieuwBordFreq = 0;
            int TotalLaagBijFreq = 0;
            int TotalNewShopTrolleyFreq = 0;
            int TotalNewTrolleyFreq = 0;

            JObject JOdistributer = new JObject();
            foreach (Distributer d in floor.DistrList)
            {
                d.MainTask.AInfo.UpdateGeneralTimes();

                TotalVerdeelTijd = TotalVerdeelTijd.Add(d.MainTask.AInfo.VerdeelTijd);
                TotalWachtTijd = TotalWachtTijd.Add(d.MainTask.AInfo.WachtTijd);
                TotalHerindeelTijd = TotalHerindeelTijd.Add(d.MainTask.AInfo.HerindeelTijd);
                TotalNieuwBordTijd = TotalNieuwBordTijd.Add(d.MainTask.AInfo.NieuwBordTijd);
                TotalLaagBijTijd = TotalLaagBijTijd.Add(d.MainTask.AInfo.LaagBijTijd);
                TotalTransportVerdeelTijd = TotalTransportVerdeelTijd.Add(d.MainTask.AInfo.TransportVerdeelTijd);
                TotalNewShopTrolleyTijd = TotalNewShopTrolleyTijd.Add(d.MainTask.AInfo.NewShopTrolleyTijd);
                TotalTakeFullTrolleyTijd = TotalTakeFullTrolleyTijd.Add(d.MainTask.AInfo.TakeNewFullTrolleyTijd);
                TotalTrolleyTijd = TotalTrolleyTijd.Add(d.MainTask.AInfo.TotalTrolleyTime);

                TotalVerdeelFreq += d.MainTask.AInfo.VerdeelFreq;
                TotalWachtFreq += d.MainTask.AInfo.WachtFreq;
                TotalHerindeelFreq += d.MainTask.AInfo.HerindeelFreq;
                TotalNieuwBordFreq += d.MainTask.AInfo.NieuwBordFreq;
                TotalLaagBijFreq += d.MainTask.AInfo.LaagBijFreq;
                TotalNewShopTrolleyFreq += d.MainTask.AInfo.NewShopTrolleyFreq;
                TotalNewTrolleyFreq += d.MainTask.AInfo.NewFullTrolleyFreq;


                JObject dbuter_info = new JObject
                {
                    {"ID", d.id },
                    {"Verdeel Tijd", d.MainTask.AInfo.VerdeelTijd.ToString(@"hh\:mm\:ss")},
                    {"Verdeel Frequency", d.MainTask.AInfo.VerdeelFreq},

                    {"Wacht Tijd", d.MainTask.AInfo.WachtTijd.ToString(@"hh\:mm\:ss")},
                    {"Wacht Frequency", d.MainTask.AInfo.WachtFreq},

                    {"Herindeel Tijd", d.MainTask.AInfo.HerindeelTijd.ToString(@"hh\:mm\:ss")},
                    {"Herindeel Frequency", d.MainTask.AInfo.HerindeelFreq},

                    {"Nieuw Bord Tijd", d.MainTask.AInfo.NieuwBordTijd.ToString(@"hh\:mm\:ss")},
                    {"Nieuw Bord Frequency", d.MainTask.AInfo.NieuwBordFreq},

                    {"Laag Bij Tijd", d.MainTask.AInfo.LaagBijTijd.ToString(@"hh\:mm\:ss")},
                    {"Laag Bij Frequency", d.MainTask.AInfo.LaagBijFreq},

                    {"Transport tussen verdelen Tijd", d.MainTask.AInfo.TransportVerdeelTijd.ToString(@"hh\:mm\:ss")},
                    {"Transport tussen verdelen Frequency", Math.Max(d.MainTask.AInfo.VerdeelFreq - 1, 0)},

                    {"Nieuwe Shop trolley Tijd", d.MainTask.AInfo.NewShopTrolleyTijd.ToString(@"hh\:mm\:ss")},
                    {"Nieuwe Shop Trolley Frequency", d.MainTask.AInfo.NewShopTrolleyFreq},

                    {"Nieuwe volle trolley pak Tijd", d.MainTask.AInfo.TakeNewFullTrolleyTijd.ToString(@"hh\:mm\:ss")},

                    {"Totale tijd voor trolley", d.MainTask.AInfo.TotalTrolleyTime.ToString(@"hh\:mm\:ss")},
                    {"Aantal trolley's verdeeld", d.MainTask.AInfo.NewFullTrolleyFreq},
                };
                JOdistributer.Add("Distributer " + (d.id + 1), dbuter_info);
            }

            JObject DistrAverage = new JObject
            {
                {"Verdeel Tijd", TSSafeDivision(TotalVerdeelTijd, TotalNewTrolleyFreq).ToString(@"hh\:mm\:ss")},
                {"Verdeel Frequency", ISafeDivision(TotalVerdeelFreq, TotalNewTrolleyFreq)},

                {"Wacht Tijd", TSSafeDivision(TotalWachtTijd, TotalNewTrolleyFreq).ToString(@"hh\:mm\:ss")},
                {"Wacht Frequency", ISafeDivision(TotalWachtFreq, TotalNewTrolleyFreq)},

                {"Herindeel Tijd", TSSafeDivision(TotalHerindeelTijd, TotalNewTrolleyFreq).ToString(@"hh\:mm\:ss")},
                {"Herindeel Frequency", ISafeDivision(TotalHerindeelFreq, TotalNewTrolleyFreq)},

                {"Nieuw Bord Tijd", TSSafeDivision(TotalNieuwBordTijd, TotalNewTrolleyFreq).ToString(@"hh\:mm\:ss")},
                {"Nieuw Bord Frequency", ISafeDivision(TotalNieuwBordFreq, TotalNewTrolleyFreq)},

                {"Laag Bij Tijd", TSSafeDivision(TotalLaagBijTijd, TotalNewTrolleyFreq).ToString(@"hh\:mm\:ss")},
                {"Laag Bij Frequency", ISafeDivision(TotalLaagBijFreq, TotalNewTrolleyFreq)},

                {"Transport tussen verdelen Tijd", TSSafeDivision(TotalTransportVerdeelTijd, TotalNewTrolleyFreq).ToString(@"hh\:mm\:ss")},

                {"Nieuwe Shop trolley Tijd", TSSafeDivision(TotalNewShopTrolleyTijd, TotalNewTrolleyFreq).ToString(@"hh\:mm\:ss")},
                {"Nieuwe Shop Trolley Frequency", ISafeDivision(TotalNewShopTrolleyFreq, TotalNewTrolleyFreq)},

                {"Nieuwe volle trolley pak Tijd", TSSafeDivision(TotalTakeFullTrolleyTijd, TotalNewTrolleyFreq).ToString(@"hh\:mm\:ss")},

                {"Totale Tijd Voor Trolley", TSSafeDivision(TotalTrolleyTijd, TotalNewTrolleyFreq).ToString(@"hh\:mm\:ss")},
            };
            JOdistributer.Add("AveragePerTrolley", DistrAverage);

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
        private TimeSpan TSSafeDivision(TimeSpan a, int b)
        {
            if (b == 0)
                return TimeSpan.FromTicks(0);
            else return TimeSpan.FromTicks(a.Ticks / b);
        }
        private double ISafeDivision(int a, int b)
        {
            if (b == 0)
                return 0;
            else return Math.Round(Convert.ToDouble(a) / Convert.ToDouble(b), 2);
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

        public TimeSpan TotalTrolleyTime;

        public ref TimeSpan CurrentTask
        {
            get
            {
                if (!MTask.InTask)
                    return ref FillerTijd;
                if (MTask.Waiting || MTask.TargetWasSaveTile)
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
        public int NewShopTrolleyFreq = 0;
        public int NewFullTrolleyFreq = 0;

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

        public void UpdateGeneralTimes()
        {
            TotalTrolleyTime = VerdeelTijd + WachtTijd + HerindeelTijd + NieuwBordTijd + LaagBijTijd + TransportVerdeelTijd + NewShopTrolleyTijd + TakeNewFullTrolleyTijd;
        }

        public void UpdateFreq(string goal, bool ForceUpdate = false)
        {
            if (OldGoal == goal && !ForceUpdate)
                return;

            OldGoal = goal;
            if (goal == "DeliveringEmptyTrolley")
                NewFullTrolleyFreq++;
            else if (goal == "DistributePlants")
            {
                if (DButer.SideActivity == "Bord")
                    NieuwBordFreq++;
                else if (DButer.SideActivity == "Laag")
                    LaagBijFreq++;
                else if (DButer.SideActivity == "Her")
                    HerindeelFreq++;
                else
                    VerdeelFreq++;
            }
            else if (goal == "DeliverEmptyTrolleyToShop")
                NewShopTrolleyFreq++;
        }
        
        public void UpdateWachtFreq()
        {
            WachtFreq++;
        }
    }
}
