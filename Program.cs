using System;
using System.Threading;
using System.Windows.Forms;

namespace LilyHid
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            using (var loop = new QmkTray())
            {
                while (loop.IsNotififyIconVisible)
                {
                    Application.DoEvents();
                    Thread.Sleep(100);
                }
            }

            Application.Exit();
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                e.ExceptionObject.ToString(),
                e.ExceptionObject.GetType().ToString(),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
