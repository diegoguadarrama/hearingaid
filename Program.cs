using System;
using System.Windows.Forms;

namespace HearingAI
{
    internal static class Program
    {
        private static string GetLogPath()
        {
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HearingAI");
            try { if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir); } catch { }
            return System.IO.Path.Combine(dir, "error.log");
        }

        private static void LogError(string message, Exception? ex = null)
        {
            try
            {
                var log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n" + (ex != null ? ex.ToString() + "\r\n" : "");
                System.IO.File.AppendAllText(GetLogPath(), log);
            }
            catch { }
        }

        private static void SetupGlobalExceptionHandlers()
        {
            Application.ThreadException += (s, e) =>
            {
                LogError("Unhandled UI thread exception", e.Exception);
                try { MessageBox.Show($"An unexpected error occurred and the app must close.\r\n\r\n{e.Exception.Message}", "HearingAI", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                LogError("Unhandled non-UI exception", ex);
                try { MessageBox.Show($"A fatal error occurred and the app must close.\r\n\r\n{ex?.Message}", "HearingAI", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
            };
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            SetupGlobalExceptionHandlers();

            try
            {
                // Run the full SettingsForm
                var settingsForm = new SettingsForm();

                // Safety: bring the form to front on Shown to avoid rare visibility issues
                settingsForm.Shown += (s, e) =>
                {
                    try
                    {
                        ((Form)s!).WindowState = FormWindowState.Normal;
                        ((Form)s!).BringToFront();
                        ((Form)s!).Activate();
                    }
                    catch { }
                };

                Application.Run(settingsForm);
            }
            catch (Exception ex)
            {
                LogError("Fatal error during startup", ex);
                try { MessageBox.Show($"Failed to start HearingAI:\r\n\r\n{ex.Message}", "HearingAI", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
            }
        }
    }
}
