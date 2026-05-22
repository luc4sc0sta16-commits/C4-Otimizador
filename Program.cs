using System;
using System.Windows.Forms;

namespace C4_Otimizador
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Garante que o Windows use os estilos visuais modernos nos botões arredondados
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Inicia a C4 V4
            Application.Run(new Form1());
        }
    }
}
