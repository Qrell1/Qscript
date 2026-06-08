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

        public static (string, int) getFileName (int pos)
        {
            int offset = 0;
            string file = string.Empty;
            foreach (var strings in code.strings)
            {
                file = strings.Key;
                if (pos == offset || pos < offset + strings.Value.Count)
                    return (file, offset);
                offset += strings.Value.Count;
            }    
            return (file, offset);
        }

        public static void SyntaxError(string message="Синтаксическая Ошибка!", int pos = 0)
        {
            (string file, int offset) = getFileName(pos);
            try
            {
                Console.WriteLine(pos.ToString() + ": " + code.strings[file][pos]);
                Console.WriteLine(message);
            }
            catch { }
#if DEBUG
            Console.ReadLine();
#endif
            //throw new Exception(message);
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
            (string file, int offset) = getFileName(node.token.pos);
            Console.ForegroundColor = ConsoleColor.White;
            Program.PrintAST(node, 0);
            try
            {
                Console.WriteLine("=---------------------------------------------------------=[]");
                try { Console.WriteLine((node.token.pos - 1 - offset).ToString() + ": " + code.strings[file][node.token.pos-2-offset]); } catch { }
                try { Console.WriteLine((node.token.pos - offset).ToString() + ": " + code.strings[file][node.token.pos-1-offset]); } catch { }
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("" + (node.token.pos + 1 - offset).ToString() + ": " + code.strings[file][node.token.pos-offset]);

                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(genRedString(code.stringsSize[file][node.token.pos - offset] + 2 + ((node.token.pos + 1 - offset).ToString().Length)));
                try { Console.WriteLine((node.token.pos + 2 - offset).ToString() + ": " + code.strings[file][node.token.pos + 1-offset]); } catch { }
                try { Console.WriteLine((node.token.pos + 3 - offset).ToString() + ": " + code.strings[file][node.token.pos + 2-offset]); } catch { }
                Console.WriteLine("=---------------------------------------------------------=");
                Console.WriteLine(message + " " + node.token.value);
            }
            catch
            {

                Console.WriteLine((node.token.pos - offset).ToString() + ": " + code.strings[file][node.token.pos - offset]);
                Console.WriteLine("=---------------------------------------------------------=");
                //Console.WriteLine(message + " " + node.token.value);
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("" + (node.token.pos + 1 - offset).ToString() + ": " + code.strings[file][node.token.pos - offset]);

                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(genRedString(code.stringsSize[file][node.token.pos - offset] + 2 + ((node.token.pos + 1 - offset).ToString().Length)));
                Console.WriteLine("=---------------------------------------------------------=");
                Console.WriteLine(message + " " + node.token.value);
            }
            Console.ResetColor();
#if DEBUG
            Console.ReadLine();
#endif
            Console.BackgroundColor = ConsoleColor.Black;
            //throw new Exception("Синтаксическая Ошибка!");
        }
        public static void SyntaxError(string message = "Синтаксическая Ошибка!", Token token = null)
        {
            (string file, int offset) = getFileName(token.pos);
            Console.ForegroundColor = ConsoleColor.White;
            try
            {
                Console.WriteLine("=---------------------------------------------------------=");
                Console.WriteLine((token.pos - 1 - offset).ToString() + ": " + code.strings[file][token.pos - 2 - offset]);
                Console.WriteLine((token.pos  - offset).ToString() + ": " + code.strings[file][token.pos - 1 - offset]);
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("" + (token.pos + 1 - offset).ToString() + ": " + code.strings[file][token.pos - offset]);

                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(genRedString(code.stringsSize[file][token.pos - offset] + 2 + ((token.pos + 1 - offset).ToString().Length)));
                Console.WriteLine((token.pos + 2 - offset).ToString() + ": " + code.strings[file][token.pos + 1 - offset]);
                Console.WriteLine((token.pos + 3 - offset).ToString() + ": " + code.strings[file][token.pos + 2 - offset]);
                Console.WriteLine("=---------------------------------------------------------=");
                Console.WriteLine(message + " " + token.value);
            }
            catch
            {
                Console.WriteLine((token.pos - offset).ToString() + ": " + code.strings[file][token.pos - offset]);
                Console.WriteLine("=---------------------------------------------------------=");
                //Console.WriteLine(message + " " + token.value);
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("" + (token.pos + 1 - offset).ToString() + ": " + code.strings[file][token.pos - offset]);

                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(genRedString(code.stringsSize[file][token.pos - offset] + 2 + ((token.pos + 1 - offset).ToString().Length)));
                Console.WriteLine("=---------------------------------------------------------=");
                Console.WriteLine(message + " " + token.value);
            }
            Console.ResetColor();
#if DEBUG
            Console.ReadLine();
#endif
            Console.BackgroundColor = ConsoleColor.Black;
            //throw new Exception("Синтаксическая Ошибка!");
        }
    }
}
