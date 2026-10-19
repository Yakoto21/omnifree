using System;
using System.IO;
using Lodino.Core.Api;
using Lodino.Core.Registry;

namespace Lodino.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h" || args[0] == "help")
            {
                PrintHelp();
                return 0;
            }

            string command = args[0].ToLowerInvariant();

            try
            {
                if (command == "convert" && args.Length >= 3)
                {
                    string input = args[1];
                    string target = args[2];
                    string outputDir = args.Length >= 4 ? args[3] : Environment.CurrentDirectory;

                    var registry = EngineRegistry.Default;
                    string srcExt = Path.GetExtension(input).TrimStart('.').ToLowerInvariant();
                    var engine = registry.Resolve(srcExt, target);

                    Console.WriteLine(string.Format("Iniciando conversão: '{0}' -> .{1}", input, target));
                    Console.WriteLine(string.Format("Motor responsável: {0} ({1})", engine.EngineLabel, engine.EngineId));

                    var req = new ConvertRequest
                    {
                        InputPath = Path.GetFullPath(input),
                        TargetFormat = target,
                        OutputDirectory = Path.GetFullPath(outputDir)
                    };

                    var result = engine.Convert(req, pct =>
                    {
                        Console.Write(string.Format("\rProgresso: {0}%", pct));
                    });
                    Console.WriteLine();

                    if (result.Status == "completed")
                    {
                        Console.WriteLine(string.Format("Sucesso! Arquivo gerado em: {0}", result.OutputPath));
                        Console.WriteLine(string.Format("Tempo de processamento: {0} ms ({1:N0} bytes)", result.DurationMs, result.BytesProcessed));
                        return 0;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(string.Format("Falha na conversão: {0}", result.ErrorMessage));
                        Console.ResetColor();
                        return 1;
                    }
                }
                else if (command == "inspect" && args.Length >= 2)
                {
                    string file = args[1];
                    if (!File.Exists(file))
                    {
                        Console.WriteLine("Arquivo não encontrado: " + file);
                        return 1;
                    }

                    using (var fs = File.OpenRead(file))
                    {
                        string ext = Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
                        var registry = EngineRegistry.Default;
                        var engine = registry.Resolve(ext, ext);
                        var info = engine.Inspect(fs, file);

                        Console.WriteLine("========================================");
                        Console.WriteLine("  INSPEÇÃO DE ARQUIVO — LODINO NATIVE   ");
                        Console.WriteLine("========================================");
                        Console.WriteLine(string.Format("Caminho: {0}", Path.GetFullPath(file)));
                        Console.WriteLine(string.Format("Formato Identificado: {0}", info.FormatId));
                        Console.WriteLine(string.Format("MIME Type: {0}", info.MimeType));
                        Console.WriteLine(string.Format("Tamanho: {0:N0} bytes", info.FileSizeBytes));
                        Console.WriteLine(string.Format("Suportado pelo Núcleo: {0}", info.IsSupported ? "Sim" : "Não"));
                        return 0;
                    }
                }
                else if (command == "list-engines")
                {
                    Console.WriteLine("========================================");
                    Console.WriteLine("  MOTORES PRÓPRIOS REGISTRADOS NO NÚCLEO ");
                    Console.WriteLine("========================================");
                    foreach (var engine in EngineRegistry.Default.GetAll())
                    {
                        Console.WriteLine(string.Format("- {0} [{1}]", engine.EngineLabel, engine.EngineId));
                        Console.WriteLine(string.Format("  Entradas: {0}", string.Join(", ", engine.SupportedInputs)));
                        Console.WriteLine(string.Format("  Saídas:   {0}", string.Join(", ", engine.SupportedOutputs)));
                    }
                    return 0;
                }
                else
                {
                    Console.WriteLine("Comando inválido ou argumentos insuficientes.");
                    PrintHelp();
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Erro de execução: " + ex.Message);
                Console.ResetColor();
                return 1;
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("Uso do Lodino Native CLI:");
            Console.WriteLine("  Lodino.exe convert <caminho_entrada> <formato_destino> [pasta_destino]");
            Console.WriteLine("  Lodino.exe inspect <caminho_arquivo>");
            Console.WriteLine("  Lodino.exe list-engines");
            Console.WriteLine("\nExemplos:");
            Console.WriteLine("  Lodino.exe convert dados.csv json");
            Console.WriteLine("  Lodino.exe convert config.ini json .\\saida");
        }
    }
}
