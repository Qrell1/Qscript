using Qscript.Lex;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QSicon
{
    class QscriptConfig
    {
        public string CompilerExe;
        public string Version;
    }
    internal class Program
    {
        private static int stringCount;
        private static int ypos;
        private static string endstr;
        private static string[] str;

        static string genSpace (int z)
        {
            string str = string.Empty;
            for (int i = 0; i < z; i++)
            {
                str += "~";
            }
            return str;
        }
        static List<string> concat(string[] code)
        {
            List<string> cds = new List<string>();
            string str = string.Empty;
            for (int i = 0; i < code.Length; i++)//Console.WindowHeight - 1; i++)
            {
                if (i <= code.Length)
                {
                    str = string.Empty;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    str += i.ToString();
                    str += genSpace(stringCount - i.ToString().Length);
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    str += "|";
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.White;
                    //cds.Add(code[i + ypos] + "\n");
                    str += code[i+ypos]+"\n";
                    cds.Add(str);
                }
                else
                    break;
            }
            return cds;
        }
        static void Main(string[] args)
        {
            Console.Title = "Qscript Watching";

            string[] codes = File.ReadAllLines(args[0]);
            stringCount = codes.Length.ToString().Length;
            Lexer lexer = new Lexer();
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine($"Qscript file{args[0]}");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;


            str = concat(codes).ToArray();

            lexer.lexAnalysis(str);
            /*while (true)
            {
                Console.Clear();
                lexer.lexAnalysis(str);
                Thread.Sleep(1000);
            }*/

            string command = Console.ReadLine();
            string[] _args = command.Split(' ');
            if (_args[0] == "compile")
            {
                string config = File.ReadAllText("C:\\ProgramData\\Qscript\\config.json");
                QscriptConfig qscriptConfig = JsonConvert.DeserializeObject<QscriptConfig>(config);

                var proc = new Process();
                proc.StartInfo.FileName = qscriptConfig.CompilerExe;
                proc.StartInfo.Arguments = $"\"{args[0]}\" {_args[1]}";
                proc.Start();

                proc.WaitForExit();
                proc.Close();
            }
        }
    }
}
