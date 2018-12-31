using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSMouse
{
    static class Program
    {
        public const string VERSION = "181231";
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            PSMouse psm = new PSMouse();
            Application.Run(new MainForm(psm));
        }
    }
}
