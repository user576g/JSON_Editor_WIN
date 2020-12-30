using System;
using System.Windows.Forms;
using System.Drawing;

namespace JSON_Editor
{
    static class Program
    {
        internal static Image blankImg = Properties.Resources.blank_Image;
        internal static Image checkImg = Properties.Resources.check_ico.ToBitmap();

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }
    }
}
