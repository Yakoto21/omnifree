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

                    for (int i = 4; i < args.Length; i++)
                    {
                        string arg = args[i];
                        if (arg.StartsWith("--", StringComparison.Ordinal))
                        {
                            int eq = arg.IndexOf('=');
                            if (eq > 2)
                            {
                                string key = arg.Substring(2, eq - 2).Trim();
                                string val = arg.Substring(eq + 1).Trim();
                                req.Settings[key] = val;
                            }
                        }
                    }

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
                        if (info.Width > 0 && info.Height > 0)
                        {
                            Console.WriteLine(string.Format("Dimensões: {0}x{1}", info.Width, info.Height));
                        }
                        if (info.Metadata != null && info.Metadata.Count > 0)
                        {
                            Console.WriteLine("Metadados detalhados:");
                            foreach (var kvp in info.Metadata)
                            {
                                Console.WriteLine(string.Format("  - {0}: {1}", kvp.Key, kvp.Value));
                            }
                        }
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
                else if (command == "zip" && args.Length >= 2)
                {
                    string input = args[1];
                    string outputZip = null;
                    var level = Lodino.Core.Formats.Archive.Zip.ZipCompressionLevel.Optimal;

                    for (int i = 2; i < args.Length; i++)
                    {
                        if (args[i] == "-o" && i + 1 < args.Length)
                        {
                            outputZip = args[++i];
                        }
                        else if (args[i].StartsWith("--output=", StringComparison.OrdinalIgnoreCase))
                        {
                            outputZip = args[i].Substring("--output=".Length);
                        }
                        else if (args[i] == "-c" && i + 1 < args.Length)
                        {
                            string val = args[++i].ToLowerInvariant();
                            if (val == "store" || val == "none" || val == "0") level = Lodino.Core.Formats.Archive.Zip.ZipCompressionLevel.Store;
                            else if (val == "fast" || val == "1") level = Lodino.Core.Formats.Archive.Zip.ZipCompressionLevel.Fast;
                            else level = Lodino.Core.Formats.Archive.Zip.ZipCompressionLevel.Optimal;
                        }
                        else if (args[i].StartsWith("--compression=", StringComparison.OrdinalIgnoreCase))
                        {
                            string val = args[i].Substring("--compression=".Length).ToLowerInvariant();
                            if (val == "store" || val == "none" || val == "0") level = Lodino.Core.Formats.Archive.Zip.ZipCompressionLevel.Store;
                            else if (val == "fast" || val == "1") level = Lodino.Core.Formats.Archive.Zip.ZipCompressionLevel.Fast;
                            else level = Lodino.Core.Formats.Archive.Zip.ZipCompressionLevel.Optimal;
                        }
                        else if (!args[i].StartsWith("-") && string.IsNullOrEmpty(outputZip))
                        {
                            outputZip = args[i];
                        }
                    }

                    if (string.IsNullOrEmpty(outputZip))
                    {
                        outputZip = Path.GetFileNameWithoutExtension(input.TrimEnd('\\', '/')) + ".zip";
                    }

                    Console.WriteLine(string.Format("Compactando '{0}' -> '{1}' ({2})...", input, outputZip, level));
                    using (var writer = new Lodino.Core.Formats.Archive.Zip.ZipWriter(outputZip))
                    {
                        if (Directory.Exists(input))
                        {
                            writer.AddDirectoryRecursive(input, "", level);
                        }
                        else if (File.Exists(input))
                        {
                            writer.AddFile(input, Path.GetFileName(input), level);
                        }
                        else
                        {
                            Console.WriteLine("Caminho não encontrado: " + input);
                            return 1;
                        }
                        writer.Finish();
                    }
                    Console.WriteLine(string.Format("Arquivo ZIP criado com sucesso: {0} ({1:N0} bytes)",
                        Path.GetFullPath(outputZip), new FileInfo(outputZip).Length));
                    return 0;
                }
                else if (command == "unzip" && args.Length >= 2)
                {
                    string zipFile = args[1];
                    string destDir = null;

                    for (int i = 2; i < args.Length; i++)
                    {
                        if (args[i] == "-o" && i + 1 < args.Length)
                        {
                            destDir = args[++i];
                        }
                        else if (args[i].StartsWith("--output=", StringComparison.OrdinalIgnoreCase))
                        {
                            destDir = args[i].Substring("--output=".Length);
                        }
                        else if (!args[i].StartsWith("-") && string.IsNullOrEmpty(destDir))
                        {
                            destDir = args[i];
                        }
                    }

                    if (string.IsNullOrEmpty(destDir))
                    {
                        destDir = Path.GetFileNameWithoutExtension(zipFile);
                    }

                    if (!File.Exists(zipFile))
                    {
                        Console.WriteLine("Arquivo ZIP não encontrado: " + zipFile);
                        return 1;
                    }

                    Console.WriteLine(string.Format("Extraindo '{0}' -> '{1}'...", zipFile, destDir));
                    using (var reader = new Lodino.Core.Formats.Archive.Zip.ZipReader(zipFile))
                    {
                        reader.ExtractAll(destDir, pct => Console.Write(string.Format("\rProgresso: {0}%", pct)));
                    }
                    Console.WriteLine("\nExtração concluída com sucesso!");
                    return 0;
                }
                else if (command == "list-zip" && args.Length >= 2)
                {
                    string zipFile = args[1];
                    if (!File.Exists(zipFile))
                    {
                        Console.WriteLine("Arquivo ZIP não encontrado: " + zipFile);
                        return 1;
                    }

                    using (var reader = new Lodino.Core.Formats.Archive.Zip.ZipReader(zipFile))
                    {
                        Console.WriteLine("========================================");
                        Console.WriteLine(string.Format("  CONTEÚDO DO ARQUIVO ZIP: {0}", Path.GetFileName(zipFile)));
                        Console.WriteLine("========================================");
                        Console.WriteLine(string.Format("Total de entradas: {0}", reader.Entries.Count));
                        if (!string.IsNullOrEmpty(reader.Comment)) Console.WriteLine("Comentário: " + reader.Comment);
                        Console.WriteLine("----------------------------------------");
                        foreach (var e in reader.Entries)
                        {
                            Console.WriteLine(string.Format("{0,-45} {1,10} -> {2,10} [{3}]",
                                e.Name, e.UncompressedSize.ToString("N0"), e.CompressedSize.ToString("N0"), e.Method));
                        }
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
            Console.WriteLine("  Lodino.exe convert <caminho_entrada> <formato_destino> [pasta_destino] [--chave=valor ...]");
            Console.WriteLine("  Lodino.exe inspect <caminho_arquivo>");
            Console.WriteLine("  Lodino.exe zip <pasta_ou_arquivo> [caminho_zip] [--compression=store|fast|optimal]");
            Console.WriteLine("  Lodino.exe unzip <arquivo_zip> [pasta_destino]");
            Console.WriteLine("  Lodino.exe list-zip <arquivo_zip>");
            Console.WriteLine("  Lodino.exe list-engines");
            Console.WriteLine("\nExemplos:");
            Console.WriteLine("  Lodino.exe convert dados.csv json");
            Console.WriteLine("  Lodino.exe convert arquivo.zip extract .\\pasta_extraida");
            Console.WriteLine("  Lodino.exe zip .\\minha_pasta backup.zip --compression=optimal");
            Console.WriteLine("  Lodino.exe unzip backup.zip .\\saida");
            Console.WriteLine("  Lodino.exe list-zip backup.zip");
        }
    }
}
