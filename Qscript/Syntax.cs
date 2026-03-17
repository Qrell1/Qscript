using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Qscript
{
    public static class Syntax
    {
        public static CodeStruct code = new CodeStruct();

        public static void SyntaxError(string message="Синтаксическая Ошибка!", int pos = 0)
        {
            try
            {
                Console.WriteLine(pos.ToString() + ": " + code.strings[pos]);
                Console.WriteLine(message);
            }
            catch { }
            throw new Exception(message);
        }
        public static string genRedString(int count)
        {
            string resualt = string.Empty;
            for (int i = 0; i < count; i++)
            {
                resualt += "^";
            }
            return resualt;
        }
        public static void SyntaxError(string message="Синтаксическая Ошибка!", CommonNode node=null)
        {
            Console.ForegroundColor = ConsoleColor.White;
            try
            {
                Console.WriteLine("=---------------------------------------------------------=");
                Console.WriteLine((node.token.pos-3).ToString() + ": " + code.strings[node.token.pos-2]);
                Console.WriteLine((node.token.pos-2).ToString() + ": " + code.strings[node.token.pos-1]);
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("" + (node.token.pos+1).ToString() + ": " + code.strings[node.token.pos]);
                
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(genRedString(code.stringsSize[node.token.pos] + 2 + ((node.token.pos + 1).ToString().Length)));
                Console.WriteLine((node.token.pos+2).ToString() + ": " + code.strings[node.token.pos+1]);
                Console.WriteLine((node.token.pos+3).ToString() + ": " + code.strings[node.token.pos+2]);
                Console.WriteLine("=---------------------------------------------------------=");
                Console.WriteLine(message);
            }
            catch
            {
                try
                {
                    Console.WriteLine(node.token.pos.ToString() + ": " + code.strings[node.token.pos]);
                    Console.WriteLine("=---------------------------------------------------------=");
                    Console.WriteLine(message);
                }
                catch
                {
                    Console.WriteLine("=---------------------------------------------------------=");
                    Console.WriteLine(message);
                }
            }
            Console.ResetColor();
#if DEBUG
            Console.ReadLine();
#endif
            //throw new Exception("Синтаксическая Ошибка!");
        }
        public static void SyntaxError(string message = "Синтаксическая Ошибка!", Token token = null)
        {
            Console.ForegroundColor = ConsoleColor.White;
            try
            {
                Console.WriteLine("=---------------------------------------------------------=");
                Console.WriteLine((token.pos - 3).ToString() + ": " + code.strings[token.pos - 2]);
                Console.WriteLine((token.pos - 2).ToString() + ": " + code.strings[token.pos - 1]);
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("" + (token.pos + 1).ToString() + ": " + code.strings[token.pos]);
                
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(genRedString(code.stringsSize[token.pos] + 2 + ((token.pos + 1).ToString().Length)));
                Console.WriteLine((token.pos + 2).ToString() + ": " + code.strings[token.pos + 1]);
                Console.WriteLine((token.pos + 3).ToString() + ": " + code.strings[token.pos + 2]);
                Console.WriteLine("=---------------------------------------------------------=");
                Console.WriteLine(message);
            }
            catch
            {
                try
                {
                    Console.WriteLine(token.pos.ToString() + ": " + code.strings[token.pos]);
                    Console.WriteLine("=---------------------------------------------------------=");
                    Console.WriteLine(message);
                }
                catch
                {
                    Console.WriteLine("=---------------------------------------------------------=");
                    Console.WriteLine(message);
                }
            }
            Console.ResetColor();
#if DEBUG
            Console.ReadLine();
#endif
            //throw new Exception("Синтаксическая Ошибка!");
        }
    }
}
