using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using CommandLine.Text;

namespace OSBExtract
{
    public class OSBExtract
    {
        public class Options
        {
            [Option('i', "input", Required = true, HelpText = "Input files to be processed. Can be files or directories.")]
            public IEnumerable<string> InputFiles { get; set; }
            [Option('o', "output", Required = false, HelpText = "WAVE output folder (Default is same as input folder).")]
            public string OutputFiles { get; set; }
            [Option('x', "extra", Required = false, HelpText = "Output extra data not included in the OSB to WAVE conversion.")]
            public bool Extra { get; set; }
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }
            [Option('s', "sample", Required = false, HelpText = "Use alternate sample rate of 32kHz (Default is 44.1kHz).")]
            public bool Sample { get; set; }

        }
        public static void Main(string[] args)
        {
            var parser = new CommandLine.Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<Options>(args);

            parserResult
              .WithParsed<Options>(opts =>
              {
                  foreach (string input in opts.InputFiles)
                  {
                      // Let's check to see if input is a file or directory
                      bool isFile = File.Exists(input), isDir = Directory.Exists(input);

                      // If it's not a file and it's not a directory, don't continue
                      if (!isFile && !isDir)
                      {
                          Console.WriteLine("Not a valid file or directory: {0}", input);
                      }
                      else if (isFile && !isDir)
                      {
                          CheckFile(in input, in opts);
                      }
                      else if (isDir && !isFile)
                      {
                          foreach (string file in Directory.GetFiles(input))
                          {
                              CheckFile(in file, in opts);
                              GC.Collect();
                          }
                      }
                      else 
                      {
                          Console.WriteLine("Input is both a file and directory? : {0}", input);
                      }
                  }
                  Console.Write("Press any key to exit...");
                  Console.ReadKey();
              })
              .WithNotParsed<Options>((errs) =>
              {
                  var helpText = HelpText.AutoBuild(parserResult, h =>
                  {
                      h.AdditionalNewLineAfterOption = false;
                      h.Copyright = "";
                      h.Heading = "OBSExtract - https://github.com/biggestsonicfan/OSBExtract";
                      return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                  }, e => e);
                  Console.WriteLine(helpText);

              });
        }

        private static string OutputDir(in string inFile, string outDir)
        {
            //Path handling nonsense goes here
            if (!String.IsNullOrWhiteSpace(outDir))
            {
                try
                {
                    string outPath = Path.GetFullPath(outDir);
                    if (!Directory.Exists(outPath))
                    {
                        Directory.CreateDirectory(outPath);
                    }
                    return outPath;
                }
                catch (Exception)
                {
                    Console.Write("Error: Issue with provided output directory: {0} - Using input file directory.", outDir);
                    return Directory.GetParent(inFile).FullName;
                }         
            }
            else
                return Directory.GetParent(inFile).FullName;
        }

        private static void CheckFile(in string inFile, in Options opts)
        {
            // Get the file name, file extension, and parent directory
            string outDir = OutputDir(in inFile, opts.OutputFiles);
            string fname = Path.GetFileName(inFile);
            string fnameWithoutExt = Path.GetFileNameWithoutExtension(inFile);
            string fext = Path.GetExtension(inFile).ToLower();

            switch (fext)
            {
                // OSB
                case ".osb":
                    {
                        Console.Write("Extracting {0}", fname);
                        OSB.Extract(0, inFile, Path.Combine(outDir, fnameWithoutExt), in opts);
                        Console.WriteLine(" ... OK");

                        break;
                    }
                // P04 (aka ADPCM aka ACIA aka Yamaha 4-bit ADPCM)
                case ".p04":
                    {
                        Console.Write("Converting {0} to WAV", fname);
                        byte[] buffer = File.ReadAllBytes(inFile);
                        //buffer = ADPCM.ToRaw(buffer, 0, buffer.Length);
                        buffer = ADPCM.adpcm2pcm(in buffer, 0, (uint)buffer.Length);
                        PCM.ToWav(in buffer, buffer.Length, Path.Combine(outDir, fnameWithoutExt + ".wav"));
                        Console.Write(" ... OK\n");
                        break;
                    }
                // 16-bit PCM
                case ".p16":
                    {
                        Console.Write("Converting {0} to WAV", fname);
                        byte[] buffer = File.ReadAllBytes(inFile);
                        PCM.ToWav(in buffer, buffer.Length, Path.Combine(outDir, fnameWithoutExt + ".wav"));
                        Console.Write(" ... OK\n");
                        break;
                    }
                // MLT
                case ".mlt":
                    {
                        Console.Write("Extracting {0}", fname);
                        MLT.Extract(inFile, Path.Combine(outDir, fnameWithoutExt), in opts);
                        Console.Write(" ... OK\n");
                        break;
                    }
                // Unknown
                default:
                    Console.WriteLine("Skipping {0}", fname);
                    break;

            }
        }
    }
}