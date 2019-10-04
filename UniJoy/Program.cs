using System;
using System.Windows.Forms;

namespace UniJoy
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ExcelProtocolConfigFileLoader excelLoader = new ExcelProtocolConfigFileLoader();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GuiInterface(ref excelLoader));
        }
    }
}
