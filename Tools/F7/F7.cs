using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DuneEdit;

namespace F7
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Usage();
                return;
            }

            bool compress = false;
            switch (args[0])
            {
                case "-c":
                    compress = true;
                    break;
                case "-d":
                    compress = false;
                    break;
                default:
                    Usage();
                    return;
            }

            string filename = args[1];

            byte[] input;
            using (var f = File.OpenRead(filename))
            {
                input = new byte[f.Length];
                f.Read(input, 0, (int)f.Length);
            }

            byte[] output = compress ?
                SavegameCompression.Compress(input) :
                SavegameCompression.Decompress(input);

            using (var f = File.OpenWrite(filename))
                f.Write(output, 0, output.Length);
        }

        private static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("Compress:   f7 -c filename.sav");
            Console.WriteLine("Decompress: f7 -d filename.sav");
        }
    }
}
