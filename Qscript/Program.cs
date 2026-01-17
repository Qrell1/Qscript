using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.Server;
using Qscript.Lex;

namespace Qscript
{
    internal class Program
    {
        public static Lexer lexer;
        public static CommentLexer commentLexer = new CommentLexer();
        public static IncludeLexer includeLexer = new IncludeLexer();
        public static Parser parser;

        static void PrintListToken (List<Token> list)
        {
            foreach (Token token in list)
            {
                string spaces = "";
                for (int i = 0; i < 16 - token.type.type.Length; i++)
                {
                    spaces += " ";
                }
                Console.WriteLine($"[DEBUG] Токен позиция:{token.pos} тип:{token.type.type}" + spaces + $"значение:{token.value}");
            }
        }

        static string GenSpaces (int count)
        {
            return new string(' ', count);
        }

        static void PrintAST(CommonNode root, int z_buffer)
        {
            ConsoleColor color;
            if (root.type == "ROOT")
                color = ConsoleColor.Yellow;
            else if (root.type == "VAR" || root.type == "REFVAR")
                color = ConsoleColor.Green;
            else if (root.type == "CALL")
                color = ConsoleColor.Yellow;
            else if (root.type == "NUMBER")
                color = ConsoleColor.Cyan;
            else if (root.type == "BINOPER" || root.type == "CMP")
                color = ConsoleColor.Magenta;
            else if (root.type == "TYPE")
                color = ConsoleColor.Blue;
            else if (root.type == "SIGNATURE" || root.type == "BODY")
                color = ConsoleColor.Yellow;
            else if (root.type == "ASM")
                color = ConsoleColor.DarkRed;
            else
                color = ConsoleColor.White;
            //Console.Write("[DEBUG]--PrintAST>");
            Console.ForegroundColor = color;
            Console.WriteLine($"[DEBUG]--PrintAST>{GenSpaces(z_buffer)} |Тип:{root.type} [{root.token.value}] Дочерних узлов:{root.childs.Count}");
            Console.ResetColor();
            for (int i = 0; i < root.childs.Count; i++)
            {
                PrintAST(root.childs[i], z_buffer + 2);
            }
        }
        static void PrintAST2(CommonNode root, int depth = 0)
        {
            // Определяем цвет узла
            ConsoleColor color;
            switch(root.type)
            {
                case "ROOT":
                    color = ConsoleColor.Yellow;
                    break;
                case "VAR":
                    color = ConsoleColor.Green;
                    break;
                case "NUMBER":
                    color = ConsoleColor.Cyan;
                    break;
                case "BINOPER":
                    color = ConsoleColor.Magenta;
                    break;
                case "TYPE":
                    color = ConsoleColor.Blue;
                    break;
                default:
                    color = ConsoleColor.White;
                    break;
            };

            // Формируем содержимое
            string content = root.type;
            if (root.token != null && !string.IsNullOrEmpty(root.token.value))
            {
                content += $": {root.token.value}";
            }
            if (root.childs.Count > 0)
            {
                content += $" [{root.childs.Count}]";
            }

            // Рисуем рамку
            string indent = new string(' ', depth * 3);
            string boxTop = indent + "┌" + new string('─', content.Length + 2) + "┐";
            string boxMiddle = indent + "│ " + content.PadRight(content.Length + 1) + " │";
            string boxBottom = indent + "└" + new string('─', content.Length + 2) + "┘";

            Console.ForegroundColor = color;
            Console.WriteLine(boxTop);
            Console.WriteLine(boxMiddle);
            Console.WriteLine(boxBottom);
            Console.ResetColor();

            // Рекурсивно обходим детей
            foreach (var child in root.childs)
            {
                PrintAST2(child, depth + 1);
            }
        }

        static void Main(string[] args)
        {

            //string code = "asm format mov eax, ebx cmp eax, ebx je true asm
            Console.WriteLine("Write File: ");
            Console.WriteLine("Auto Run!");
            //string filename = Console.ReadLine();
            string filename = "compile.qs";
            string[] codes = File.ReadAllLines("codes\\" + filename);
            


            string code = commentLexer.lexCodes(codes);

            lexer = new Lexer(code);
            //stringLexer = new StringLexer(lexer.lexAnalysis());
            //List<List<Token>> tokens =  stringLexer.LexStrings();
            //tokens = tokens;
            List<Token> list = lexer.lexAnalysis();
            List<Token> list2 = includeLexer.lexIncludes(list);
            list = includeLexer.destroyIncludes(list);
            list2.AddRange(list);

            List<Token> forType = new List<Token>();
            List<Token> forVar = new List<Token>();

            int index = 0;
            /*foreach (Token token in list2)
            {
                string spaces = "";
                for (int i = 0; i < 16 - token.type.type.Length; i++)
                {
                    spaces += " ";
                }
                if (token.type.type == "TYPE")
                    forType.Add(token);
                else if (token.type.type == "VAR")
                    forVar.Add(token);
                Console.WriteLine($"[DEBUG] Токен позиция:{token.pos} в списке:{index} тип:{token.type.type}" + spaces + $"значение:{token.value}");
                index++;
            }*/

            //Console.WriteLine("[DEBUG] -- Debuging TYPE --");
            //PrintListToken(forType);
            //Console.WriteLine("[DEBUG] -- Debuging VAR --");
            //PrintListToken(forVar);

            parser = new Parser(list);
            ProgramNode ast = parser.parseCode();

            PrintAST(ast, 0);

            Compiler compiler = new Compiler("dsd");
            compiler.Translation(ast, 0);
            Console.WriteLine("[DEBUG] SECTION DATA");
            Console.WriteLine(compiler._objProg.data.ToString());
            Console.WriteLine("[DEBUG] SECTION CODE");
            Console.WriteLine(compiler._objProg.code.ToString());

            //var compiler = new FASMCompiler();
            //var asm = compiler.Compile(ast);
            //Console.WriteLine(asm.ToString());
            //FASMUntils.CompileAndRun(asm);
            Console.WriteLine("End...");
            //parser = new Parser(lexer.lexAnalysis());
            //var rootNode = parser.parseCode();
            //parser.run(rootNode);
        }
    }
}
