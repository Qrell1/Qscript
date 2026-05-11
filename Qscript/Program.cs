using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Server;
using Qscript.Lex;

namespace Qscript
{
    internal class Program
    {
        public static Lexer lexer;
        public static CommentLexer commentLexer = new CommentLexer();
        public static Preproccessor preproccessor = new Preproccessor();
        public static Parser parser;
        public static Compiler compiler;
        public static AbbreviationParser addParser;

        static void PrintListToken (List<Token> list)
        {
            foreach (Token token in list)
            {
                string spaces = "";
                for (int i = 0; i < 16 - token.type.ToString().Length; i++)
                {
                    spaces += " ";
                }
                Console.WriteLine($"[DEBUG] Токен позиция:{token.pos} тип:{token.type}" + spaces + $"значение:{token.value}");
            }
        }

        static string GenSpaces (int count)
        {
            return new string(' ', count);
        }

        public static void PrintAST(CommonNode root, int z_buffer)
        {
            ConsoleColor color;
            if (root.type == NT.ROOT)
                color = ConsoleColor.Yellow;
            else if (root.type == NT.VAR || root.type == NT.REFVAR)
                color = ConsoleColor.Green;
            else if (root.type == NT.CALL)
                color = ConsoleColor.Yellow;
            else if (root.type == NT.NUMBER || root.type == NT.FLOAT || root.type == NT.BOOL)
                color = ConsoleColor.Cyan;
            else if (root.type == NT.BINOPER || root.type == NT.CMP || root.type == NT.FLOATBINOPER || root.type == NT.ADDRESS)
                color = ConsoleColor.Magenta;
            else if (root.type == NT.TYPE || root.type == NT.SIZEOF)
                color = ConsoleColor.Blue;
            else if (root.type == NT.SIGNATURE || root.type == NT.BODY )//|| root.type == "FUNC")
                color = ConsoleColor.Yellow;
            else if (root.type == NT.ASM || root.type == NT.FUNC)
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
                case NT.ROOT:
                    color = ConsoleColor.Yellow;
                    break;
                case NT.VAR:
                    color = ConsoleColor.Green;
                    break;
                case NT.NUMBER:
                    color = ConsoleColor.Cyan;
                    break;
                case NT.BINOPER:
                    color = ConsoleColor.Magenta;
                    break;
                case NT.TYPE:
                    color = ConsoleColor.Blue;
                    break;
                default:
                    color = ConsoleColor.White;
                    break;
            };

            // Формируем содержимое
            string content = root.type.ToString();
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
            string filename;
            string[] codes;
            string pathCompile;
            TypeApp typeApp = TypeApp.program32;

            if (args.Length >= 2)
            {
                codes = File.ReadAllLines(args[0]);
                FileStream fileStream = File.OpenRead(args[0]);
                string[] refs = args[0].Split('\\');
                filename = refs[refs.Length-1].Split('.')[0];
                pathCompile = args[0].Replace(refs[refs.Length-1], "");
                Console.WriteLine(pathCompile);
                Console.WriteLine(filename);
                Console.WriteLine("args[1] = " + args[1]);
                //Console.Read();
                fileStream.Close();
                if (args[1] == "-asm")
                    typeApp = TypeApp.asmmodule;
                if (args[1] == "-gui")
                    typeApp = TypeApp.gui;
                else if (args[1] == "-dll")
                    typeApp = TypeApp.dll;
                else if (args[1] == "-program32")
                    typeApp = TypeApp.program32;
                else if (args[1] == "-program64")
                    typeApp = TypeApp.program64;
                else if (args[1] == "-bin")
                    typeApp = TypeApp.bin;
            }
            else
            {
                pathCompile = AppDomain.CurrentDomain.BaseDirectory + "\\compile\\";
                filename = "cm";
                codes = File.ReadAllLines("codes\\" + filename + ".qs");
            }
            if (typeApp == TypeApp.program64)
            {
                DataBase.types["long"] = "dq";
                DataBase.typesarg["long"] = "QWORD";
                DataBase.aligns["long"] = 8;
                DataBase.typesregs["long"] = "rax";

                DataBase.types["half"] = "dd";
                DataBase.typesarg["half"] = "DWORD";
                DataBase.aligns["half"] = 4;
                DataBase.typesregs["half"] = "eax";
            }

            (string code, CodeStruct codeStruct) = commentLexer.lexCodes(codes);
            Syntax.code = codeStruct;

            Console.WriteLine("Start Lexer...");
            lexer = new Lexer();
            List<Token> list = lexer.lexAnalysis();
            Console.WriteLine("Start PreproccessorIncludes...");
            list = preproccessor.lexIncludes(list);


            //int index = 0
            ProgramNode ast;
            Console.WriteLine("Start Parser...");
            try
            {
                parser = new Parser(list);
                ast = parser.parseCode();
            }
            catch (Exception e) { Console.WriteLine($"При Парсинге что-то пошло не так...(\n{e.Message}\n{e.StackTrace}"); Console.ReadKey(); return; }
            //PrintAST(ast, 0);

            Console.WriteLine("Start PostParser...");
            addParser = new AbbreviationParser();
            try { ast = addParser.abbParse(ast); }
            catch (Exception e) { Console.WriteLine($"При пост-парсинге что-то пошло не так...(\n{e.Message}\n{e.StackTrace}"); Console.ReadKey(); return; }
            Console.WriteLine("Start SemanticAnalyser...");
            try { SemanticAnalyzer.startAnalis(ast); }
            catch (Exception e) { Console.WriteLine($"При Симантическом Анализе что-то пошло не так...(\n{e.Message}\n{e.StackTrace}"); Console.ReadKey(); return; }


            //Console.WriteLine("NEW AST AbbreviationParser!!!");
            //if (args.Length == 0)
            //    PrintAST(ast, 0);


            Console.WriteLine("Start Compiler...");
            compiler = new Compiler("dsd", ast);
            try { compiler.Translation(ast, 0); }
            catch (Exception e) { PrintAST(ast, 0); Console.WriteLine($"При Компиляции что-то пошло не так...(\n{e.Message}\n{e.StackTrace}"); Console.ReadKey(); return; }
            string data = compiler.ConcatData(typeApp);
            compiler.WriteCode(data, filename, pathCompile, "qsr");

            Console.WriteLine("End...");
            Console.ReadKey();

        }
    }
}
