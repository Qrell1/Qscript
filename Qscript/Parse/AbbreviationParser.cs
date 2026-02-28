using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qscript
{
    public class AbbreviationParser
    {
        public ProgramNode ast;
        public Dictionary<string, CommonNode> varTypes = new Dictionary<string, CommonNode>();
        public Dictionary<string, CommonNode> varTypesLocal = new Dictionary<string, CommonNode>();

        private Dictionary<string, string> types = new Dictionary<string, string>()
        {
            {"int32", "dd"},
            {"int16", "dw"},
            {"int8", "db"},
            {"byte", "db"},
            {"string", "db"},
            {"char", "db"},
            {"float", "dd"},
            {"int32_a", "dd"}
        };

        public AbbreviationParser() { }

        public CommonNode take(CommonNode node, int i = 0)
        {
            if (node.childs.Count >= i + 1)
            {
                return node.childs[i];
            }
            return null;
        }

        public ProgramNode abbParse(ProgramNode root)
        {
            ast = root;
            CommonNode astNode = copyNodes(root);
            // Replace Constant Var Value
            astNode = replaceConstantVarValue(astNode);
            // BinOper Cheak Float
            astNode = binOperCheak(astNode);
            // Constant BinOper ReFresh
            astNode = binOperReFresh(astNode);
            // Second Repcale Constant Var Value
            astNode = replaceConstantVarValue(astNode);
            // Cmp ReFresh
            astNode = cmpReFresh(astNode);
            // If destroy
            astNode = ifCheakDelete(astNode);
            // General Parse
            ast.childs.Clear();
            for (int i = 0; i < astNode.childs.Count; i++) ast.childs.Add(astNode.childs[i]);
            ast.childs = parse(ast, 0).childs;
            ast.varTypes = varTypes;
            return ast;
        }

        // Вспомогательные функции
        private Dictionary<CommonNode, CommonNode> cheakAllVarInLocal(CommonNode root)
        {
            if (root == null) return null;
            if (root.type == "VAR" && root.childs.Count != 0 && root.childs[0].type == "TYPE")
                return new Dictionary<CommonNode, CommonNode> { { root, null } };

            Dictionary<CommonNode, CommonNode> resualt = new Dictionary<CommonNode, CommonNode>();
            for (int i = 0; i < root.childs.Count; i++)
            {
                Dictionary<CommonNode, CommonNode> vars = cheakAllVarInLocal(root.childs[i]);
                if (vars != null && vars.Count != 0) foreach (var child in vars) resualt.Add(child.Key, child.Value);
            }
            if (resualt.Count != 0) return resualt;
            else return null;
        }
        private bool varContains (CommonNode root, Dictionary<CommonNode, CommonNode> vars)
        {
            foreach (var var in vars) if (var.Key.token.value == root.token.value) return true;
            return false;
        }
        private CommonNode getVar (CommonNode root, Dictionary<CommonNode, CommonNode> vars)
        {
            foreach (var var in vars) if (var.Key.token.value == root.token.value) return var.Key;
            return null;
        }
        private CommonNode getVarValue (CommonNode root, Dictionary<CommonNode, CommonNode> vars)
        {
            foreach (var var in vars) if (var.Key.token.value == root.token.value) return var.Value;
            return null;
        }

        private CommonNode replaceConstantVarValue(CommonNode root, Dictionary<CommonNode, CommonNode> varsLocal = null)
        {
            if (root.token.value == "=" && root.childs[0].type == "VAR"
                && varsLocal != null && varContains(root.childs[0], varsLocal)
                && (root.childs[1].type == "NUMBER" || root.childs[1].type == "FLOAT" || root.childs[1].type == "STRING" || root.childs[1].type == "CHAR")
                )
            {
                varsLocal[getVar(root.childs[0], varsLocal)] = root.childs[1];
            }
            if (root.type == "BINOPER" && root.token.value != "=" && root.childs[0].type == "VAR" && varsLocal != null
                && varContains(root.childs[0], varsLocal) && getVarValue(root.childs[0], varsLocal) != null)
            {
                varsLocal[getVar(root.childs[0], varsLocal)] = null;
            }
            if (root.type == "BINOPER" && root.token.value != "=" && root.childs[1].type == "VAR" && varsLocal != null
                && varContains(root.childs[1], varsLocal) && getVarValue(root.childs[1], varsLocal) != null)
            {
                varsLocal[getVar(root.childs[1], varsLocal)] = null;
            }
            if (root.type == "BINOPER" && root.token.value != "=" && root.childs[0].type == "VAR" && varsLocal != null
                && varContains(root.childs[0], varsLocal) && getVarValue(root.childs[0], varsLocal) != null)
            {
                root.childs[0] = getVarValue(root.childs[0], varsLocal);
            }
            if (root.type == "BINOPER" && root.childs[1].type == "VAR" && varsLocal != null
                && varContains(root.childs[1], varsLocal) && getVarValue(root.childs[1], varsLocal) != null)
            {
                root.childs[1] = getVarValue(root.childs[1], varsLocal);
            }
            for (int i = 0; i < root.childs.Count; i++)
            {
                if (root.type != "BINOPER" && root.type != "FLOATBINOPER" && root.childs[i].type == "VAR"
                    && root.childs[i].childs.Count == 0 && varsLocal != null
                    && varContains(root.childs[i], varsLocal) && getVarValue(root.childs[i], varsLocal) != null)
                    root.childs[i] = getVarValue(root.childs[i], varsLocal);
            }
            try
            {
                Dictionary<CommonNode, CommonNode> vars = cheakAllVarInLocal(root);
                if (varsLocal != null && vars != null) foreach (var child in varsLocal)
                        if (!vars.ContainsKey(child.Key)) vars.Add(child.Key, child.Value);

                for (int i = 0; i < root.childs.Count; i++)
                    root.childs[i] = replaceConstantVarValue(root.childs[i], vars);
            }
            catch { Console.WriteLine("CONSTANT VAR ERROR"); }

            return root;
        }
        private CommonNode binOperCheak(CommonNode root)
        {
            /*
             * Как и по каким признакам мы выявляем присутствие флотовых операций
             * Некоторые признаки основной узел TYPEOPER и его значени float и ещё конечно переменные флотовые
             * Ещё функции с результатом флота или же сами флот числа
             */
            if (root.type != "BINOPER")
            {
                for (int i = 0; i < root.childs.Count; i++) root.childs[i] = binOperCheak(root.childs[i]);
                return root;
            }
            root.childs[0] = binOperCheak(root.childs[0]);
            root.childs[1] = binOperCheak(root.childs[1]);

            CommonNode leftNode = take(root, 0);
            CommonNode rightNode = take(root, 1);

            bool flagFloat = false;

            if (leftNode.type == "VAR" && varTypes.ContainsKey(leftNode.token.value) && varTypes[leftNode.token.value].token.value == "float") flagFloat = true;
            if (rightNode.type == "VAR" && varTypes.ContainsKey(rightNode.token.value) && varTypes[rightNode.token.value].token.value == "float") flagFloat = true;

            if (leftNode.type == "VAR" && ast.resualtFunc.ContainsKey(leftNode.token.value) && ast.resualtFunc[leftNode.token.value].token.value == "float") flagFloat = true;
            if (rightNode.type == "VAR" && ast.resualtFunc.ContainsKey(rightNode.token.value) && ast.resualtFunc[rightNode.token.value].token.value == "float") flagFloat = true;

            if (leftNode.type == "VAR" && ast.typesArgsFunc.ContainsKey(leftNode.token.value) && ast.typesArgsFunc[leftNode.token.value].token.value == "float") flagFloat = true;
            if (rightNode.type == "VAR" && ast.typesArgsFunc.ContainsKey(rightNode.token.value) && ast.typesArgsFunc[rightNode.token.value].token.value == "float") flagFloat = true;

            if (leftNode.type == "FLOATBINOPER") flagFloat = true;
            if (rightNode.type == "FLOATBINOPER") flagFloat = true;


            if (leftNode.type == "TYPEOPER")
            {
                if (leftNode.token.value == "float") flagFloat = true;
                leftNode = leftNode.childs[0];
            }

            if (rightNode.type == "TYPEOPER")
            {
                if (rightNode.token.value == "float") flagFloat = true;
                rightNode = rightNode.childs[0];
            }

            if (flagFloat) root.type = "FLOATBINOPER";

            return root;
        }

        private CommonNode binOperReFresh(CommonNode root)
        {
            /*
             * Как и по каким признакам мы выявляем что можно сокротить константы
             * Ну конечно во первых если 2 оператора числа или флоты но это для флотовых
             * 
             */
            if (root.type != "BINOPER" && root.type != "FLOATBINOPER")
            {
                for (int i = 0; i < root.childs.Count; i++) root.childs[i] = binOperReFresh(root.childs[i]);
                return root;
            }
            root.childs[0] = binOperReFresh(root.childs[0]);
            root.childs[1] = binOperReFresh(root.childs[1]);

            CommonNode leftNode = take(root, 0);
            CommonNode rightNode = take(root, 1);

            CommonNode reFreshNode = null;

            if (root.type == "FLOATBINOPER")
            {
                if (leftNode.type == "FLOAT" && rightNode.type == "FLOAT")
                    switch (root.token.value)
                    {
                        case "+": reFreshNode = new CommonNode("FLOAT", new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToDouble(leftNode.token.value) + Convert.ToDouble(rightNode.token.value)), root.token.pos)); break;
                        case "-":
                            reFreshNode = new CommonNode("FLOAT", new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToDouble(leftNode.token.value) - Convert.ToDouble(rightNode.token.value)), root.token.pos)); break;
                        case "*":
                            reFreshNode = new CommonNode("FLOAT", new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToDouble(leftNode.token.value) * Convert.ToDouble(rightNode.token.value)), root.token.pos)); break;
                        case "/":
                            reFreshNode = new CommonNode("FLOAT", new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToDouble(leftNode.token.value) / Convert.ToDouble(rightNode.token.value)), root.token.pos)); break;
                        case "%":
                            reFreshNode = new CommonNode("FLOAT", new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToDouble(leftNode.token.value) % Convert.ToDouble(rightNode.token.value)), root.token.pos)); break;
                    }
            } else if (root.type == "BINOPER")
            {
                if (leftNode.type == "NUMBER" && rightNode.type == "NUMBER")
                    switch (root.token.value)
                    {
                        case "+":
                            reFreshNode = new CommonNode("NUMBER", new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToInt32(leftNode.token.value) + Convert.ToInt32(rightNode.token.value)), root.token.pos)); break;
                        case "-":
                            reFreshNode = new CommonNode("NUMBER", new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToInt32(leftNode.token.value) - Convert.ToInt32(rightNode.token.value)), root.token.pos)); break;
                        case "*":
                            reFreshNode = new CommonNode("NUMBER", new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToInt32(leftNode.token.value) * Convert.ToInt32(rightNode.token.value)), root.token.pos)); break;
                        case "/":
                            reFreshNode = new CommonNode("NUMBER", new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToInt32(leftNode.token.value) / Convert.ToInt32(rightNode.token.value)), root.token.pos)); break;
                        case "%":
                            reFreshNode = new CommonNode("NUMBER", new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToInt32(leftNode.token.value) % Convert.ToInt32(rightNode.token.value)), root.token.pos)); break;
                    }
            }

            if (reFreshNode != null) return reFreshNode;

            return root;
        }

        private CommonNode cmpReFresh(CommonNode root)
        {
            /*
             * //Как и по каким признакам мы выявляем что можно сокротить константы
             * //Ну конечно во первых если 2 оператора числа или флоты но это для флотовых
             * 
             */
            if (root.type != "CMP")
            {
                for (int i = 0; i < root.childs.Count; i++) root.childs[i] = cmpReFresh(root.childs[i]);
                return root;
            }
            root.childs[0] = cmpReFresh(root.childs[0]);
            root.childs[1] = cmpReFresh(root.childs[1]);

            CommonNode leftNode = take(root, 0);
            CommonNode rightNode = take(root, 1);

            if (leftNode.type == "CMP" && (leftNode.token.value == "true") &&
                rightNode.type == "CMP" && (rightNode.token.value == "true")) return new CommonNode(root.type, new Token(root.token.type, "true", root.token.pos));
            if (leftNode.type == "CMP" && (leftNode.token.value == "false") &&
                rightNode.type == "CMP" && (rightNode.token.value == "false")) return new CommonNode(root.type, new Token(root.token.type, "false", root.token.pos));
            if (leftNode.type == "CMP" && rightNode.type == "CMP" && root.token.value == "&&") return new CommonNode(root.type, new Token(root.token.type, "false", root.token.pos));
            if (leftNode.type == "CMP" && rightNode.type == "CMP" && root.token.value == "||") return new CommonNode(root.type, new Token(root.token.type, "true", root.token.pos));

            bool cmpB = false;
            bool cmp = false;
            decimal leftValue = decimal.Zero;
            decimal rightValue = decimal.Zero;

            leftNode.token.value = leftNode.token.value.Replace(".", ",");
            rightNode.token.value = rightNode.token.value.Replace(".", ",");

            if (leftNode.type == "FLOAT") leftNode.token.value = leftNode.token.value.Replace("f", "");
            if (rightNode.type == "FLOAT") rightNode.token.value = rightNode.token.value.Replace("f", "");

            if (leftNode.type == "NUMBER" || leftNode.type == "FLOAT") leftValue = Convert.ToDecimal(leftNode.token.value);
            if (rightNode.type == "NUMBER" || rightNode.type == "FLOAT") rightValue = Convert.ToDecimal(rightNode.token.value);

            if (leftValue != decimal.Zero && rightValue != decimal.Zero)
            {
                switch (root.token.value)
                {
                    case "==": cmp = (leftValue == rightValue) ? true : false; cmpB = true; break;
                    case "!=": cmp = (leftValue != rightValue) ? true : false; cmpB = true; break;
                    case "<=": cmp = (leftValue <= rightValue) ? true : false; cmpB = true; break;
                    case ">=": cmp = (leftValue >= rightValue) ? true : false; cmpB = true; break;
                    case "<": cmp = (leftValue < rightValue) ? true : false; cmpB = true; break;
                    case ">": cmp = (leftValue > rightValue) ? true : false; cmpB = true; break;
                }
            }
            if (cmpB == true) return new CommonNode(root.type, new Token(root.token.type, (cmp) ? "true" : "false", root.token.pos));

            return root;
        }

        private CommonNode ifCheakDelete(CommonNode root)
        {
            if (root.type != "IF")
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = ifCheakDelete(root.childs[i]);
                }
                return root;
            }

            CommonNode cmpNode = take(root, 0);
            CommonNode bodyNode = take(root, 1);

            if (cmpNode.token.value == "false") return new CommonNode("AIR", root.token);
            if (cmpNode.token.value == "true") return bodyNode;

            return root;
        }

        public CommonNode parse(CommonNode root, int z_buffer)
        {
            switch (root.type)
            {
                //case "BINOPER":
                    //parseBinOper(root, z_buffer);
                    //CommonNode leftNode = take(root, 0);
                    //CommonNode rightNode = take(root, 1);

                    /*if (
                        (leftNode.type == "VAR" && rightNode.type == "VAR") &&
                        varTypes.Keys.Contains(leftNode.token.value) && varTypes.Keys.Contains(rightNode.token.value) &&
                        !(leftNode.token.value.Contains(".") || rightNode.token.value.Contains("."))
                        )
                    {
                        if (varTypes[leftNode.token.value].token.value == "float" || varTypes[rightNode.token.value].token.value == "float")
                        {
                            CommonNode leftNodeTemp = parse(leftNode, z_buffer);
                            CommonNode rightNodeTemp = parse(rightNode, z_buffer);
                            root.childs[0] = leftNodeTemp;
                            root.childs[1] = rightNodeTemp;
                            root.type = "FLOATBINOPER";
                            return root;
                        }
                    } else if ((leftNode.type == "VAR" && rightNode.type == "VAR") &&
                        !(leftNode.token.value.Contains(".") || rightNode.token.value.Contains(".")) &&
                        !varTypes.Keys.Contains(leftNode.token.value) && !varTypes.Keys.Contains(rightNode.token.value)
                        )
                    {
                        Syntax.SyntaxError($"Нельзя складывать не объявленные Переменные: {leftNode.token.value}, {rightNode.token.value}", leftNode);
                    }
                    if (leftNode.type == "FLOAT" || rightNode.type == "FLOAT" || (leftNode.type == "CALL" && ast.resualtFunc[leftNode.token.value].token.value == "float") || (rightNode.type == "CALL" && ast.resualtFunc[rightNode.token.value].token.value == "float") ||
                        (
                        (leftNode.type == "VAR" && varTypes.Keys.Contains(leftNode.token.value) && varTypes[leftNode.token.value].token.value == "float") &&
                        (rightNode.type == "VAR" && varTypes.Keys.Contains(rightNode.token.value) && varTypes[rightNode.token.value].token.value == "float")
                        )
                        )
                    {
                        CommonNode leftNodeTemp = parse(leftNode, z_buffer);
                        CommonNode rightNodeTemp = parse(rightNode, z_buffer);
                        root.childs[0] = leftNodeTemp;
                        root.childs[1] = rightNodeTemp;
                        root.type = "FLOATBINOPER";
                        return root;
                    }
                    if (leftNode.type == "BINOPER" || rightNode.type == "BINOPER")
                    {
                        if (leftNode.type == "BINOPER")
                        {
                            CommonNode leftNodeTemp = parse(leftNode, z_buffer);
                            //root.childs[0] = leftNodeTemp;
                            if (leftNodeTemp.type == "FLOATBINOPER") root.type = "FLOATBINOPER";
                        }
                        if (rightNode.type == "BINOPER")
                        {
                            CommonNode rightNodeTemp = parse(rightNode, z_buffer);
                            //root.childs[0] = rightNodeTemp;
                            if (rightNodeTemp.type == "FLOATBINOPER") root.type = "FLOATBINOPER";
                        }
                    }
                    if (leftNode.type == "BINOPER" && rightNode.type == "BINOPER")
                    {
                        CommonNode leftNodeTemp = parse(leftNode, z_buffer);
                        CommonNode rightNodeTemp = parse(rightNode, z_buffer);
                        if (leftNode.type == "FLOATBINOPER" || rightNode.type == "FLOATBINOPER")
                        {
                            root.childs[0] = leftNodeTemp;
                            root.childs[1] = rightNodeTemp;
                            root.type = "FLOATBINOPER";
                            return root;
                        }
                    }


                    if (leftNode.type == "NUMBER" && rightNode.type == "NUMBER")
                    {
                        int resualt = 0;
                        switch (root.token.value)
                        {
                            case "+":
                                resualt = Convert.ToInt32(leftNode.token.value) + Convert.ToInt32(rightNode.token.value);
                                break;
                            case "-":
                                resualt = Convert.ToInt32(leftNode.token.value) - Convert.ToInt32(rightNode.token.value);
                                break;
                            case "*":
                                resualt = Convert.ToInt32(leftNode.token.value) * Convert.ToInt32(rightNode.token.value);
                                break;
                            case "/":
                                resualt = Convert.ToInt32(leftNode.token.value) / Convert.ToInt32(rightNode.token.value);
                                break;
                        }
                        root.type = "NUMBER";
                        root.childs = new List<CommonNode>();
                        root.token.type.type = "NUMBER";
                        root.token.value = resualt.ToString();
                        break;
                    }
                    if (leftNode.type != "NUMBER")
                        leftNode = parse(leftNode, z_buffer);
                    if (rightNode.type != "NUMBER")
                        rightNode = parse(rightNode, z_buffer);

                    if (leftNode.type == "NUMBER" && rightNode.type == "NUMBER")
                    {
                        int resualt = 0;
                        switch (root.token.value)
                        {
                            case "+":
                                resualt = Convert.ToInt32(leftNode.token.value) + Convert.ToInt32(rightNode.token.value);
                                break;
                            case "-":
                                resualt = Convert.ToInt32(leftNode.token.value) - Convert.ToInt32(rightNode.token.value);
                                break;
                            case "*":
                                resualt = Convert.ToInt32(leftNode.token.value) * Convert.ToInt32(rightNode.token.value);
                                break;
                            case "/":
                                resualt = Convert.ToInt32(leftNode.token.value) / Convert.ToInt32(rightNode.token.value);
                                break;
                        }
                        root.type = "NUMBER";
                        root.childs = new List<CommonNode>();
                        root.token.type.type = "NUMBER";
                        root.token.value = resualt.ToString();
                        break;
                    }*/
                    //break;
                case "CONST":
                    parseConst(root, z_buffer);
                    break;
                case "STRING":
                    root.token.value = $"'{root.token.value}', 0";
                    return root;
                    break;
                case "INLINE":
                    return parseInline(root, z_buffer);
                    break;
                case "FUNC":
                    return parseFunc(root, z_buffer);
                    break;
                case "CALL":
                    return parseCall(root, z_buffer);
                    break;
                case "STRUCT":
                    return parseStruct(root, z_buffer);
                    break;
                case "CLASS":
                    if (ast.declarotivePatternsStruct.Keys.Contains(root.token.value)) return new CommonNode("AIR", root.token);
                    return root;
                    break;
                case "USING":
                    return parseUsing(root, z_buffer);
                    break;
                case "VAR":
                    return parseVar(root, z_buffer);
                    break;
                case "MODIFIER":
                    return parseModifier(root, z_buffer);
                    break;
                case "FOR":
                    return parseCycle(root, z_buffer);
                    break;
                default:
                    if (root.childs.Count == 0)
                        break;
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        root.childs[i] = parse(root.childs[i], z_buffer + 1);
                    }
                    return root;
                    break;
            }
            return root;
        }
        private CommonNode parseBinOper(CommonNode root, int z_buffer)
        {
            CommonNode body = take(root, 1);
            for (int i = 0; i < body.childs.Count; i++)
            {
                body.childs[i] = parse(body.childs[i], z_buffer + 2);
                if (body.childs[i].type == "RETURN")
                    throw new Exception("Ошибка в инлайн функции не может быть return");
            }
            ast.inlineNames.Add(root.token.value);
            return root;
        }
        private CommonNode parseInline(CommonNode root, int z_buffer)
        {
            CommonNode body = take(root, 1);
            for (int i = 0; i < body.childs.Count; i++)
            {
                body.childs[i] = parse(body.childs[i], z_buffer + 2);
                if (body.childs[i].type == "RETURN")
                    throw new Exception("Ошибка в инлайн функции не может быть return");
            }
            ast.inlineNames.Add(root.token.value);
            return root;
        }
        private CommonNode parseFunc(CommonNode root, int z_buffer)
        {
            if (ast.declarotivePatternsFunctions.Keys.Contains(root.token.value)) return new CommonNode("AIR", root.token);
            CommonNode signature = take(root, 1);
            CommonNode bodyFunc = take(root, 2);
            List<CommonNode> childs = new List<CommonNode>();
            if (ast.resualtFunc[root.token.value] != null && ast.resualtFunc[root.token.value].token.value != "void" && !types.Keys.Contains(ast.resualtFunc[root.token.value].token.value))
            {
                CommonNode resualtVar = new CommonNode("VAR", new Token(null, "resualtPtr", signature.token.pos));
                resualtVar.childs.Add(ast.resualtFunc[root.token.value]);
                childs.Add(resualtVar);
            }
            Dictionary<string, CommonNode> varTypesTemp = new Dictionary<string, CommonNode>();
            foreach (var v in varTypes)
            {
                varTypesTemp.Add(v.Key, v.Value);
            }
            foreach (CommonNode child in signature.childs)
            {
                varTypes.Add(child.token.value, child.childs[0]);
            }
            for (int i = 0; i < bodyFunc.childs.Count; i++)
            {
                bodyFunc.childs[i] = parse(bodyFunc.childs[i], z_buffer + 2);
            }
            varTypes.Clear();
            foreach (var v in varTypesTemp)
            {
                varTypes.Add(v.Key, v.Value);
            }
            childs.AddRange(signature.childs);
            ast.typesArgsFunc.Add(root.token.value, signature);
            signature.childs = childs;
            root.childs[1] = signature;
            root.childs[2] = bodyFunc;
            return root;
        }
        private CommonNode parseModifier(CommonNode root, int z_buffer)
        {
            for (int i = 0; i < root.childs.Count; i++)
            {
                if (root.childs[i].type != "VAR") root.childs[i] = parse(root.childs[i], z_buffer + 1);
            }
            return root;
        }
        private CommonNode parseStruct(CommonNode root, int z_buffer)
        {
            if (ast.declarotivePatternsStruct.Keys.Contains(root.token.value)) return new CommonNode("AIR", root.token);
            Dictionary<string, CommonNode> typesVar = new Dictionary<string, CommonNode>();
            foreach (CommonNode var in root.childs)
            {
                typesVar.Add(var.token.value, var.childs[0]);
            }
            ast.structs.Add(root.token.value, typesVar);
            return root;
        }
        private CommonNode parseUsing(CommonNode root, int z_buffer)
        {
            if (root.childs.Count != 2) return root;
            CommonNode name = take(root, 0);
            CommonNode mode = take(root, 1);
            if (mode.type == "INLINE" && name.childs.Count == 0) ast.inlineNames.Add(name.token.value);
            else if (mode.type == "INLINE")
            {
                foreach (CommonNode childName in name.childs) ast.inlineNames.Add(childName.token.value);
            }
            return new CommonNode("AIR", root.token);
        }
        private CommonNode parseCall(CommonNode root, int z_buffer)
        {
            if (ast.declarotivePatternsFunctions.Keys.Contains(root.token.value)) root = generationDeclarationFunc(root);
            if (take(root, 0).type == "DECLARATOR") Syntax.SyntaxError("Ошибка использывание не декларотивную функцию как декларотивную!", root);

            CommonNode signatureCall = take(root, 0);
            for (int j = 0; j < signatureCall.childs.Count; j++)
            {
                signatureCall.childs[j] = parse(signatureCall.childs[j], z_buffer + 2);
            }
            root.childs[0] = signatureCall;
            return root;
        }
        private CommonNode parseVar(CommonNode root, int z_buffer)
        {
            if (root.childs.Count > 0)
            {
                if (varTypes.Keys.Contains(root.token.value))
                    Syntax.SyntaxError($"Нельзя объявлять две переменных с одним именем {root.token.value}!", root);

                CommonNode type = root.childs[0];
                if (type.childs.Count > 0 && root.childs[0].type != "OFFSET")
                {
                    type.token.value = generationDeclarationStruct(type, type.childs[0]);
                    type.childs.Clear();
                }
                if (root.childs[0].type != "OFFSET") varTypes.Add(root.token.value, type);
            }
            return root;
        }
        private CommonNode parseConst(CommonNode root, int z_buffer)
        {
            if (take(root, 0).type == "NUMBER")
            {
                root.childs[0] = parse(root.childs[0], z_buffer + 1);
                return root;
            }
            else if (take(root, 0).type == "STRING")
            {
                root.childs[0] = parse(root.childs[0], z_buffer + 1);
                return root;
            }
            else if (take(root, 0).type != "BINOPER")
                throw new Exception("Ошибка не верный токен ");

            root.childs[0] = parse(root.childs[0], z_buffer + 1);
            return root;
        }
        private CommonNode parseCycle(CommonNode root, int z_buffer)
        {
            if (root.type == "FOR")
            {
                Dictionary<string, CommonNode> varTypesTempFor = new Dictionary<string, CommonNode>();
                foreach (var v in varTypes)
                {
                    varTypesTempFor.Add(v.Key, v.Value);
                }
                CommonNode recurse(CommonNode commonNode)
                {
                    foreach (var child in commonNode.childs)
                    {
                        if (child.childs.Count == 1 && child.childs[0].type == "TYPE") return child;
                        return recurse(child);
                    }
                    return null;
                }
                //parse(take(root, 0), z_buffer + 1);
                CommonNode varNode = recurse(take(root, 0));
                if (varNode == null) { Program.PrintAST(root, 0); Syntax.SyntaxError("Ошибка в объявлениии переменной в цикле For", root); }
                parse(take(root, 3), z_buffer + 1);
                varTypes.Clear();
                foreach (var v in varTypesTempFor)
                {
                    varTypes.Add(v.Key, v.Value);
                }
            }
            return root;
        }



        private CommonNode copyNodes (CommonNode root)
        {
            CommonNode rootNode = new CommonNode(root.type, new Token(root.token.type, root.token.value, root.token.pos));
            List<CommonNode> nodes = new List<CommonNode>();

            for (int i = 0; i < root.childs.Count; i++)
            {
                nodes.Add(copyNodes(root.childs[i]));
            }
            rootNode.childs = nodes;
            return rootNode;
        }
        private CommonNode replaceNodes (CommonNode rootNode, ref Dictionary<string, string> table)
        {
            CommonNode root = new CommonNode(rootNode.type, new Token(rootNode.token.type, rootNode.token.value, rootNode.token.pos));
            root.childs = new List<CommonNode>();

            if (table.Keys.Contains(root.token.value))
                root.token.value = table[root.token.value];
            for (int i = 0; i < rootNode.childs.Count; i++)
            {
                if (root.type == "TYPE" && rootNode.childs[i].type == "DECLARATOR")
                {
                    CommonNode replace = replaceNodes(rootNode.childs[i], ref table);
                    string name = generationDeclarationStruct(root, replace);
                    replace.token.value = name;
                    root = replace;
                    root.type = "TYPE";
                    root.childs.Clear();
                    continue;
                }
                root.childs.Add(replaceNodes(rootNode.childs[i], ref table));
            }
            return root;
        }

        private string generationDeclarationStruct (CommonNode type, CommonNode declarator)
        {
            string secondDeclarator = string.Empty;
            if (declarator.childs.Count > 0 && declarator.childs[0].childs.Count > 0 && declarator.childs[0].childs[0].type == "DECLARATOR")
                secondDeclarator = generationDeclarationStruct(declarator.childs[0], declarator.childs[0].childs[0]);
            if (!ast.declarotivePatternsStruct.Keys.Contains(type.token.value)) Syntax.SyntaxError($"Невозможно объявить декларотивный Тип:{type.token.value} так как его не существует!", type);
            CommonNode pattern = ast.declarotivePatternsStruct[type.token.value];
            CommonNode patternDeclarator = take(pattern, 0);

            if (patternDeclarator.childs.Count != declarator.childs.Count) Syntax.SyntaxError($"Невозможно объявить декларотивный Тип:{type.token.value} так как количесва типов разные!", declarator);

            string newName = string.Empty;
            if (secondDeclarator == string.Empty)
            {
                newName = pattern.token.value;
                for (int i = 0; i < declarator.childs.Count; i++)
                {
                    newName += $"_{declarator.childs[i].token.value}";
                }
            }
            else
            {
                newName = secondDeclarator;
                for (int i = 0; i < declarator.childs.Count; i++)
                {
                    newName = $"{declarator.childs[i].token.value}_" + newName;
                }
            }
            if (ast.declarotiveNames.Contains(newName)) return newName;

            Dictionary<string, string> declaratorTypes = new Dictionary<string, string>();
            for (int i = 0; i < patternDeclarator.childs.Count; i++)
            {
                string declaratorString = declarator.childs[i].token.value;
                if (secondDeclarator != string.Empty) declaratorString = secondDeclarator; // declarator.childs[i].token.value + "_" + 
                declaratorTypes.Add(patternDeclarator.childs[i].token.value, declaratorString);
            }
            ast.declarotiveNames.Add(newName);
            CommonNode newStruct = replaceNodes(pattern, ref declaratorTypes);
            newStruct.childs.RemoveAt(0);
            newStruct.token.value = newName;

            ast.childs.Add(newStruct);

            return newName;
        }
        private CommonNode generationDeclarationFunc(CommonNode root)
        {
            if (!ast.declarotivePatternsFunctions.Keys.Contains(root.token.value)) Syntax.SyntaxError($"Невозможно объявить декларотивный Тип:{root.token.value} так как его не существует!", root);
            CommonNode pattern = ast.declarotivePatternsFunctions[root.token.value];
            CommonNode patternDeclarator = pattern.childs.Last();

            CommonNode declarator = take(root, 0);

            if (patternDeclarator.childs.Count != declarator.childs.Count) Syntax.SyntaxError($"Невозможно объявить декларотивный Тип:{root.token.value} так как количесва типов разные!", declarator);

            string newName = string.Empty;
            newName += pattern.token.value;
            //if () newName += $"_{}";
            for (int i = 0; i < declarator.childs.Count; i++)
            {
                //newName += $"_{declarator.childs[i].token.value}";
                newName += $"_{generationDeclarationType(declarator.childs[i])}";
            }
            root.token.value = newName;
            root.childs.RemoveAt(0);
            if (ast.declarotiveNames.Contains(newName)) return root;

            Dictionary<string, string> declaratorTypes = new Dictionary<string, string>();
            for (int i = 0; i < patternDeclarator.childs.Count; i++)
            {
                declaratorTypes.Add(patternDeclarator.childs[i].token.value, generationDeclarationType(declarator.childs[i]));
            }
            ast.declarotiveNames.Add(newName);
            CommonNode newFunc = replaceNodes(pattern, ref declaratorTypes);
            newFunc.childs.RemoveAt(newFunc.childs.Count-1);
            ast.resualtFunc.Add(newName, take(newFunc, 0));
            newFunc.token.value = newName;


            ast.childs.Add(newFunc);
            
            return root;
        }
        private string generationDeclarationType(CommonNode declarator)
        {
            if (declarator.childs.Count == 0)
                return declarator.token.value;

            string result = string.Empty;
            result += declarator.token.value;
            declarator = take(declarator, 0);
            foreach (var ch in declarator.childs)
            {
                result += "_" + generationDeclarationType(ch);
            }

            return result;
        }
    }
}
