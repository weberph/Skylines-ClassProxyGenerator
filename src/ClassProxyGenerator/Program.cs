using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ClassProxyGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Start(args);
            }
            catch (ProxyGeneratorException e)
            {
                Console.Error.WriteLine("Proxy generation failed: " + e.Message);
                Environment.Exit(-1);
            }
            catch (ProxyCompilationException e)
            {
                Console.Error.WriteLine("Proxy compilation failed:");
                var errors = e.EmitResult.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
                foreach (var error in errors)
                {
                    Console.Error.WriteLine(error.GetMessage());
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unexpected error: " + e.Message);
                Console.Error.WriteLine(e.StackTrace);
                Environment.Exit(-2);
            }
        }

        private static void Start(string[] args)
        {
            string inputAssemblyPath = null;
            string csManagedDirectory = null;

            if (args.Length != 2)
            {
                if (Debugger.IsAttached)
                {
                    var testSettings = File.ReadAllLines(@"..\..\LocalTestSettings.txt");
                    Debug.Assert(testSettings.Length == 2);
                    inputAssemblyPath = testSettings[0];
                    csManagedDirectory = testSettings[1];
                }
                else
                {
                    PrintUsage();
                    Environment.Exit(1);
                }
            }
            else
            {
                inputAssemblyPath = args[0];
                csManagedDirectory = args[1];
            }

            if (string.IsNullOrWhiteSpace(inputAssemblyPath) || !File.Exists(inputAssemblyPath))
            {
                Console.Error.WriteLine("InputAssembly not found");
                PrintUsage();
                Environment.Exit(2);
            }

            if (string.IsNullOrWhiteSpace(csManagedDirectory) || !Directory.Exists(csManagedDirectory))
            {
                Console.Error.WriteLine("Cities_Data\\Managed directory not found");
                PrintUsage();
                Environment.Exit(3);
            }

            var rawInputAssembly = File.ReadAllBytes(inputAssemblyPath);
            var proxyGenerator = new ProxyGenerator(rawInputAssembly, csManagedDirectory);
            var proxyAssembly = proxyGenerator.Compile();

            var targetFile = Path.Combine(Path.GetDirectoryName(inputAssemblyPath), Path.GetFileNameWithoutExtension(inputAssemblyPath) + "Proxy.dll");
            File.WriteAllBytes(targetFile, proxyAssembly);
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine(@"Usage: ClassProxyGenerator <InputAssembly> <Cities_Data\Managed path>");
            Console.Error.WriteLine(@" e.g.: ClassProxyGenerator c:\MyMod\MyMod.dll c:\Steam\SteamApps\common\Cities_Skylines\Cities_Data\Managed\");
        }
    }
}
