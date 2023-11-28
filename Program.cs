using System;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;

namespace FloorSimulation
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //public const string rootfolder = @"Z:\justh\AllGreen\Simulation\FloorSimulation";
        //public const string rootfolder = @"C:\Users\frank van der salm\source\repos\JustJust03\FloorSimulation";
        private static readonly DirectoryInfo rootfolder1 = Directory.GetParent(Directory.GetCurrentDirectory());

        public static readonly string rootfolder = Directory.GetParent(rootfolder1.FullName).FullName;
        public const int TICKS_PER_SECOND = 30;

        [STAThread]
        static void Main()
        {
            bool RunSimpleCalculation = false;

            if (RunSimpleCalculation)
                SimpleCalculation.GetPlantsPerTrolley("2023-04-14");
            else
            {
                Application.EnableVisualStyles();
                Application.Run(new MainDisplay());
            }
        }
    }
}
