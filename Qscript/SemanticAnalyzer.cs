using Qscript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qscript
{
    public static class SemanticAnalyzer
    {
        static public ProgramNode ast;
        //static SemanticAnalyzer() { }

        static Dictionary<string, string> types = new Dictionary<string, string>()
        {
            {"int32", "NUMBER"},
            {"int16", "NUMBER"},
            {"int8", "NUMBER"},
            {"string", "STRING"},
            {"char", "CHAR"},
            {"bool", "BOOL"},
            {"float", "FLOAT"}
        };

        static Dictionary<string, CommonNode> varTypes = new Dictionary<string, CommonNode>();

        public static CommonNode take(CommonNode node, int i = 0)
        {
            if (node.childs.Count >= i + 1)
            {
                return node.childs[i];
            }
            return null;
        }

        public static void startAnalis(ProgramNode root)
        {
            ast = root;
            analis(root, 0);
        }

        public static void analis(CommonNode root, int z_buffer)
        {
            switch (root.type)
            {
                case "BINOPER":
                    CommonNode leftNode = take(root, 0);
                    CommonNode rightNode = take(root, 1);

                    if (leftNode.type == "VAR" && leftNode.childs.Count == 1)
                    {
                        if (varTypes.Keys.Contains(root.token.value))
                            Syntax.SyntaxError($"Нельзя объявлять две переменных с одним именем!", root);
                        CommonNode type = leftNode.childs[0];
                        varTypes.Add(leftNode.token.value, type);
                    }

                    if (leftNode.type == "VAR" && rightNode.type == "CALL")
                    {
                        if (!ast.resualtFunc.ContainsKey(rightNode.token.value))
                            Syntax.SyntaxError($"Ошибка Функция:{rightNode.token.value} не существует чтобы её вызывать!", rightNode);
                        if (!varTypes.ContainsKey(leftNode.token.value))
                            Syntax.SyntaxError($"Ошибка Переменной:{leftNode.token.value} не существует!", leftNode);
                        if (ast.resualtFunc[rightNode.token.value] == null)
                            Syntax.SyntaxError($"Ошибка Функция:{rightNode.token.value} не может возвращать в перменную значения типа void!", rightNode);
                        CommonNode typeResualt = ast.resualtFunc[rightNode.token.value];
                        CommonNode type = varTypes[leftNode.token.value];
                        if (type.token.value != typeResualt.token.value)
                            Syntax.SyntaxError($"Ошибка Функция:{rightNode.token.value} не может возвращать значение в Переменную:{leftNode.token.value} другого типа!", rightNode);
                    }

                    if (leftNode.type == "VAR")
                    {
                        if ((!varTypes.Keys.Contains(leftNode.token.value) && leftNode.childs.Count == 0) && !leftNode.token.value.Contains(".") && !rightNode.token.value.Contains("."))
                        {
                            Syntax.SyntaxError($"Данной переменной несуществует!", leftNode);
                        }
                        // 1
                        if (rightNode.type == "VAR")
                        {
                            try
                            {
                                if ((varTypes[leftNode.token.value] != varTypes[rightNode.token.value]))
                                    Syntax.SyntaxError("Нельзя присваивать этой переменной значение другого типа", leftNode);
                            }
                            catch { }
                        }
                        // 2
                        else if (rightNode.type == "BINOPER")
                        { analis(rightNode, z_buffer + 1); }
                        /*else if (!(varTypes.ContainsKey(leftNode.token.value)))
                        { // Тож Ошибка}
                            if ((tyvarTypes[leftNode.token.value]] != rightNode.token.value))
                                Error.SyntaxError("Нельзя присвоить ", leftNode);
                        }*/
                        else if (leftNode.type == "BINOPER")
                        {
                            analis(leftNode, z_buffer + 1);
                        }
                        else
                        {
                            analis(rightNode, z_buffer + 1);
                        }
                        break;
                    }

                    break;
                case "VAR":
                    if (root.childs.Count > 0)
                    {
                        
                        if (varTypes.Keys.Contains(root.token.value))
                            Syntax.SyntaxError($"Нельзя объявлять две переменных с одним именем!", root);
                        CommonNode type = root.childs[0];
                        varTypes.Add(root.token.value, type);
                    } else
                    {
                        if (!varTypes.Keys.Contains(root.token.value) && !root.token.value.Contains("."))
                            Syntax.SyntaxError($"Нельзя объявлять переменные без указания типа!", root);
                    }
                    break;
                default:
                    if (root.childs.Count == 0 || root.type == "SIGNATURE")
                        break;
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        analis(root.childs[i], z_buffer + 1);
                    }
                    break;
            }
            return;
        }
    }
}
