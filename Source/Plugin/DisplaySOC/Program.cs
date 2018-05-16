using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using STARS.Applications.VETS.Plugins.SOC;

namespace DisplaySOC
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// args looks like: Title, Units, MaxSoc, DeltaSoc0, DeltaSoc1, DeltaSoc2, ...
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args == null || args.Length < 5) return;
            //if (args == null || args.Length < 5) args = new string[] { "hello there", "Ah", "175", "0", "-165", "-3", "-2", "-1", "-1" };
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(args));
        }
    }
}
