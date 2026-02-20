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
        static public bool local;
        //static SemanticAnalyzer() { }

        static Dictionary<string, string> types = new Dictionary<string, string>()
        {
            {"int32", "NUMBER"},
            {"int16", "NUMBER"},
            {"int8", "NUMBER"},
            {"string", "STRING"},
            {"char", "CHAR"},
            {"bool", "BOOL"},
            {"float", "FLOAT"},
            {"int32_a", "dd"}
        };

        static Dictionary<string, CommonNode> varTypes = new Dictionary<string, CommonNode>();
        static Dictionary<string, CommonNode> varTypesLocal = new Dictionary<string, CommonNode>();

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

                    if (leftNode.type == "VAR" && leftNode.childs.Count == 1 && leftNode.childs[0].type != "OFFSET")
                    {
                        if (varTypes.Keys.Contains(root.token.value))
                            Syntax.SyntaxError($"Нельзя объявлять две переменных с одним именем!", root);
                        CommonNode type = leftNode.childs[0];
                        varTypes.Add(leftNode.token.value, type);
                    }

                    if (leftNode.type == "VAR" && rightNode.type == "VAR" && varTypes.Keys.Contains(leftNode.token.value) && varTypes.Keys.Contains(rightNode.token.value))
                    {
                        CommonNode leftNodeFloatType = varTypes[leftNode.token.value];
                        CommonNode rightNodeFloatType = varTypes[rightNode.token.value];
                        if ((leftNodeFloatType.token.value == "FLOAT" && rightNodeFloatType.token.value == "NUMBER"))
                            Syntax.SyntaxError("Ошибка нельзя складывать переменные разных типов!", leftNodeFloatType);
                        else if ((leftNodeFloatType.token.value == "NUMBER" && rightNodeFloatType.token.value == "FLOAT"))
                            Syntax.SyntaxError("Ошибка нельзя складывать переменные разных типов!", leftNodeFloatType);
                        else if ((leftNodeFloatType.token.value == "FLOAT" && rightNodeFloatType.token.value == "FLOAT"))
                            Syntax.SyntaxError("Ошибка нельзя складывать переменные разных типов!", leftNodeFloatType);
                        else if ((leftNodeFloatType.token.value == "NUMBER" && rightNodeFloatType.token.value == "NUMBER"))
                            Syntax.SyntaxError("Ошибка нельзя складывать переменные разных типов!", leftNodeFloatType);
                        else if ((leftNodeFloatType.token.value == "FLOAT" && rightNodeFloatType.token.value == "NUMBER"))
                            Syntax.SyntaxError("Ошибка нельзя складывать переменные разных типов!", leftNodeFloatType);
                        else if (leftNodeFloatType.token.value != rightNodeFloatType.token.value)
                            Syntax.SyntaxError("Ошибка нельзя складывать переменные разных типов!", leftNodeFloatType);

                        return;
                    }

                    if (leftNode.type == "VAR" && rightNode.type == "CALL" && !ast.declarotiveNames.Contains(rightNode.token.value))
                    {
                        if (!ast.resualtFunc.ContainsKey(rightNode.token.value))
                            Syntax.SyntaxError($"Ошибка Функция:{rightNode.token.value} не существует чтобы её вызывать!", rightNode);
                        if (!varTypes.ContainsKey(leftNode.token.value) || (local && !varTypesLocal.ContainsKey(leftNode.token.value)))
                            Syntax.SyntaxError($"Ошибка Переменной:{leftNode.token.value} не существует!", leftNode);
                        if (ast.resualtFunc[rightNode.token.value] == null)
                            Syntax.SyntaxError($"Ошибка Функция:{rightNode.token.value} не может возвращать в перменную значения типа void!", rightNode);
                        CommonNode typeResualt = ast.resualtFunc[rightNode.token.value];
                        CommonNode type = varTypes[leftNode.token.value];
                        if (type.token.value != typeResualt.token.value)
                            Syntax.SyntaxError($"Ошибка Функция:{rightNode.token.value} не может возвращать значение в Переменную:{leftNode.token.value} другого типа!", rightNode);
                    }

                    if (leftNode.type == "CALL" && !ast.resualtFunc.ContainsKey(leftNode.token.value) && !ast.declarotiveNames.Contains(leftNode.token.value))
                        Syntax.SyntaxError($"Ошибка Функция:{leftNode.token.value} не существует чтобы её вызывать!", leftNode);
                    if (rightNode.type == "CALL" && !ast.resualtFunc.ContainsKey(rightNode.token.value) && !ast.declarotiveNames.Contains(rightNode.token.value))
                        Syntax.SyntaxError($"Ошибка Функция:{rightNode.token.value} не существует чтобы её вызывать!", rightNode);
                    if (rightNode.type == "VAR" && !varTypes.ContainsKey(rightNode.token.value) && !rightNode.token.value.Contains("."))
                        Syntax.SyntaxError($"Ошибка Переменной:{rightNode.token.value} не существует чтобы её использовать!", rightNode);
                    if (leftNode.type == "VAR" && !varTypes.ContainsKey(leftNode.token.value) && !leftNode.token.value.Contains("."))
                        Syntax.SyntaxError($"Ошибка Переменной:{leftNode.token.value} не существует чтобы её использовать!", leftNode);

                    if (leftNode.type == "VAR")
                    {
                        if ((!local && !varTypes.Keys.Contains(leftNode.token.value) && leftNode.childs.Count == 0) && !leftNode.token.value.Contains(".") && !rightNode.token.value.Contains("."))
                        {
                            Syntax.SyntaxError($"Данной переменной несуществует!", leftNode);
                        }
                        /*if ((local && !varTypesLocal.Keys.Contains(leftNode.token.value) && leftNode.childs.Count == 0) && !leftNode.token.value.Contains(".") && !rightNode.token.value.Contains("."))
                        {
                            Syntax.SyntaxError($"Данной переменной несуществует!", leftNode);
                        }*/
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
                case "FLOATBINOPER":
                    CommonNode leftNodeFloat = take(root, 0);
                    CommonNode rightNodeFloat = take(root, 1);

                    if (leftNodeFloat.type == "VAR" && leftNodeFloat.childs.Count == 1)
                    {
                        if (varTypes.Keys.Contains(root.token.value))
                            Syntax.SyntaxError($"Нельзя объявлять две переменных с одним именем!", root);
                        CommonNode type = leftNodeFloat.childs[0];
                        varTypes.Add(leftNodeFloat.token.value, type);
                    }

                    if (leftNodeFloat.type == "VAR" && rightNodeFloat.type == "VAR" && varTypes.Keys.Contains(leftNodeFloat.token.value) && varTypes.Keys.Contains(rightNodeFloat.token.value))
                    {
                        CommonNode leftNodeFloatType = varTypes[leftNodeFloat.token.value];
                        CommonNode rightNodeFloatType = varTypes[rightNodeFloat.token.value];
                        if ((leftNodeFloatType.token.value == "FLOAT" && rightNodeFloatType.token.value == "NUMBER"))
                            Syntax.SyntaxError("Ошибка нельзя складывать переменные разных типов!", leftNodeFloatType);
                        else if ((leftNodeFloatType.token.value == "NUMBER" && rightNodeFloatType.token.value == "FLOAT"))
                            Syntax.SyntaxError("Ошибка нельзя складывать переменные разных типов!", leftNodeFloatType);
                        else if ((leftNodeFloatType.token.value == "FLOAT" && rightNodeFloatType.token.value == "FLOAT"))
                            Syntax.SyntaxError("Ошибка нельзя складывать переменные разных типов!", leftNodeFloatType);
                        else if ((leftNodeFloatType.token.value == "NUMBER" && rightNodeFloatType.token.value == "NUMBER"))
                            Syntax.SyntaxError("Ошибка нельзя складывать переменные разных типов!", leftNodeFloatType);
                        else if ((leftNodeFloatType.token.value == "FLOAT" && rightNodeFloatType.token.value == "NUMBER"))
                            Syntax.SyntaxError("Ошибка нельзя складывать переменные разных типов!", leftNodeFloatType);
                        else if (leftNodeFloatType.token.value != rightNodeFloatType.token.value)
                            Syntax.SyntaxError("Ошибка нельзя складывать переменные разных типов!", leftNodeFloatType);

                        /*if (
                            ((leftNodeFloatType.token.value == "FLOAT" && rightNodeFloatType.token.value == "NUMBER") ||
                            (leftNodeFloatType.token.value == "NUMBER" && rightNodeFloatType.token.value == "FLOAT")) &&
                            varTypes.ContainsKey(r)
                            )*/


                        return;
                    }

                    if (leftNodeFloat.type == "VAR" && rightNodeFloat.type == "CALL" && !ast.declarotiveNames.Contains(rightNodeFloat.token.value))
                    {
                        if (!ast.resualtFunc.ContainsKey(rightNodeFloat.token.value))
                            Syntax.SyntaxError($"Ошибка Функция:{rightNodeFloat.token.value} не существует чтобы её вызывать!", rightNodeFloat);
                        if (!varTypes.ContainsKey(leftNodeFloat.token.value) || (local && !varTypesLocal.ContainsKey(leftNodeFloat.token.value)))
                            Syntax.SyntaxError($"Ошибка Переменной:{leftNodeFloat.token.value} не существует!", leftNodeFloat);
                        if (ast.resualtFunc[rightNodeFloat.token.value] == null)
                            Syntax.SyntaxError($"Ошибка Функция:{rightNodeFloat.token.value} не может возвращать в перменную значения типа void!", rightNodeFloat);
                        CommonNode typeResualt = ast.resualtFunc[rightNodeFloat.token.value];
                        CommonNode type = varTypes[leftNodeFloat.token.value];
                        if (type.token.value != typeResualt.token.value)
                            Syntax.SyntaxError($"Ошибка Функция:{rightNodeFloat.token.value} не может возвращать значение в Переменную:{leftNodeFloat.token.value} другого типа!", rightNodeFloat);
                    }

                    if (leftNodeFloat.type == "CALL" && !ast.resualtFunc.ContainsKey(leftNodeFloat.token.value) && !ast.declarotiveNames.Contains(leftNodeFloat.token.value))
                        Syntax.SyntaxError($"Ошибка Функция:{leftNodeFloat.token.value} не существует чтобы её вызывать!", leftNodeFloat);
                    if (rightNodeFloat.type == "CALL" && !ast.resualtFunc.ContainsKey(rightNodeFloat.token.value) && !ast.declarotiveNames.Contains(rightNodeFloat.token.value))
                        Syntax.SyntaxError($"Ошибка Функция:{rightNodeFloat.token.value} не существует чтобы её вызывать!", rightNodeFloat);
                    if (rightNodeFloat.type == "VAR" && !varTypes.ContainsKey(rightNodeFloat.token.value) && !rightNodeFloat.token.value.Contains("."))
                        Syntax.SyntaxError($"Ошибка Переменной:{rightNodeFloat.token.value} не существует чтобы её использовать!", rightNodeFloat);
                    if (leftNodeFloat.type == "VAR" && !varTypes.ContainsKey(leftNodeFloat.token.value) && !leftNodeFloat.token.value.Contains("."))
                        Syntax.SyntaxError($"Ошибка Переменной:{leftNodeFloat.token.value} не существует чтобы её использовать!", leftNodeFloat);

                    if (leftNodeFloat.type == "VAR")
                    {
                        if ((!local && !varTypes.Keys.Contains(leftNodeFloat.token.value) && leftNodeFloat.childs.Count == 0) && !leftNodeFloat.token.value.Contains(".") && !rightNodeFloat.token.value.Contains("."))
                        {
                            Syntax.SyntaxError($"Данной переменной несуществует!", leftNodeFloat);
                        }
                        /*if ((local && !varTypesLocal.Keys.Contains(leftNode.token.value) && leftNode.childs.Count == 0) && !leftNode.token.value.Contains(".") && !rightNode.token.value.Contains("."))
                        {
                            Syntax.SyntaxError($"Данной переменной несуществует!", leftNode);
                        }*/
                        // 1
                        if (rightNodeFloat.type == "VAR")
                        {
                            try
                            {
                                if ((varTypes[leftNodeFloat.token.value] != varTypes[rightNodeFloat.token.value]))
                                    Syntax.SyntaxError("Нельзя присваивать этой переменной значение другого типа", leftNodeFloat);
                            }
                            catch { }
                        }
                        // 2
                        else if (rightNodeFloat.type == "FLOATBINOPER")
                        { analis(rightNodeFloat, z_buffer + 1); }
                        /*else if (!(varTypes.ContainsKey(leftNode.token.value)))
                        { // Тож Ошибка}
                            if ((tyvarTypes[leftNode.token.value]] != rightNode.token.value))
                                Error.SyntaxError("Нельзя присвоить ", leftNode);
                        }*/
                        else if (leftNodeFloat.type == "FLOATBINOPER")
                        {
                            analis(leftNodeFloat, z_buffer + 1);
                        }
                        else
                        {
                            analis(rightNodeFloat, z_buffer + 1);
                        }
                        break;
                    }

                    break;
                case "FUNC":
                    Dictionary<string, CommonNode> types = new Dictionary<string, CommonNode>();
                    foreach (var v in varTypes)
                    {
                        types.Add(v.Key, v.Value);
                    }
                    for (int i = 0; i < take(root, 1).childs.Count; i++)
                    {
                        varTypes.Add(take(root, 1).childs[i].token.value,  take(root, 1).childs[i].childs[0]);
                    }
                    analis(take(root, 2), z_buffer + 1);
                    varTypes.Clear();
                    foreach (var v in types)
                    {
                        varTypes.Add(v.Key, v.Value);
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
                /*case "BODY":
                    local = true;
                    Dictionary<string, CommonNode> varTypesTemp = new Dictionary<string, CommonNode>();
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        analis(root.childs[i], z_buffer + 1);
                    }
                    varTypes = varTypesTemp;
                    local = false;
                    break;*/
                default:
                    if (root.childs.Count == 0 || root.type == "SIGNATURE" || root.type == "CMP" || root.type == "STRUCT" || root.type == "FUNC")
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
