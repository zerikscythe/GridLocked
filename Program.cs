using System;
using System.Windows.Forms;

namespace GridLocked
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string moduleToLoad = null;
            if (args != null && args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            {
                moduleToLoad = args[0];
            }

            if (string.IsNullOrEmpty(moduleToLoad))
            {
                using (var selector = new ModuleSelectForm())
                {
                    if (selector.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(selector.SelectedModulePath))
                    {
                        moduleToLoad = selector.SelectedModulePath;
                    }
                    else
                    {
                        return; // User canceled or closed launcher
                    }
                }
            }

            Application.Run(new Form1(moduleToLoad));
        }
    }
}
