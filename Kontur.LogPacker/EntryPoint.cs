using System;
using System.IO;

namespace Kontur.LogPacker
{
    internal static class EntryPoint
    {
        public static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                var (inputFile, outputFile) = (args[0], args[1]);
                
                if (File.Exists(inputFile))
                {
                    var sampless = Analyzer.Analyze(inputFile);

                    if (sampless != null && sampless.Length != 0)
                    {
                        var samples = new Samples(sampless);
                        Precompressor.Compress(inputFile, outputFile, samples);
                    }
                    else
                        Precompressor.Compress0(inputFile, outputFile);

                    return;
                }
            }

            if (args.Length == 3 && args[0] == "-d")
            {
                var (inputFile, outputFile) = (args[1], args[2]);
                
                if (File.Exists(inputFile))
                {
                    Postcompressor.Decompress(inputFile, outputFile);
                    return;
                }
            }
            
            ShowUsage();
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine($"{AppDomain.CurrentDomain.FriendlyName} [-d] <inputFile> <outputFile>");
            Console.WriteLine("\t-d flag turns on the decompression mode");
            Console.WriteLine();
        }
    }
}