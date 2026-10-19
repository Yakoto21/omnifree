using System;
using System.IO;
using System.Runtime.InteropServices;
using Lodino.Core.Registry;

namespace Lodino.Desktop
{
    /// <summary>
    /// Ponto de entrada da aplicação cliente desktop nativa do Lodino (pure Win32).
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                // Inicializa o gerenciador da fila de conversões
                var manager = new ConversionManager();

                // Suporte a argumentos CLI (abrir arquivos passados como parâmetro ou arrastados sobre o executável)
                if (args != null && args.Length > 0)
                {
                    foreach (string arg in args)
                    {
                        if (!string.IsNullOrEmpty(arg) && File.Exists(arg))
                        {
                            manager.AddFile(Path.GetFullPath(arg));
                        }
                    }
                }

                // Cria e exibe a janela principal nativa
                var appWindow = new LodinoAppWindow(manager);
                if (!appWindow.CreateAndShow())
                {
                    NativeMethods.MessageBox(IntPtr.Zero,
                        "Não foi possível criar a janela principal da aplicação Lodino.",
                        "Erro de Inicialização — Lodino",
                        0x10 /* MB_ICONERROR */);
                    return 1;
                }

                // Laço de despacho de mensagens Win32 (Message Loop)
                NativeMethods.MSG msg;
                while (NativeMethods.GetMessage(out msg, IntPtr.Zero, 0, 0) > 0)
                {
                    NativeMethods.TranslateMessage(ref msg);
                    NativeMethods.DispatchMessage(ref msg);
                }

                return (int)msg.wParam;
            }
            catch (Exception ex)
            {
                NativeMethods.MessageBox(IntPtr.Zero,
                    "Erro fatal durante a execução do Lodino:\n\n" + ex.Message,
                    "Falha Crítica — Lodino",
                    0x10 /* MB_ICONERROR */);
                return -1;
            }
        }
    }
}
