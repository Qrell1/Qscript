using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Qscript
{
    public static class Error
    {
        public static List<int> codeLenght = new List<int>();
        public static List<string> code = new List<string>();
        public static void SyntaxError(string message="Синтаксическая Ошибка!", int pos = 0)
        {
            try
            {
                Console.WriteLine(pos.ToString() + ": " + code[pos]);
                Console.WriteLine(message);
            }
            catch { }
            throw new Exception(message);
        }
        public static void SyntaxError(string message="Синтаксическая Ошибка!", CommonNode node=null)
        {
            try
            {
                Console.WriteLine("=---------------------------------------------------------=");
                Console.WriteLine((node.token.pos-2).ToString() + ": " + code[node.token.pos-2-1]);
                Console.WriteLine((node.token.pos-1).ToString() + ": " + code[node.token.pos-1-1]);

                Console.WriteLine("" + (node.token.pos).ToString() + ": " + code[node.token.pos-1]);

                Console.WriteLine((node.token.pos+1).ToString() + ": " + code[node.token.pos+1-1]);
                Console.WriteLine((node.token.pos+2).ToString() + ": " + code[node.token.pos+2-1]);
                Console.WriteLine("=---------------------------------------------------------=");
                Console.WriteLine(message);
            }
            catch
            {
                try
                {
                    Console.WriteLine(node.token.pos.ToString() + ": " + code[node.token.pos]);
                    Console.WriteLine("=---------------------------------------------------------=");
                    Console.WriteLine(message);
                }
                catch
                {
                    Console.WriteLine("=---------------------------------------------------------=");
                    Console.WriteLine(message);
                }
            }
            throw new Exception("Синтаксическая Ошибка!");
        }
    }
}
