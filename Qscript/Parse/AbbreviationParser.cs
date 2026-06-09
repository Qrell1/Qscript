using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Qscript
{
    public class AbbreviationParser
    {
        public ProgramNode ast;
        public VarSpace varSpace = new VarSpace();

        private List<string> strings = new List<string>();
        private List<string> varRegisters = new List<string>();

        private int lambdaIndex;

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
            // Const Remove
            astNode = constRemove(astNode);
            // Hex to number
            astNode = hexToNumber(astNode);
            // Ts Preparing
            astNode = tsPreparing(astNode);
            // Var Register Cheak
            astNode = varRegisterCheakUses(astNode);
            // Lambda Preparing
            astNode = lambdaPreparing(astNode);
            // Lambda Queue
            foreach (var child in ast.lambdaQueue) astNode.childs.Add(child);
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
            astNode = cmpCheakDelete(astNode);
            // Class ReFresh
            astNode = classReFresh(astNode);
            // Class Inheritances
            astNode = classInheritancesMethods(astNode);
            // Var Cheak
            //astNode = varDeclaratorCheak(astNode, ref astNode);
            // Struct Cheak
            astNode = structCheak(astNode);
            // Struct Inheritances
            astNode = structInheritancesCheak(astNode);
            // General Parse
            //astNode = 
            // Call Cheak
            //astNode = callCheak(astNode);
            astNode = funcCheak(astNode);
            foreach (var cl in ast.classMethods.Values)
                astNode.childs.AddRange(cl);
            // General Parse
            ast.childs.Clear();
            for (int i = 0; i < astNode.childs.Count; i++) ast.childs.Add(astNode.childs[i]);
            ast.childs = parse(ast, 0).childs;
            for (int i = 0; i < ast.childs.Count; i++)
            {
                if (ast.declarotivePatternsStruct.ContainsKey(ast.childs[i].token.value)
                    ) ast.childs[i] = new CommonNode(NT.AIR, astNode.childs[i].token);
            }
            //foreach (var cl in ast.classMethods.Values)
                //ast.childs.AddRange(cl);
            //ast.childs = new List<CommonNode>() { callCheak(ast) };
            CommonNode newAst = callCheak(ast);
            ast.childs = new List<CommonNode>(newAst.childs);
            newAst = varTypePreparing(ast);
            ast.childs = new List<CommonNode>(newAst.childs);
            newAst = funcCheak(ast);
            ast.childs = new List<CommonNode>(newAst.childs);
            newAst = callCheak(ast);
            ast.childs = new List<CommonNode>(newAst.childs);
            newAst = defineCheak(ast);
            ast.childs = new List<CommonNode>(newAst.childs);
            newAst = includeParentsStructs(ast);
            ast.childs = new List<CommonNode>(newAst.childs);
            ast.varTypes = varSpace.VarsData;
            return ast;
        }
        
        // Вспомогательные функции
        private Dictionary<CommonNode, CommonNode> cheakAllVarInLocal(CommonNode root)
        {
            if (root == null) return null;
            if (root.type == NT.VAR && root.childs.Count != 0 && root.childs[0].type == NT.TYPE)
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
        private bool varContains(CommonNode root, Dictionary<CommonNode, CommonNode> vars)
        {
            foreach (var var in vars) if (var.Key.token.value == root.token.value) return true;
            return false;
        }
        private CommonNode getVar(CommonNode root, Dictionary<CommonNode, CommonNode> vars)
        {
            foreach (var var in vars) if (var.Key.token.value == root.token.value) return var.Key;
            return null;
        }
        private CommonNode getVarValue(CommonNode root, Dictionary<CommonNode, CommonNode> vars)
        {
            foreach (var var in vars) if (var.Key.token.value == root.token.value) return var.Value;
            return null;
        }
        private CommonNode functionTypePreparing(CommonNode root)
        {
            Dictionary<string, CommonNode> varsLocal = new Dictionary<string, CommonNode>();
            foreach (CommonNode node in root.childs[1].childs) varsLocal.Add(node.token.value, node.childs[0]);

            List<CommonNode> recurse(CommonNode commonNode)
            {
                List<CommonNode> types = null;
                foreach (var child in commonNode.childs)
                {
                    if (child.childs.Count == 1 && child.childs[0].type == NT.TYPE)
                    {
                        if (types == null) types = new List<CommonNode>();
                        types.Add(child);
                    }
                    else
                    {
                        List<CommonNode> types2 = recurse(child);
                        if (types2 != null)
                        {
                            if (types == null) types = new List<CommonNode>();
                            types.AddRange(types2);
                        }
                    }
                }
                return types;
            }
            List<CommonNode> vars = recurse(root.childs[2]);
            if (vars != null) foreach (var node in vars) varsLocal.Add(node.token.value, node.childs[0]);

            List<CommonNode> parseReturns(CommonNode commonNode)
            {
                List<CommonNode> types = null;
                foreach (var child in commonNode.childs)
                {
                    if (child.type == NT.RETURN)
                    {
                        if (types == null) types = new List<CommonNode>();
                        types.Add(child.childs[0]);
                    }
                    else
                    {
                        List<CommonNode> types2 = parseReturns(child);
                        if (types2 != null)
                        {
                            if (types == null) types = new List<CommonNode>();
                            types.AddRange(types2);
                        }
                    }
                }
                return types;
            }
            List<CommonNode> Returns = parseReturns(root.childs[2]);
            if (Returns == null || Returns.Count == 0)
            {
                root.childs[0].token.value = "void";
                if (!ast.resualtFunc.ContainsKey(root.token.value))
                    ast.resualtFunc.Add(root.token.value, null);
            }
            else
            {
                //CommonNode ReturnType = Returns[0];
                CommonNode ReturnValue = DataBase.getFormulaNodeType(Returns[0], ref varsLocal, ref ast);
                foreach (CommonNode returnNode in Returns)
                {
                    CommonNode ReturnValueLast = DataBase.getFormulaNodeType(returnNode, ref varsLocal, ref ast);
                    if (!(ReturnValueLast._equals(ReturnValue)))
                        Syntax.SyntaxError($"Не все возвращаемые типы Функции: {root.token.value} равны!", returnNode);
                }
                //ReturnType = Returns[0].type;
                CommonNode type = DataBase.getFormulaNodeType(Returns[0], ref varsLocal, ref ast);
                root.childs[0] = type;
                if (!ast.resualtFunc.ContainsKey(root.token.value))
                    ast.resualtFunc.Add(root.token.value, root.childs[0]);
                else ast.resualtFunc[root.token.value] = type;
            }
            // VAR
            // BINOPER
            // FLOATOPER
            // NUMBER
            // PREUNAROPER
            // ADDRESS
            // SIZEOF
            // TYPEOF
            // STRING
            // CHAR
            // BOOL
            // FLOAT
            return root;
        }

        private CommonNode hexToNumber(CommonNode root)
        {
            if (root.type != NT.HEX)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = hexToNumber(root.childs[i]);
                }
                return root;
            }
            root.token.value = root.token.value.Remove(0, 2).Replace("x", "");
            // A   B   C   D   E   F
            //10, 11, 12, 13, 14, 15
            Dictionary<char, int> chars = new Dictionary<char, int>{ {'A', 10}, {'B', 11}, {'C', 12}, {'D', 13}, {'E', 14}, {'F', 15} };

            // hex0F == 15
            int resualt = 0;
            int pow = root.token.value.Length - 1;
            for (int i = 0; i < root.token.value.Length; i++)
            {
                if (chars.ContainsKey(root.token.value[i]))
                    resualt += chars[root.token.value[i]] * (int)Math.Pow(16, pow);
                else resualt += Convert.ToInt32(root.token.value[i].ToString()) * (int)Math.Pow(16, pow);
                pow--;
            }
            root.token.value = Convert.ToString(resualt);
            root.type = NT.NUMBER;

            return root;
        }
        private CommonNode tsPreparing(CommonNode root)
        {
            for (int i = 0; i < root.childs.Count; i++)
            {
                root.childs[i] = tsPreparing(root.childs[i]);
            }
            if (root.type == NT.VAR
                || root.type == NT.TYPE
                || root.type == NT.INDICATOR
                || root.type == NT.CALL
                || root.type == NT.ADDRESS)
                root.token.value = root.token.value.Replace(",", ".");
            return root;
        }
        private CommonNode replaceConstantVarValue(CommonNode root, Dictionary<CommonNode, CommonNode> varsLocal = null)
        {
            if (root.token.value == "=" && root.childs[0].type == NT.VAR
                && varsLocal != null && varContains(root.childs[0], varsLocal)
                && (root.childs[1].type == NT.NUMBER || root.childs[1].type == NT.FLOAT || root.childs[1].type == NT.STRING || root.childs[1].type == NT.CHAR)
                )
            {
                varsLocal[getVar(root.childs[0], varsLocal)] = root.childs[1];
            }
            if (root.type == NT.BINOPER && root.token.value != "=" && root.childs[0].type == NT.VAR && varsLocal != null
                && varContains(root.childs[0], varsLocal) && getVarValue(root.childs[0], varsLocal) != null)
            {
                varsLocal[getVar(root.childs[0], varsLocal)] = null;
            }
            if (root.type == NT.BINOPER && root.token.value != "=" && root.childs[1].type == NT.VAR && varsLocal != null
                && varContains(root.childs[1], varsLocal) && getVarValue(root.childs[1], varsLocal) != null)
            {
                varsLocal[getVar(root.childs[1], varsLocal)] = null;
            }
            if (root.type == NT.BINOPER && root.token.value != "=" && root.childs[0].type == NT.VAR && varsLocal != null
                && varContains(root.childs[0], varsLocal) && getVarValue(root.childs[0], varsLocal) != null)
            {
                root.childs[0] = getVarValue(root.childs[0], varsLocal);
            }
            if (root.type == NT.BINOPER && root.childs[1].type == NT.VAR && varsLocal != null
                && varContains(root.childs[1], varsLocal) && getVarValue(root.childs[1], varsLocal) != null)
            {
                root.childs[1] = getVarValue(root.childs[1], varsLocal);
            }
            for (int i = 0; i < root.childs.Count; i++)
            {
                if (root.type != NT.BINOPER && root.type != NT.FLOATBINOPER && root.childs[i].type == NT.VAR
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
            if (root.type != NT.BINOPER)
            {
                for (int i = 0; i < root.childs.Count; i++) root.childs[i] = binOperCheak(root.childs[i]);
                return root;
            }
            root.childs[0] = binOperCheak(root.childs[0]);
            root.childs[1] = binOperCheak(root.childs[1]);

            CommonNode leftNode = take(root, 0);
            CommonNode rightNode = take(root, 1);

            bool flagFloat = false;

            if (leftNode.type == NT.VAR && varSpace.VarIsType(leftNode.token.value, "float")) flagFloat = true;
            if (rightNode.type == NT.VAR && varSpace.VarIsType(rightNode.token.value, "float")) flagFloat = true;

            if (leftNode.type == NT.VAR && ast.resualtFunc.ContainsKey(leftNode.token.value) && ast.resualtFunc[leftNode.token.value].token.value == "float") flagFloat = true;
            if (rightNode.type == NT.VAR && ast.resualtFunc.ContainsKey(rightNode.token.value) && ast.resualtFunc[rightNode.token.value].token.value == "float") flagFloat = true;

            if (leftNode.type == NT.VAR && ast.typesArgsFunc.ContainsKey(leftNode.token.value) && ast.typesArgsFunc[leftNode.token.value].token.value == "float") flagFloat = true;
            if (rightNode.type == NT.VAR && ast.typesArgsFunc.ContainsKey(rightNode.token.value) && ast.typesArgsFunc[rightNode.token.value].token.value == "float") flagFloat = true;

            if (leftNode.type == NT.FLOATBINOPER) flagFloat = true;
            if (rightNode.type == NT.FLOATBINOPER) flagFloat = true;


            if (leftNode.type == NT.TYPEOPER)
            {
                if (leftNode.token.value == "float") flagFloat = true;
                leftNode = leftNode.childs[0];
            }

            if (rightNode.type == NT.TYPEOPER)
            {
                if (rightNode.token.value == "float") flagFloat = true;
                rightNode = rightNode.childs[0];
            }

            if (flagFloat) root.type = NT.FLOATBINOPER;

            return root;
        }
        private CommonNode binOperReFresh(CommonNode root)
        {
            /*
             * Как и по каким признакам мы выявляем что можно сокротить константы
             * Ну конечно во первых если 2 оператора числа или флоты но это для флотовых
             * 
             */
            if (root.type != NT.BINOPER && root.type != NT.FLOATBINOPER)
            {
                for (int i = 0; i < root.childs.Count; i++) root.childs[i] = binOperReFresh(root.childs[i]);
                return root;
            }
            root.childs[0] = binOperReFresh(root.childs[0]);
            root.childs[1] = binOperReFresh(root.childs[1]);

            CommonNode leftNode = take(root, 0);
            CommonNode rightNode = take(root, 1);

            CommonNode reFreshNode = null;

            if (root.type == NT.FLOATBINOPER)
            {
                if (leftNode.type == NT.FLOAT && rightNode.type == NT.FLOAT)
                    switch (root.token.value)
                    {
                        case "+": reFreshNode = new CommonNode(NT.FLOAT, new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToDouble(leftNode.token.value) + Convert.ToDouble(rightNode.token.value)), root.token.pos)); break;
                        case "-":
                            reFreshNode = new CommonNode(NT.FLOAT, new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToDouble(leftNode.token.value) - Convert.ToDouble(rightNode.token.value)), root.token.pos)); break;
                        case "*":
                            reFreshNode = new CommonNode(NT.FLOAT, new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToDouble(leftNode.token.value) * Convert.ToDouble(rightNode.token.value)), root.token.pos)); break;
                        case "/":
                            reFreshNode = new CommonNode(NT.FLOAT, new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToDouble(leftNode.token.value) / Convert.ToDouble(rightNode.token.value)), root.token.pos)); break;
                        case "%":
                            reFreshNode = new CommonNode(NT.FLOAT, new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToDouble(leftNode.token.value) % Convert.ToDouble(rightNode.token.value)), root.token.pos)); break;
                    }
            } else if (root.type == NT.BINOPER)
            {
                if (leftNode.type == NT.NUMBER && rightNode.type == NT.NUMBER)
                    switch (root.token.value)
                    {
                        case "+":
                            reFreshNode = new CommonNode(NT.NUMBER, new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToInt32(leftNode.token.value) + Convert.ToInt32(rightNode.token.value)), root.token.pos)); break;
                        case "-":
                            reFreshNode = new CommonNode(NT.NUMBER, new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToInt32(leftNode.token.value) - Convert.ToInt32(rightNode.token.value)), root.token.pos)); break;
                        case "*":
                            reFreshNode = new CommonNode(NT.NUMBER, new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToInt32(leftNode.token.value) * Convert.ToInt32(rightNode.token.value)), root.token.pos)); break;
                        case "/":
                            reFreshNode = new CommonNode(NT.NUMBER, new Token(leftNode.token.type,
                    Convert.ToString(Convert.ToInt32(leftNode.token.value) / Convert.ToInt32(rightNode.token.value)), root.token.pos)); break;
                        case "%":
                            reFreshNode = new CommonNode(NT.NUMBER, new Token(leftNode.token.type,
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
            if (root.type != NT.CMP)
            {
                for (int i = 0; i < root.childs.Count; i++) root.childs[i] = cmpReFresh(root.childs[i]);
                return root;
            }
            if (root.childs.Count == 1)
            {
                if (root.childs[0].type == NT.BOOL) root.token.value = root.childs[0].token.value;
                return root;
            }
            root.childs[0] = cmpReFresh(root.childs[0]);
            root.childs[1] = cmpReFresh(root.childs[1]);

            CommonNode leftNode = take(root, 0);
            CommonNode rightNode = take(root, 1);

            if (leftNode.type == NT.CMP && (leftNode.token.value == "true") &&
                rightNode.type == NT.CMP && (rightNode.token.value == "true")) return new CommonNode(root.type, new Token(root.token.type, "true", root.token.pos));
            if (leftNode.type == NT.CMP && (leftNode.token.value == "false") &&
                rightNode.type == NT.CMP && (rightNode.token.value == "false")) return new CommonNode(root.type, new Token(root.token.type, "false", root.token.pos));
            if (leftNode.type == NT.CMP && rightNode.type == NT.CMP && root.token.value == "&&") return new CommonNode(root.type, new Token(root.token.type, "false", root.token.pos));
            if (leftNode.type == NT.CMP && rightNode.type == NT.CMP && root.token.value == "||") return new CommonNode(root.type, new Token(root.token.type, "true", root.token.pos));

            bool cmpB = false;
            bool cmp = false;
            decimal leftValue = decimal.Zero;
            decimal rightValue = decimal.Zero;

            if (leftNode.type == NT.FLOAT) leftNode.token.value = leftNode.token.value.Replace(".", ",");
            if (rightNode.type == NT.FLOAT) rightNode.token.value = rightNode.token.value.Replace(".", ",");

            if (leftNode.type == NT.FLOAT) leftNode.token.value = leftNode.token.value.Replace("f", "");
            if (rightNode.type == NT.FLOAT) rightNode.token.value = rightNode.token.value.Replace("f", "");

            if (leftNode.type == NT.NUMBER || leftNode.type == NT.FLOAT) leftValue = Convert.ToDecimal(leftNode.token.value);
            if (rightNode.type == NT.NUMBER || rightNode.type == NT.FLOAT) rightValue = Convert.ToDecimal(rightNode.token.value);

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
        private CommonNode cmpCheakDelete(CommonNode root)
        {
            if (root.type != NT.IF)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = cmpCheakDelete(root.childs[i]);
                }
                return root;
            }

            CommonNode cmpNode = take(root, 0);
            CommonNode bodyNode = take(root, 1);

            if (cmpNode.token.value == "false") return new CommonNode(NT.AIR, root.token);
            if (cmpNode.token.value == "true") return bodyNode;

            return root;
        }
        private CommonNode classReFresh(CommonNode root)
        {
            if (root.type != NT.CLASS)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = classReFresh(root.childs[i]);
                }
                return root;
            }

            List<CommonNode> varNodes = new List<CommonNode>();
            List<CommonNode> methodNodes = new List<CommonNode>();

            CommonNode constructorNode = null;
            CommonNode destructorNode = null;
            CommonNode declarationNode = null;

            foreach (CommonNode node in root.childs)
            {
                if (node.type == NT.VAR) varNodes.Add(node);
                else if (node.type == NT.FUNC) methodNodes.Add(node);
                else if (node.type == NT.CONSTRUCTOR) constructorNode = node;
                else if (node.type == NT.DESTRUCTOR) destructorNode = node;
                else if (node.type == NT.DECLARATOR) declarationNode = node;
            }
            /*if (ast.ClassesInheritances.ContainsKey(root.token.value)
                && ast.classMethods.ContainsKey(ast.ClassesInheritances[root.token.value])
                && ast.classMethods[ast.ClassesInheritances[root.token.value]].Count != 0)
            {
                foreach (var child in ast.classMethods[ast.ClassesInheritances[root.token.value]])
                {
                    methodNodes.Add(child);
                }
            }*/

            // Что мы тут должны сделать с членами класса
            // * Все переменные переместить в структуру
            // * методы преобразовать в void print () {}   -> void print (ClASS this) {}
            // * Конструкторы и деструкторы пока что не трогаем

            CommonNode strt = new CommonNode(NT.STRUCT, root.token);
            ast.declarotiveClassVars.Add(root.token.value, new List<CommonNode>());
            if (declarationNode != null)
            {
                strt.childs.Add(declarationNode);
            }
            foreach (CommonNode node in varNodes)
            {
                strt.childs.Add(node);
                ast.declarotiveClassVars[root.token.value].Add(node);
            }
            //if (strt.childs.Count == 0)
            //{
            //    strt.childs.Add(new CommonNode(NT.VAR, new Token(TT.VAR, "value", strt.token.pos)));
            //    strt.childs[0].childs.Add(new CommonNode(NT.TYPE, new Token(TT.VAR, "int32", strt.token.pos)));
            //}
            if (declarationNode != null) ast.declarotivePatternsStruct.Add(strt.token.value,  strt);
            if (declarationNode != null) ast.declarativeClassNames.Add(strt.token.value);

            foreach (CommonNode node in methodNodes)
            {
                CommonNode newRoot = node;
                if (declarationNode == null) newRoot.token.value += "_" + root.token.value;
                CommonNode signatureFuncNode = newRoot.childs[1];
                List<CommonNode> commonNodes = new List<CommonNode>();
                
                foreach (CommonNode cn in signatureFuncNode.childs) commonNodes.Add(cn);
                commonNodes.Add(new CommonNode(NT.VAR, new Token(TT.VAR, "this", signatureFuncNode.token.pos)));
                commonNodes.Last().childs.Add(new CommonNode(NT.TYPE, new Token(TT.VAR, root.token.value, signatureFuncNode.token.pos)));
                signatureFuncNode.childs = commonNodes;
                if (declarationNode == null) ast.resualtFunc.Add(newRoot.token.value, newRoot.childs[0]);
                //ast.typesArgsFunc.Add(node.token.value, signatureFuncNode);
            }
            if (declarationNode == null) ast.classMethods.Add(root.token.value, methodNodes);
            else ast.declarotiveClassMethods.Add(root.token.value, methodNodes);

            return strt;
        }
        private CommonNode classInheritancesMethods(CommonNode root)
        {
            if (root.type != NT.STRUCT)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = classInheritancesMethods(root.childs[i]);
                }
                return root;
            }

            if (ast.ClassesInheritances.ContainsKey(root.token.value)
                && ast.classMethods.ContainsKey(ast.ClassesInheritances[root.token.value])
                && ast.classMethods[ast.ClassesInheritances[root.token.value]].Count != 0)
            {
                var reflist = ast.classMethods[ast.ClassesInheritances[root.token.value]];
                List<CommonNode> list = new List<CommonNode>();
                foreach (var e in reflist)
                    list.Add(copyNodes(e));

                foreach (var child in list)
                {
                    bool _is = false;
                    child.token.value = child.token.value.Replace($"_{ast.ClassesInheritances[root.token.value]}", $"_{root.token.value}");
                    foreach (CommonNode v in ast.classMethods[root.token.value])
                        if (v.token.value == child.token.value) _is = true;
                    if (_is) continue;
                    
                    child.childs[1].childs.Last().childs[0].token.value = root.token.value;
                    //Console.WriteLine(child.token.value);
                    ast.resualtFunc.Add(child.token.value, child.childs[0]);
                    ast.classMethods[ast.ClassesInheritances[root.token.value]].Add(child);
                }
            }

            return root;
        }

        private CommonNode structCheak(CommonNode root)
        {
            if (root.type != NT.STRUCT)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = structCheak(root.childs[i]);
                }
                return root;
            }
            //ast.structs.Add
            //parseStruct(root, 0);
            if (ast.declarotivePatternsStruct.Keys.Contains(root.token.value)) return new CommonNode(NT.AIR, root.token);
            //if (ast.declarotiveNames.Contains(root.token.value)) return new CommonNode(NT.AIR, root.token);
            Dictionary<string, CommonNode> typesVar = new Dictionary<string, CommonNode>();
            foreach (CommonNode var in root.childs)
            {
                typesVar.Add(var.token.value, var.childs[0]);
            }
            ast.structs.Add(root.token.value, typesVar);
            
            return root;
            //return parseStruct(root, 0);
        }
        private CommonNode structInheritancesCheak(CommonNode root)
        {

            if (root.type != NT.STRUCT)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = structInheritancesCheak(root.childs[i]);
                }
                return root;
            }

            if (ast.ClassesInheritances.ContainsKey(root.token.value))
            {
                foreach (var child in ast.structs[ast.ClassesInheritances[root.token.value]])
                {
                    if (ast.structs[root.token.value].ContainsKey(child.Key)) continue;
                    ast.structs[root.token.value].Add(child.Key, child.Value);
                    CommonNode varNode = new CommonNode(NT.VAR, new Token(TT.NULL, child.Key, child.Value.token.pos));
                    varNode.childs.Add(child.Value);
                    root.childs.Add(varNode);
                }
            }

            return root;
        }
        private CommonNode varDeclaratorCheak(CommonNode root, ref CommonNode _ast)
        {
            if (root.type != NT.VAR)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = varDeclaratorCheak(root.childs[i], ref _ast);
                }
                return root;
            }

            if (root.childs.Count > 0)
            {
                CommonNode type = root.childs[0];

                if (type.childs.Count > 0 && root.childs[0].type != NT.OFFSET)
                {
                    type.token.value = generationDeclarationStruct(type, type.childs[0]);
                    _ast.childs.Add(ast.childs[ast.childs.Count - 1]);
                    ast.childs.RemoveAt(ast.childs.Count - 1);
                    type.childs.Clear();
                    root.childs[0].childs.Clear();
                    root.childs[0].token.value = type.token.value;
                }
            }
            if (ast.declarotivePatternsStruct.ContainsKey(root.token.value)) return new CommonNode(NT.AIR, root.token);
            return root;
        }
        private CommonNode callCheak(CommonNode root)
        {
            if (root.type != NT.CALL && root.type != NT.CALLADDRESS)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = callCheak(root.childs[i]);
                }
                return root;
            }

            return parseCall(root, 0);
        }
        private CommonNode funcCheak(CommonNode root)
        {
            if (root.type != NT.FUNC)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = funcCheak(root.childs[i]);
                }
                return root;
            }
            if (root.childs[0].token.value != "function") return root;

            root = functionTypePreparing(root);

            return root;
        }
        private CommonNode defineCheak(CommonNode root)
        {
            if (root.type != NT.TYPEIF)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = defineCheak(root.childs[i]);
                }
                return root;
            }

            CommonNode typeFirstNode = root.childs[0];
            CommonNode typeSecondNode = root.childs[1];
            CommonNode bodyNode = root.childs[2];

            if (typeFirstNode.token.value == typeSecondNode.token.value)
                return bodyNode;
            return new CommonNode(NT.AIR, root.token);
        }
        private CommonNode constRemove(CommonNode root)
        {
            if (root.type != NT.VAR)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = constRemove(root.childs[i]);
                }
                return root;
            }

            if (ast.consts.ContainsKey(root.token.value)) return ast.consts[root.token.value];
            return root;
        }
        private CommonNode includeParentsStructs(CommonNode root)
        {
            if (root.type != NT.STRUCT)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = includeParentsStructs(root.childs[i]);
                }
                return root;
            }
            if (ast.parentsStructs.ContainsKey(root.token.value))
            {
                Dictionary<string, CommonNode> varsStructParent = ast.structs[ast.parentsStructs[root.token.value].token.value];
                Dictionary<string, CommonNode> varsStruct  = ast.structs[root.token.value];
                Console.WriteLine($"Struct {root.token.value}-> Count: {root.childs.Count}");
                Console.WriteLine($"Struct p {ast.parentsStructs[root.token.value].token}-> Count: {ast.structs[ast.parentsStructs[root.token.value].token.value].Count}");
                Console.WriteLine($"Struct v {root.token.value}-> Count: {ast.structs[root.token.value].Count}");
                foreach (var child in varsStructParent)
                {
                    varsStruct.Add(child.Key, child.Value);
                    //root.childs.Add(new CommonNode(child.Key, new Token(child.Value.token.type, child.Key, child.Value.token.pos)));
                    //root.childs.Last().childs.Add(child.Value);
                    root.childs.Add(child.Value);
                    Console.WriteLine(" :---: "+child.Value);
                    Console.WriteLine($"Struct {root.token.value}-> Key: {child.Key} Value: {child.Value.token.value}");
                }
                Console.WriteLine($"Struct {root.token.value}-> Count: {root.childs.Count}");
                Console.WriteLine($"Struct v {root.token.value}-> Count: {ast.structs[root.token.value].Count}");
                ast.structs[root.token.value] = varsStruct;
                CommonNode strt = new CommonNode(NT.STRUCT, root.token);
                Console.WriteLine(varsStruct.Count);
                Console.WriteLine(varsStructParent.Count);
                foreach (var child in varsStruct) strt.childs.Add(child.Value);
                Console.WriteLine(strt.childs.Count);
                return root;
            }
            //else return root;
            return root;
        }

        private CommonNode varRegisterCheakUses(CommonNode root)
        {
            if (root.type == NT.VAR && varRegisters.Contains(root.token.value))
            {
                root.type = NT.REGUSE;
            } else if (root.type == NT.REGDECL && !varRegisters.Contains(root.token.value))
            {
                varRegisters.Add(root.token.value);
            } else if (root.type != NT.REGDECL)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = varRegisterCheakUses(root.childs[i]);
                }
                return root;
            }

            return root;
        }
        private CommonNode lambdaPreparing(CommonNode root)
        {
            if (root.type != NT.LAMBDA)
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = lambdaPreparing(root.childs[i]);
                }
                return root;
            }

            if (root.childs.Count > 1)
            {
                CommonNode signatureCallNode = take(root, 0);
                CommonNode templeteNode = take(root, 1);

                templeteNode.token.value = $"lambda{lambdaIndex++}";
                templeteNode.type = NT.FUNC;
                templeteNode = functionTypePreparing(templeteNode);
                ast.lambdaQueue.Add(templeteNode);

                CommonNode callNode = new CommonNode(NT.CALL, new Token(TT.NULL, templeteNode.token.value, signatureCallNode.token.pos));
                callNode.childs.Add(signatureCallNode);
                return callNode;
            }
            else
            {
                CommonNode templeteNode = take(root, 0);

                templeteNode.token.value = $"lambda{lambdaIndex++}";
                templeteNode.type = NT.FUNC;
                templeteNode = functionTypePreparing(templeteNode);
                ast.lambdaQueue.Add(templeteNode);

                CommonNode addressNode = new CommonNode(NT.ADDRESS, new Token(TT.NULL, "&", templeteNode.token.pos));
                CommonNode callNode = new CommonNode(NT.CALLADDRESS, templeteNode.token);
                addressNode.childs.Add(callNode);
                return addressNode;
            }

            return root;
        }
        private CommonNode varTypePreparing(CommonNode root)
        {
            if (root.type == NT.VARDECL)
            {
                CommonNode varNode = take(root, 0);
                CommonNode exprNode = take(root, 1);

                varNode.childs[0] = DataBase.getFormulaNodeType(exprNode, ref varSpace, ref ast);
                root.childs[0] = varNode;
                root.type = NT.BINOPER;
                if (!varSpace.ContainsKey(varNode.token.value)) varSpace.AddVar(varNode.token.value, varNode.childs[0]);
                //Program.PrintAST(root, 0);
                return root;
            }
            else if (root.type == NT.VAR && root.childs.Count > 0 && root.childs[0].type == NT.TYPE)
            {
                if (!varSpace.ContainsKey(root.token.value)) varSpace.AddVar(root.token.value, root.childs[0]);
            }
            else
            {
                varSpace.OpenSpace();
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = varTypePreparing(root.childs[i]);
                }
                varSpace.CloseSpace();
                return root;
            }
            return root;
        }

        private CommonNode parse(CommonNode root, int z_buffer)
        {
            switch (root.type)
            {
                case NT.INLINE:
                    return parseInline(root, z_buffer);
                case NT.FUNC:
                    return parseFunc(root, z_buffer);
                case NT.CALL:
                    return parseCall(root, z_buffer);
                case NT.CALLADDRESS:
                    return parseCallAddress(root, z_buffer);
                case NT.STRUCT:
                    return parseStruct(root, z_buffer);
                case NT.CLASS:
                    return parseClass(root, z_buffer);
                case NT.USING:
                    return parseUsing(root, z_buffer);
                case NT.VAR:
                    return parseVar(root, z_buffer);
                case NT.MODIFIER:
                    return parseModifier(root, z_buffer);
                case NT.FOR:
                    return parseFor(root, z_buffer);
                case NT.ENUMERATOR:
                    return parseEnumerator(root, z_buffer);
                //case NT.ADDRESS:
                //    return root;
                default:
                    if (root.childs.Count == 0)
                        break;
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        //if (root.childs[i].type != NT.ADDRESS)
                        root.childs[i] = parse(root.childs[i], z_buffer + 1);
                    }
                    return root;
            }
            return root;
        }
        /*private CommonNode parseBinOper(CommonNode root, int z_buffer)
        {
            CommonNode body = take(root, 1);
            for (int i = 0; i < body.childs.Count; i++)
            {
                body.childs[i] = parse(body.childs[i], z_buffer + 2);
                if (body.childs[i].type == NT.RETURN)
                    throw new Exception("Ошибка в инлайн функции не может быть return");
            }
            ast.inlineNames.Add(root.token.value);
            return root;
        }*/
        private CommonNode parseInline(CommonNode root, int z_buffer)
        {
            CommonNode body = take(root, 1);
            for (int i = 0; i < body.childs.Count; i++)
            {
                body.childs[i] = parse(body.childs[i], z_buffer + 2);
                if (body.childs[i].type == NT.RETURN)
                    throw new Exception("Ошибка в инлайн функции не может быть return");
            }
            ast.inlineNames.Add(root.token.value);
            return root;
        }
        private CommonNode parseFunc(CommonNode root, int z_buffer)
        {
            if (ast.declarotivePatternsFunctions.Keys.Contains(root.token.value)) return new CommonNode(NT.AIR, root.token);
            CommonNode signature = take(root, 1);
            CommonNode bodyFunc = take(root, 2);
            List<CommonNode> childs = new List<CommonNode>();
            for (int i = 0; i < signature.childs.Count; i++)
            {
                CommonNode type = signature.childs[i].childs[0];

                if (type.childs.Count > 0 && root.childs[0].type != NT.OFFSET)
                {
                    type.token.value = generationDeclarationStruct(type, type.childs[0]);
                    type.childs.Clear();
                    signature.childs[i].childs[0] = type;
                }
            }
            if (ast.resualtFunc[root.token.value] != null && ast.resualtFunc[root.token.value].token.value != "void" && !DataBase.types.ContainsKey(ast.resualtFunc[root.token.value].token.value) && ast.resualtFunc[root.token.value].type != NT.INDICATOR)
            {
                CommonNode resualtVar = new CommonNode(NT.VAR, new Token(TT.NULL, "resualtPtr", signature.token.pos));
                resualtVar.childs.Add(ast.resualtFunc[root.token.value]);
                childs.Add(resualtVar);
            }

            varSpace.OpenSpace();
            foreach (CommonNode child in signature.childs)
            {
                //Console.WriteLine($"Func Signature Var : {child.token.value} | {child.childs[0].token.value}");
                try
                {
                    varSpace.AddVar(child.token.value, child.childs[0]);
                } catch
                {
                    Program.PrintAST(ast, 0);
                    Console.ReadKey();
                }
            }
            for (int i = 0; i < bodyFunc.childs.Count; i++)
            {
                bodyFunc.childs[i] = parse(bodyFunc.childs[i], z_buffer + 2);
            }
            varSpace.CloseSpace();
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
                if (root.childs[i].type != NT.VAR) root.childs[i] = parse(root.childs[i], z_buffer + 1);
            }
            return root;
        }
        private CommonNode parseStruct(CommonNode root, int z_buffer)
        {
            if (ast.declarotivePatternsStruct.Keys.Contains(root.token.value)) return new CommonNode(NT.AIR, root.token);
            Dictionary<string, CommonNode> typesVar = new Dictionary<string, CommonNode>();
            foreach (CommonNode var in root.childs)
            {
                typesVar.Add(var.token.value, var.childs[0]);
            }
            if (!ast.structs.ContainsKey(root.token.value)) ast.structs.Add(root.token.value, typesVar);
            return root;
        }
        private CommonNode parseUsing(CommonNode root, int z_buffer)
        {
            if (root.childs.Count != 2) return root;
            CommonNode name = take(root, 0);
            CommonNode mode = take(root, 1);
            if (mode.type == NT.INLINE && name.childs.Count == 0) ast.inlineNames.Add(name.token.value);
            else if (mode.type == NT.INLINE)
            {
                foreach (CommonNode childName in name.childs) ast.inlineNames.Add(childName.token.value);
            }
            return new CommonNode(NT.AIR, root.token);
        }
        private CommonNode parseCallAddress(CommonNode root, int z_buffer)
        {
            try
            {
                string[] strs = root.token.value.Split('.');
                string type = string.Empty;
                type = varSpace.GetTypeValue(strs[0]);
                for (int i = 1; i < strs.Length - 1; i++)
                    type = ast.structs[type][strs[i]].token.value;
                if (ast.classMethods.ContainsKey(type))
                {
                    string name = string.Empty;
                    for (int i = 1; i < strs.Length - 1; i++) name += strs[i];
                    name += "_" + type;
                    name.Remove(0, 1);
                    List<CommonNode> argsNew = new List<CommonNode>();
                    foreach (CommonNode node in root.childs[0].childs) argsNew.Add(node);
                    argsNew.Add(new CommonNode(NT.VAR, new Token(TT.VAR, strs[0], root.childs[0].token.pos)));
                    root.childs[0].childs = argsNew;
                    root.token.value = strs[strs.Length - 1] + "_" + type;
                }
            }
            catch { }

            if (ast.declarotivePatternsFunctions.Keys.Contains(root.token.value)) root = generationDeclarationFunc(root);
            if (take(root, 0).type == NT.DECLARATOR) Syntax.SyntaxError("Ошибка использывание не декларотивную функцию как декларотивную!", root);

            return root;
        }
        private CommonNode parseCall(CommonNode root, int z_buffer)
        {
            if (ast.functionOver.ContainsKey(root.token.value) && ast.functionOver[root.token.value].Count > 1)
            {
                string name = root.token.value;
                for (int i = 0; i < ast.functionOver[root.token.value].Count; i++)
                {
                    if (ast.functionOver[root.token.value][i].childs[1].childs.Count == root.childs[0].childs.Count)
                    {
                        name = ast.functionOver[root.token.value][i].token.value;
                        bool flag = true;
                        for (int j = 0; j < ast.functionOver[root.token.value][i].childs[1].childs.Count; j++)
                        {
                            string childValue = root.childs[0].childs[j].token.value;
                            NT childType = root.childs[0].childs[j].type;
                            string functionType = ast.functionOver[root.token.value][i].childs[1].childs[j].token.value;
                            switch (childType)
                            {
                                case NT.VAR:
                                case NT.POSTUNAROPER:
                                case NT.PREUNAROPER:
                                    if (varSpace.GetTypeValue(childValue) != functionType) flag = false;
                                    break;
                                case NT.SIZEOF:
                                case NT.TYPEOF:
                                case NT.NUMBER:
                                case NT.ADDRESS:
                                case NT.BINOPER:
                                    if (!DataBase.types.ContainsKey(functionType)) flag = false;
                                    break;
                                case NT.FLOAT:
                                case NT.FLOATOPER:
                                    if (functionType != "float" && functionType != "double") flag = false;
                                    break;
                                case NT.STRING:
                                    if (functionType != "STRING") flag = false;
                                    break;
                                case NT.CHAR:
                                    if (functionType != "char" && functionType != "byte" && functionType != "int8") flag = false;
                                    break;
                                case NT.BOOL:
                                    if (functionType != "bool") flag = false;
                                    break;
                                case NT.TYPEOPER:
                                    if (functionType != childValue) flag = false;
                                    break;
                                case NT.CALL:
                                    if (functionType != ast.resualtFunc[childValue].token.value) flag = false;
                                    break;
                            }
                            if (!flag) break;
                        }
                        if (flag) name = ast.functionOver[root.token.value][i].token.value;
                    }
                }
                root.token.value = name;
            }

            foreach (var library in ast.externLibrarys)
            {
                if (ast.externFuncs[library.Key].Contains(root.token.value))
                {
                    foreach (var child in root.childs) parse(child, z_buffer + 1);
                    return root;
                }
            }
            try
            {
                string[] strs = root.token.value.Split('.');
                string type = string.Empty;
                type = varSpace.GetTypeValue(strs[0]);
                for (int i = 1; i < strs.Length-1; i++)
                    type = ast.structs[type][strs[i]].token.value;
                if (ast.classMethods.ContainsKey(type))
                {
                    string name = string.Empty;
                    for (int i = 1; i < strs.Length-1; i++) name += strs[i];
                    name += "_" + type;
                    name.Remove(0, 1);
                    List<CommonNode> argsNew = new List<CommonNode>();
                    foreach (CommonNode node in root.childs[0].childs) argsNew.Add(node);
                    argsNew.Add(new CommonNode(NT.VAR, new Token(TT.VAR, strs[0], root.childs[0].token.pos)));
                    root.childs[0].childs = argsNew;
                    root.token.value = strs[strs.Length-1] + "_" + type;
                }
            } catch { }
            if (ast.declarotivePatternsFunctions.Keys.Contains(root.token.value)) root = generationDeclarationFunc(root);
            if (ast.resualtFunc.ContainsKey(root.token.value) && take(root, 0).type == NT.DECLARATOR) Syntax.SyntaxError("Ошибка использывание не декларотивную функцию как декларотивную!", root);

            CommonNode signatureCall = take(root, 0);
           
            for (int j = 0; j < signatureCall.childs.Count; j++)
                signatureCall.childs[j] = parse(signatureCall.childs[j], z_buffer + 2);
            root.childs[0] = signatureCall;
            return root;
        }
        private CommonNode parseVar(CommonNode root, int z_buffer)
        {
            if (root.childs.Count > 0)
            {
                if (!root.token.value.Contains(".") && root.childs[0].type != NT.OFFSET && varSpace.ContainsKey(root.token.value))
                    Syntax.SyntaxError($"В текущей области видимости Переменная: {root.token.value} уже объявлена!", root);

                CommonNode type = root.childs[0];
                

                if (type.childs.Count > 0 && root.childs[0].type != NT.OFFSET)
                {
                    CommonNode _ast = ast;
                    string oldType = type.token.value;
                    string oldType2 = type.token.value;
                    Dictionary<string, CommonNode> declaratorTypes = new Dictionary<string, CommonNode>();
                    //Console.ReadKey();
                    type.token.value = generationDeclarationStruct(type, type.childs[0], ref declaratorTypes);
                    oldType = type.token.value;
                    if (ast.declarativeClassNames.Contains(oldType2))
                    {
                        //Console.WriteLine("CLASS DECLARATION " + oldType2 + " | " + oldType);
                        //foreach (var c in declaratorTypes)
                        //{
                        //    Console.WriteLine($"{c.Key} - {c.Value.token.value} - {c.Value.type}");
                        //}
                        //Console.ReadKey();
                        ast.structs.Add(oldType, new Dictionary<string, CommonNode>());
                        foreach (var child in ast.declarotiveClassVars[oldType2])
                        {
                            //Program.PrintAST(child, 0);
                            CommonNode newType = replaceNodes(child.childs[0], ref declaratorTypes);
                            ast.structs[oldType].Add(child.token.value, newType);
                            //Console.WriteLine($"{newType.token.value} -- {oldType} -- {child.token.value} -- {newType.type}");
                            //Console.ReadKey();
                        }
                        ast.classMethods.Add(oldType, new List<CommonNode>());
                        foreach (CommonNode child in ast.declarotiveClassMethods[oldType2])
                        {
                            CommonNode newMethod = replaceNodes(child, ref declaratorTypes);
                            newMethod.token.value += "_" + type.token.value;
                            newMethod.childs[1].childs.Last().childs[0].token.value = type.token.value;
                            ast.classMethods[oldType].Add(newMethod);
                            ast.resualtFunc.Add(
                                newMethod.token.value,
                                newMethod.childs[0]
                                );
                        }
                        foreach (var child in ast.classMethods[oldType])
                        {
                            ast.childs.Add(child);
                        }
                    }
                    type.childs.Clear();
                    root.childs[0] = type;
                }
                if (root.childs[0].type != NT.OFFSET)
                {
                    if (varSpace.VarsSpaces.Count == 0) varSpace.VarsData.Add(root.token.value, type);
                    else varSpace.AddVar(root.token.value, type);
                }
            }
            return root;
        }
        private CommonNode parseFor(CommonNode root, int z_buffer)
        {
            varSpace.OpenSpace();
            CommonNode recurse(CommonNode commonNode)
            {
                foreach (var child in commonNode.childs)
                {
                    if (child.childs.Count == 1 && child.childs[0].type == NT.TYPE) return child;
                    return recurse(child);
                }
                return null;
            }
            CommonNode varNode = recurse(take(root, 0));
            varSpace.AddVar(varNode.token.value, varNode.childs[0]);
            if (varNode == null) { Program.PrintAST(root, 0); Syntax.SyntaxError("Ошибка в объявлениии переменной в цикле For", root); }
            parse(take(root, 3), z_buffer + 1);
            varSpace.CloseSpace();
            return root;
        }
        private CommonNode parseEnumerator(CommonNode root, int z_buffer)
        {
            varSpace.OpenSpace();
            CommonNode varNode = take(root, 0);
            varSpace.AddVar(varNode.token.value, varNode.childs[0]);
            foreach (var child in take(root, 2).childs) parse(child, z_buffer + 2);
            varSpace.CloseSpace();
            return root;
        }
        private CommonNode parseClass(CommonNode root, int z_buffer)
        {
            if (ast.declarotivePatternsStruct.Keys.Contains(root.token.value)) return new CommonNode(NT.AIR, root.token);
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
        private CommonNode replaceNodes (CommonNode rootNode, ref Dictionary<string, CommonNode> table)
        {
            CommonNode root = new CommonNode(rootNode.type, new Token(rootNode.token.type, rootNode.token.value, rootNode.token.pos));
            root.childs = new List<CommonNode>();

            if (table.Keys.Contains(root.token.value))
            {
                root.type = table[root.token.value].type;
                root.token.value = table[root.token.value].token.value;
            }
            for (int i = 0; i < rootNode.childs.Count; i++)
            {
                if (root.type == NT.TYPE && rootNode.childs[i].type == NT.DECLARATOR)
                {
                    //Console.WriteLine($"{root.token.value} + {root.type}");

                    CommonNode replace = replaceNodes(rootNode.childs[i], ref table);
                    string name = generationDeclarationStruct(root, replace);
                    replace.token.value = name;
                    root = replace;
                    root.type = NT.TYPE;
                    root.childs.Clear();
                    continue;
                }
                if (root.type == NT.INDICATOR)
                {
                    //Console.ReadKey(); Console.WriteLine("REPLACE INDICATOR");
                    CommonNode replace = replaceNodes(rootNode.childs[i], ref table);
                    string name = generationDeclarationStruct(root, replace);
                    replace.token.value = name;
                    root = replace;
                    root.type = NT.INDICATOR;
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
            if (declarator.childs.Count > 0 && declarator.childs[0].childs.Count > 0 && declarator.childs[0].childs[0].type == NT.DECLARATOR)
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

            Dictionary<string, CommonNode> declaratorTypes = new Dictionary<string, CommonNode>();
            for (int i = 0; i < patternDeclarator.childs.Count; i++)
            {
                CommonNode declaratorString = declarator.childs[i];
                if (secondDeclarator != string.Empty) declaratorString.token.value = secondDeclarator; // declarator.childs[i].token.value + "_" + 
                declaratorTypes.Add(patternDeclarator.childs[i].token.value, declaratorString);
            }
            ast.declarotiveNames.Add(newName);
            CommonNode newStruct = replaceNodes(pattern, ref declaratorTypes);
            newStruct.childs.RemoveAt(0);
            newStruct.token.value = newName;

            ast.childs.Add(newStruct);

            return newName;
        }
        private string generationDeclarationStruct(CommonNode type, CommonNode declarator, ref Dictionary<string, CommonNode> types)
        {
            string secondDeclarator = string.Empty;
            if (declarator.childs.Count > 0 && declarator.childs[0].childs.Count > 0 && declarator.childs[0].childs[0].type == NT.DECLARATOR)
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

            Dictionary<string, CommonNode> declaratorTypes = new Dictionary<string, CommonNode>();
            for (int i = 0; i < patternDeclarator.childs.Count; i++)
            {
                CommonNode declaratorString = declarator.childs[i];
                if (secondDeclarator != string.Empty) declaratorString.token.value = secondDeclarator; // declarator.childs[i].token.value + "_" + 
                declaratorTypes.Add(patternDeclarator.childs[i].token.value, declaratorString);
                //Console.WriteLine($"DECLAR : {declaratorString.token.value} -- {declaratorString.type}");
            }
            ast.declarotiveNames.Add(newName);
            CommonNode newStruct = replaceNodes(pattern, ref declaratorTypes);
            newStruct.childs.RemoveAt(0);
            newStruct.token.value = newName;

            ast.childs.Add(newStruct);
            types = declaratorTypes;
            return newName;
        }
        private CommonNode generationDeclarationFunc(CommonNode root)
        {
            string oldName = root.token.value;
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
                newName += $"_{generationDeclarationType(declarator.childs[i]).token.value}";
                //Console.WriteLine("\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\NAME : " + newName);
            }
            root.token.value = newName;
            root.childs.RemoveAt(0);
            if (ast.declarotiveNames.Contains(newName)) return root;

            Dictionary<string, CommonNode> declaratorTypes = new Dictionary<string, CommonNode>();
            for (int i = 0; i < patternDeclarator.childs.Count; i++)
            {
                declaratorTypes.Add(patternDeclarator.childs[i].token.value, generationDeclarationType(declarator.childs[i]));
            }
            ast.declarotiveNames.Add(newName);
            CommonNode newFunc = replaceNodes(pattern, ref declaratorTypes);
            newFunc.childs.RemoveAt(newFunc.childs.Count-1);
            ast.resualtFunc.Add(newName, take(newFunc, 0));
            newFunc.token.value = newName;

            if (ast.qsFunction.Contains(oldName)) ast.qsFunction.Add(newName);
            ast.childs.Add(newFunc);
            
            return root;
        }
        private CommonNode generationDeclarationType(CommonNode declarator)
        {
            //Console.WriteLine("@@!!--");
            //Console.WriteLine($"@@  {declarator.token.value}  @@");
            if (declarator.childs.Count == 0)
                return declarator;

            string result = string.Empty;
            result += declarator.token.value;
            CommonNode final = declarator;
            declarator = take(declarator, 0);
            foreach (var ch in declarator.childs)
            {
                result += "_" + generationDeclarationType(ch).token.value;
                //Console.WriteLine($"@@  {result}  @@");
            }
            final.childs.Clear();
            final.token.value = result;
            return final;
        }
    }
}
