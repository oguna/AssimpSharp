using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using AssimpSharp.FBX;
using System.Text;

namespace AssimpView
{
    static class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string file;
            if (args != null && args.Length == 1 && !string.IsNullOrEmpty(args[0]))
            {
                file =args[0];
            }
            else
            {
                var dialog = new OpenFileDialog();
                dialog.InitialDirectory = Directory.GetCurrentDirectory();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    file = dialog.FileName;
                }
                else
                {
                    return;
                }
            }
            
            using (var program = new AssimpViewGame(file))
            {
                program.Run();
            }
        }
    }
}
