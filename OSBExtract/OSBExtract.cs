using System;
using System.IO;

namespace OSBExtract
{
    public class OSBExtract
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("OSBExtract");

            // If we have an argument, clearly we want to do something.
            if (args.Length == 1)
            {
                bool isFile = false, isDir = false;

                // Let's check to see if args[0] is a file or directory
                if (File.Exists(args[0])) isFile = true;
                if (Directory.Exists(args[0])) isDir = true;

                // If it's not a file and it's not a directory, don't continue
                if (!isFile && !isDir)
                {
                    Console.WriteLine("I don't know what you want to do!");
                    return;
                }

                // Now we can convert or extract this file or files in this directory
                if (isFile)
                {
                    CheckFile(args[0]);
                }
                else if (isDir)
                {
                    foreach (string file in Directory.GetFiles(args[0]))
                    {
                        CheckFile(file);
                        GC.Collect();
                    }
                }
            }
            else
            {
                Console.WriteLine("Usage: OSBExtract <file or directory>");
            }
        }

        private static void CheckFile(string inFile)
        {
            // Get the file name, file extension, and parent directory
            string dir = Directory.GetParent(inFile).FullName;
            string fname = Path.GetFileName(inFile);
            string fnameWithoutExt = Path.GetFileNameWithoutExtension(inFile);
            string fext = Path.GetExtension(inFile).ToLower();

            // OSB
            if (fext == ".osb")
            {
                Console.Write("Extracting {0}", fname);

                OSB2.Extract(0, inFile, Path.Combine(dir, fnameWithoutExt));

                Console.WriteLine(" ... OK");
            }

            // P04 (aka ADPCM aka ACIA aka Yamaha 4-bit ADPCM)
            else if (fext == ".p04")
            {
                Console.Write("Converting {0} to WAV", fname);

                byte[] buffer = File.ReadAllBytes(inFile);
                buffer = ADPCM.ToRaw(buffer, 0, buffer.Length);
                PCM.ToWav(buffer, buffer.Length, Path.Combine(dir, fnameWithoutExt + ".wav"));

                Console.WriteLine(" ... OK");
             
            }

            // 16-bit PCM
            else if (fext == ".p16")
            {
                Console.Write("Converting {0} to WAV", fname);

                byte[] buffer = File.ReadAllBytes(inFile);
                PCM.ToWav(buffer, buffer.Length, Path.Combine(dir, fnameWithoutExt + ".wav"));

                Console.WriteLine(" ... OK");
            }

            // MLT
            else if (fext == ".mlt")
            {
                Console.Write("Extracting {0}", fname);

                MLT.Extract(inFile, Path.Combine(dir, fnameWithoutExt));

                Console.WriteLine(" ... OK");
            }

            // Unknown
            else
            {
                Console.WriteLine("Skipping {0}", fname);
            }
        }
    }
}