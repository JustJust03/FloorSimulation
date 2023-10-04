using System;
using System.Windows.Forms;
using System.IO;

namespace FloorSimulation
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //public const string rootfolder = @"Z:\justh\AllGreen\Simulation\FloorSimulation";
        public const string rootfolder = @"C:\Users\frank van der salm\source\repos\JustJust03\FloorSimulation";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new MainDisplay());
        }
    }
}
