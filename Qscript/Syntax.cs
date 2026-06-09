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
        public static int errors;

        private static void printNewLine ()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(" |  ");
            Console.ForegroundColor = ConsoleColor.White;
        }
        private static (string, int) getFileName (int pos)
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
        private static string genRedString(int count)
        {
            string resualt = "    ";
            for (int i = 0; i < count; i++)
            {
                resualt += "^";
            }
            return resualt;
        }
        public static void SyntaxError(string message = "Синтаксическая Ошибка!", CommonNode node = null)
        {
            errors++;
            (string file, int offset) = getFileName(node.token.pos);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" * Ошибка в {file}");
            Console.ForegroundColor = ConsoleColor.White;
            //Program.PrintAST(node, 0);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("=---------------------------------------------------------=");
            Console.ForegroundColor = ConsoleColor.White;
            try { printNewLine(); Console.WriteLine((node.token.pos - 1 - offset).ToString() + ": " + code.strings[file][node.token.pos - 2 - offset]); } catch { Console.WriteLine(); }
            try { printNewLine(); Console.WriteLine((node.token.pos - offset).ToString() + ": " + code.strings[file][node.token.pos - 1 - offset]); } catch { Console.WriteLine(); }
            Console.BackgroundColor = ConsoleColor.Red;
            Console.Write(" |  ");
            Console.WriteLine("" + (node.token.pos + 1 - offset).ToString() + ": " + code.strings[file][node.token.pos - offset]);       
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(genRedString(code.stringsSize[file][node.token.pos - offset] + 2 + ((node.token.pos + 1 - offset).ToString().Length)));
            Console.ResetColor();
            try { printNewLine(); Console.WriteLine((node.token.pos + 2 - offset).ToString() + ": " + code.strings[file][node.token.pos + 1 - offset]); } catch { Console.WriteLine(); }
            try { printNewLine(); Console.WriteLine((node.token.pos + 3 - offset).ToString() + ": " + code.strings[file][node.token.pos + 2 - offset]); } catch { Console.WriteLine(); }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("=---------------------------------------------------------=");
            Console.ForegroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message + " " + node.token.value);
            Console.ResetColor();
            Console.ResetColor();
#if DEBUG
            //Console.ReadLine();
#endif
            Console.BackgroundColor = ConsoleColor.Black;
            //throw new Exception("Синтаксическая Ошибка!");
        }
        public static void SyntaxError(string message = "Синтаксическая Ошибка!", Token token = null)
        {
            errors++;
            (string file, int offset) = getFileName(token.pos);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" * Ошибка в {file}");
            Console.ForegroundColor = ConsoleColor.White;
            //Program.PrintAST(node, 0);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("=---------------------------------------------------------=");
            Console.ForegroundColor = ConsoleColor.White;
            try { printNewLine(); Console.WriteLine((token.pos - 1 - offset).ToString() + ": " + code.strings[file][token.pos - 2 - offset]); } catch { Console.WriteLine(); }
            try { printNewLine(); Console.WriteLine((token.pos - offset).ToString() + ": " + code.strings[file][token.pos - 1 - offset]); } catch { Console.WriteLine(); }
            Console.BackgroundColor = ConsoleColor.Red;
            Console.Write(" |  ");
            Console.WriteLine("" + (token.pos + 1 - offset).ToString() + ": " + code.strings[file][token.pos - offset]);
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(genRedString(code.stringsSize[file][token.pos - offset] + 2 + ((token.pos + 1 - offset).ToString().Length)));
            Console.ResetColor();
            try { printNewLine(); Console.WriteLine((token.pos + 2 - offset).ToString() + ": " + code.strings[file][token.pos + 1 - offset]); } catch { Console.WriteLine(); }
            try { printNewLine(); Console.WriteLine((token.pos + 3 - offset).ToString() + ": " + code.strings[file][token.pos + 2 - offset]); } catch { Console.WriteLine(); }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("=---------------------------------------------------------=");
            Console.ForegroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message + " " + token.value);
            Console.ResetColor();
            Console.ResetColor();
#if DEBUG
            //Console.ReadLine();
#endif
            Console.BackgroundColor = ConsoleColor.Black;
            //throw new Exception("Синтаксическая Ошибка!");
        }
    }
}
