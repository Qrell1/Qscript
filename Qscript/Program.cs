using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
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
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            //string code = "asm format mov eax, ebx cmp eax, ebx je true asm
            Console.WriteLine("Write File: ");
            Console.WriteLine("Auto Run!");
            //string filename = Console.ReadLine();
            string filename;
            string[] codes;
            string pathCompile;
            

            TypeApp typeApp = TypeApp.program;
            ModeApp modeApp = ModeApp.release;
            ArchApp archApp = ArchApp.x86_32;
            FormatApp formatApp = FormatApp.windows;

            if (args.Length >= 2)
            {
                codes = File.ReadAllLines(args[0]);
                FileStream fileStream = File.OpenRead(args[0]);
                string[] refs = args[0].Split('\\');
                filename = refs[refs.Length-1].Split('.')[0];
                pathCompile = args[0].Replace(refs[refs.Length-1], "");
                Console.WriteLine(pathCompile);
                Console.WriteLine(filename);
                //Console.WriteLine("args[1] = " + args[1]);
                //Console.Read();
                fileStream.Close();

                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i].StartsWith("-type"))
                    {
                        string _typeApp = args[i].Split('=').Last().ToLower();
                        switch (_typeApp)
                        {
                            case "dll": typeApp = TypeApp.dll; break;
                            case "program": typeApp = TypeApp.program; break;
                            case "asmmodule": typeApp = TypeApp.asmmodule; break;
                            case "h": typeApp = TypeApp.h; break;
                            case "gui": typeApp = TypeApp.gui; break;
                            case "bin": typeApp = TypeApp.bin; break;
                        }
                    }
                    else if (args[i].StartsWith("-arch"))
                    {
                        string _archApp = args[i].Split('=').Last().ToLower();
                        switch (_archApp)
                        {
                            case "x86_32": archApp = ArchApp.x86_32; break;
                            case "x86_64": archApp = ArchApp.x86_64; break;
                            case "arm_32": archApp = ArchApp.arm_32; break;
                            case "arm_64": archApp = ArchApp.arm_64; break;
                            case "riscv_32": archApp = ArchApp.riscv_32; break;
                            case "riscv_64": archApp = ArchApp.riscv_64; break;
                        }
                    }
                    else if (args[i].StartsWith("-mode"))
                    {
                        string _modeApp = args[i].Split('=').Last().ToLower();
                        if (_modeApp == "debug") modeApp = ModeApp.debug;
                        else modeApp = ModeApp.release;
                    }
                    else if (args[i].StartsWith("-format"))
                    {
                        string _formatApp = args[i].Split('=').Last().ToLower();
                        switch (_formatApp)
                        {
                            case "windows": formatApp = FormatApp.windows; break;
                            case "linux": formatApp = FormatApp.linux; break;
                        }
                    }
                }
            }
            else
            {
                pathCompile = AppDomain.CurrentDomain.BaseDirectory + "\\compile\\";
                filename = "cm";
                codes = File.ReadAllLines("codes\\" + filename + ".qs");
            }
            if (archApp == ArchApp.x86_64
                || archApp == ArchApp.arm_64
                || archApp == ArchApp.riscv_64)
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

            string code = commentLexer.lexCodes(codes, filename);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Start Lexer...");
            lexer = new Lexer(Syntax.code, filename, 0);
            List<Token> list = lexer.lexAnalysis();
            Console.ResetColor();
            preproccessor.offset += Syntax.code.strings.Last().Value.Count;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Start PreproccessorIncludes...");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            list = preproccessor.lexIncludes(list);
            Console.ResetColor();


            //int index = 0
            ProgramNode ast;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Start Parser...");
            Console.ResetColor();
            try
            {
                parser = new Parser(list);
                ast = parser.parseCode();
            }
            catch (Exception e) { Console.WriteLine($"При Парсинге что-то пошло не так...(\n{e.Message}\n{e.StackTrace}"); Console.ReadKey(); return; }
            //PrintAST(ast, 0);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Start PostParser...");
            Console.ResetColor();
            addParser = new AbbreviationParser();
            try { ast = addParser.abbParse(ast); }
            catch (Exception e) { Console.WriteLine($"При пост-парсинге что-то пошло не так...(\n{e.Message}\n{e.StackTrace}"); Console.ReadKey(); return; }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Start SemanticAnalyser...");
            Console.ResetColor();
            try { SemanticAnalyzer.startAnalis(ast); }
            catch (Exception e) { Console.WriteLine($"При Симантическом Анализе что-то пошло не так...(\n{e.Message}\n{e.StackTrace}"); Console.ReadKey(); return; }

            if (Syntax.errors > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Было найдено: {Syntax.errors} ошибок!");
                Console.ResetColor(); Console.Write("Продолжить компиляцию? (y/n || д/н): ");
                string read = Console.ReadLine();
                read = read.Trim().ToLower();
                if (read[0] == 'n' || read[0] == 'н') return;
            }
            //Console.WriteLine("NEW AST AbbreviationParser!!!");
            //if (args.Length == 0)
            //PrintAST(ast, 0);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Start Compiler...");
            Console.ResetColor();
            compiler = new Compiler("dsd", ast);
            try { compiler.Translation(ast, 0); }
            catch (Exception e) { PrintAST(ast, 0); Console.WriteLine($"При Компиляции что-то пошло не так...(\n{e.Message}\n{e.StackTrace}"); Console.ReadKey(); return; }
            string data = compiler.ConcatData(typeApp, modeApp, archApp, formatApp);
            compiler.WriteCode(data, filename, pathCompile, "qsr");

            Console.WriteLine("End...");
            //Console.ReadKey();
        }
    }
}
