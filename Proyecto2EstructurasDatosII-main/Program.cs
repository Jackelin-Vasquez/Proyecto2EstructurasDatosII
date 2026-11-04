using System;
using System.Windows.Forms;
using Proyecto2EstructurasDatosII.Interfaz;

namespace Proyecto2EstructurasDatosII
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormPrincipal());
        }
    }
}
