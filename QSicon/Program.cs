using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QSicon
{
    internal class Program
    {
        private static int stringCount;

        static string genSpace (int z)
        {
            string str = string.Empty;
            for (int i = 0; i < z; i++)
            {
                str += "~";
            }
            return str;
        }
        static void Main(string[] args)
        {
            Console.Title = "Qscript Watching";

            string[] codes = File.ReadAllLines(args[0]);
            stringCount = codes.Length.ToString().Length;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.Write(genSpace(stringCount));
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine($"Qscript file{args[0]}");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            //Console.ReadKey();
            for (int i = 0; i < codes.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(i);
                Console.Write($"{genSpace(stringCount - i.ToString().Length)}");
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.Write("|");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                //codes[i] = codes[i].Replace("\t", "~");
                Console.WriteLine(codes[i]);
            }
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ReadLine();
        }
    }
}
