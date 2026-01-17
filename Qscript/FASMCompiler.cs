using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
/*
namespace Qscript
{
    public class FASMCompiler
    {
        private readonly StringBuilder _irCode;
        private int _tempCounter;
        private readonly Dictionary<string, string> _variables;
        private readonly Dictionary<string, int> _stringConstants;
        private int _stringCounter;

        public FASMCompiler()
        {
            _irCode = new StringBuilder();
            _variables = new Dictionary<string, string>();
            _stringConstants = new Dictionary<string, int>();
            _tempCounter = 0;
            _stringCounter = 0;
        }

        public string Compile(Node root)
        {
            GenerateHeader();

            if (root is ProgramNode statements)
            {
                foreach (var node in statements.getNodes())
                {
                    CompileNode(node);
                }
            }
            else
            {
                CompileNode(root);
            }

            GenerateFooter();
            return _irCode.ToString();
        }

        private void GenerateHeader()
        {
            _irCode.AppendLine("format PE console");

            _irCode.AppendLine("entry start");

            _irCode.AppendLine("include 'win32a.inc'");

            _irCode.AppendLine("section '.data' data readable writable");
            _irCode.AppendLine("        varFormat db '%d', 0");
        }

        private void GenerateFooter()
        {
            _irCode.AppendLine("  ret i32 0");
            _irCode.AppendLine("}");

            // Добавляем строковые константы если есть
            if (_stringConstants.Count > 0)
            {
                _irCode.AppendLine();
                _irCode.AppendLine("; String constants");
                foreach (var strConst in _stringConstants)
                {
                    _irCode.AppendLine($"@.str.{strConst.Value} = private unnamed_addr constant [{strConst.Key.Length + 1} x i8] c\"{EscapeString(strConst.Key)}\\00\"");
                }
            }

            // Добавляем формат для вывода чисел
            _irCode.AppendLine();
            _irCode.AppendLine("@.str.int = private unnamed_addr constant [4 x i8] c\"%d\\0A\\00\"");
        }

        private string EscapeString(string str)
        {
            return str.Replace("\\", "\\\\").Replace("\"", "\\22").Replace("\n", "\\0A");
        }

        private void CompileNode(Instruct node)
        {
            switch (node)
            {
                case ProgramNode statementsNode:
                    CompileStatements(statementsNode);
                    break;

                case BinOpNode binOpNode:
                    CompileBinOp(binOpNode);
                    break;

                case UnarOpNode unarOpNode:
                    CompileUnarOp(unarOpNode);
                    break;

                case NumberNode numberNode:
                    // Числа компилируются в выражениях
                    break;

                case VarNode varNode:
                    // Переменные компилируются в выражениях
                    break;

                default:
                    throw new Exception($"Unknown node type: {node.GetType().Name}");
            }
        }

        private void CompileStatements(ProgramNode node)
        {
            foreach (var childNode in node.getInstructs())
            {
                CompileNode(childNode);
            }
        }

        private void CompileBinOp(BinOpNode node)
        {
            if (node.op.type.type == "ASSIGN")
            {
                // Обработка присваивания
                if (node.left is VarNode varNode)
                {
                    var rightResult = CompileExpression(node.right);
                    _variables[varNode.token.value] = rightResult;

                    // Объявляем переменную если еще не объявлена
                    if (!_variables.ContainsKey(varNode.token.value))
                    {
                        _irCode.AppendLine($"  %{varNode.token.value} = alloca i32");
                    }

                    _irCode.AppendLine($"  store i32 {rightResult}, i32* %{varNode.token.value}");
                    return;
                }
                throw new Exception("Left side of assignment must be a variable");
            }
            else
            {
                // Обработка бинарных операций
                var leftResult = CompileExpression(node.left);
                var rightResult = CompileExpression(node.right);

                string instruction;

                switch (node.op.type.type)
                {
                    case "PLUS":
                        instruction = "add";
                        break;

                    case "MINUS":
                        instruction = "sub";
                        break;

                    default:
                        throw new Exception($"Unknown binary operator: {node.op.type.type}");
                }

                var resultVar = $"%temp.{_tempCounter++}";
                _irCode.AppendLine($"  {resultVar} = {instruction} i32 {leftResult}, {rightResult}");
            }
        }

        private void CompileUnarOp(UnarOpNode node)
        {
            if (node.op.type.type == "OUT")
            {
                CompilePrint(node.operand);
            }
            else
            {
                throw new Exception($"Unknown unary operator: {node.op.type.type}");
            }
        }

        private void CompilePrint(Node node)
        {
            var value = CompileExpression(node);

            // Вывод числа
            var formatPtr = $"%format.{_tempCounter++}";
            _irCode.AppendLine($"  {formatPtr} = getelementptr inbounds [4 x i8], [4 x i8]* @.str.int, i64 0, i64 0");
            _irCode.AppendLine($"  call i32 (i8*, ...) @printf(i8* {formatPtr}, i32 {value})");
        }

        private string CompileExpression(Node node)
        {
            switch (node)
            {
                case NumberNode numberNode:
                    return numberNode.token.value;

                case VarNode varNode:
                    if (_variables.ContainsKey(varNode.token.value))
                    {
                        var tempVar = $"%temp.load.{_tempCounter++}";
                        _irCode.AppendLine($"  {tempVar} = load i32, i32* %{varNode.token.value}");
                        return tempVar;
                    }
                    throw new Exception($"Variable '{varNode.token.value}' not initialized at position {varNode.token.pos}");

                case BinOpNode binOpNode:
                    if (binOpNode.op.type.type != "ASSIGN") // Исключаем присваивание
                    {
                        return CompileBinaryExpression(binOpNode);
                    }
                    throw new Exception("Assignment not allowed in expression context");

                default:
                    throw new Exception($"Cannot compile expression of type: {node.GetType().Name}");
            }
        }

        private string CompileBinaryExpression(BinOpNode node)
        {
            var leftResult = CompileExpression(node.left);
            var rightResult = CompileExpression(node.right);

            string instruction;

            switch (node.op.type.type)
            {
                case "PLUS":
                    instruction = "add";
                    break;

                case "MINUS":
                    instruction = "sub";
                    break;

                default:
                    throw new Exception($"Unknown binary operator: {node.op.type.type} at position {node.op.pos}");
            }

            var resultVar = $"%temp.{_tempCounter++}";
            _irCode.AppendLine($"  {resultVar} = {instruction} i32 {leftResult}, {rightResult}");
            return resultVar;
        }
    }
}*/
