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

        public List<string> strings = new List<string>();

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
                    ) ast.childs[i] = new CommonNode("AIR", astNode.childs[i].token);
            }
            //foreach (var cl in ast.classMethods.Values)
                //ast.childs.AddRange(cl);
            //ast.childs = new List<CommonNode>() { callCheak(ast) };
            CommonNode newAst = callCheak(ast);
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
        private string getTypeFromNode(CommonNode root, Dictionary<string, CommonNode> vars)
        {
            string type = string.Empty;
            switch (root.type)
            {
                case "VAR":
                case "POSTUNAROPER":
                case "PREUNAROPER":
                    type = vars[root.token.value].token.value;
                    break;
                case "SIZEOF":
                case "TYPEOF":
                case "NUMBER":
                case "ADDRESS":
                case "BINOPER":
                    type = "long";
                    break;
                case "FLOAT":
                case "FLOATOPER":
                    type = "float";
                    break;
                case "STRING":
                    type = "string";
                    break;
                case "CHAR":
                    type = "char";
                    break;
                case "BOOL":
                    type = "bool";
                    break;
                case "TYPEOPER":
                    type = root.token.value;
                    break;
                case "CALL":
                    type = ast.resualtFunc[root.token.value].token.value;
                    break;
                default:
                    type = "long";
                    break;
            }
            return type;
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

            if (leftNode.type == "VAR" && varSpace.VarIsType(leftNode.token.value, "float")) flagFloat = true;
            if (rightNode.type == "VAR" && varSpace.VarIsType(rightNode.token.value, "float")) flagFloat = true;

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
        private CommonNode cmpCheakDelete(CommonNode root)
        {
            if (root.type != "IF")
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = cmpCheakDelete(root.childs[i]);
                }
                return root;
            }

            CommonNode cmpNode = take(root, 0);
            CommonNode bodyNode = take(root, 1);

            if (cmpNode.token.value == "false") return new CommonNode("AIR", root.token);
            if (cmpNode.token.value == "true") return bodyNode;

            return root;
        }
        private CommonNode classReFresh(CommonNode root)
        {
            if (root.type != "CLASS")
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
                if (node.type == "VAR") varNodes.Add(node);
                else if (node.type == "FUNC") methodNodes.Add(node);
                else if (node.type == "CONSTRUCTOR") constructorNode = node;
                else if (node.type == "DESTRUCTOR") destructorNode = node;
                else if (node.type == "DECLARATOR") declarationNode = node;
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

            CommonNode strt = new CommonNode("STRUCT", root.token);
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
            if (strt.childs.Count == 0)
            {
                strt.childs.Add(new CommonNode("VAR", new Token(TokenTypeList.tokenTypes["VAR"], "value", strt.token.pos)));
                strt.childs[0].childs.Add(new CommonNode("TYPE", new Token(TokenTypeList.tokenTypes["VAR"], "int32", strt.token.pos)));
            }
            if (declarationNode != null) ast.declarotivePatternsStruct.Add(strt.token.value,  strt);
            if (declarationNode != null) ast.declarativeClassNames.Add(strt.token.value);

            foreach (CommonNode node in methodNodes)
            {
                CommonNode newRoot = node;
                if (declarationNode == null) newRoot.token.value += "_" + root.token.value;
                CommonNode signatureFuncNode = newRoot.childs[1];
                List<CommonNode> commonNodes = new List<CommonNode>();
                commonNodes.Add(new CommonNode("VAR", new Token(TokenTypeList.tokenTypes["VAR"], "this", signatureFuncNode.token.pos)));
                commonNodes[0].childs.Add(new CommonNode("TYPE", new Token(TokenTypeList.tokenTypes["VAR"], root.token.value, signatureFuncNode.token.pos)));
                foreach (CommonNode cn in signatureFuncNode.childs) commonNodes.Add(cn);
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
            if (root.type != "CLASS")
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
                foreach (var child in ast.classMethods[ast.ClassesInheritances[root.token.value]])
                {
                    ast.classMethods[ast.ClassesInheritances[root.token.value]].Add(child);
                    ast.classMethods[ast.ClassesInheritances[root.token.value]][0].type = root.token.value;
                    List<CommonNode> newMethod = ast.classMethods[ast.ClassesInheritances[root.token.value]];
                    ast.classMethods[root.token.value].Remove(ast.ClassesInheritances[root.token.value]);
                    ast.classMethods.Add(child.token.value + "_" + root.token.value, newMethod);
                }
            }

            return root;
        }

        private CommonNode structCheak(CommonNode root)
        {
            if (root.type != "STRUCT")
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = structCheak(root.childs[i]);
                }
                return root;
            }
            //ast.structs.Add
            //parseStruct(root, 0);
            if (ast.declarotivePatternsStruct.Keys.Contains(root.token.value)) return new CommonNode("AIR", root.token);
            //if (ast.declarotiveNames.Contains(root.token.value)) return new CommonNode("AIR", root.token);
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

            if (root.type != "STRUCT")
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
                    CommonNode varNode = new CommonNode("VAR", new Token(null, child.Key, child.Value.token.pos));
                    varNode.childs.Add(child.Value);
                    root.childs.Add(varNode);
                }
            }

            return root;
        }
        private CommonNode varDeclaratorCheak(CommonNode root, ref CommonNode _ast)
        {
            if (root.type != "VAR")
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

                if (type.childs.Count > 0 && root.childs[0].type != "OFFSET")
                {
                    type.token.value = generationDeclarationStruct(type, type.childs[0]);
                    _ast.childs.Add(ast.childs[ast.childs.Count - 1]);
                    ast.childs.RemoveAt(ast.childs.Count - 1);
                    type.childs.Clear();
                    root.childs[0].childs.Clear();
                    root.childs[0].token.value = type.token.value;
                }
            }
            if (ast.declarotivePatternsStruct.ContainsKey(root.token.value)) return new CommonNode("AIR", root.token);
            return root;
        }
        private CommonNode callCheak(CommonNode root)
        {
            if (root.type != "CALL")
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
            if (root.type != "FUNC")
            {
                for (int i = 0; i < root.childs.Count; i++)
                {
                    root.childs[i] = funcCheak(root.childs[i]);
                }
                return root;
            }
            if (root.childs[0].token.value != "function") return root;

            Dictionary<string, CommonNode> varsLocal = new Dictionary<string, CommonNode>();
            foreach (CommonNode node in root.childs[1].childs) varsLocal.Add(node.token.value, node.childs[0]);

            List<CommonNode> recurse(CommonNode commonNode)
            {
                List<CommonNode> types = null;
                foreach (var child in commonNode.childs)
                {
                    if (child.childs.Count == 1 && child.childs[0].type == "TYPE")
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
                    if (child.type == "RETURN")
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
            if (Returns == null || Returns.Count == 0) root.childs[0].token.value = "void";
            else {
                string ReturnType = Returns[0].type;
                string ReturnValue = getTypeFromNode(Returns[0], varsLocal);
                foreach (CommonNode returnNode in Returns)
                {
                    string ReturnValueLast = getTypeFromNode(returnNode, varsLocal);
                    if (ReturnValueLast != ReturnValue)
                    { 
                        if (Compiler.types.ContainsKey(ReturnValue) && Compiler.types.ContainsKey(ReturnValueLast))
                        {
                            int aling_first = 0;
                            int aling_second = -1;
                            aling_first = Compiler.aligns[ReturnValue];
                            aling_second = Compiler.aligns[ReturnValueLast];
                            if (aling_first != aling_second) Syntax.SyntaxError($"Не все возвращаемые типы Функции: {root.token.value} равны!", returnNode);
                        } else Syntax.SyntaxError($"Не все возвращаемые типы Функции: {root.token.value} равны!", returnNode);
                    }
                }
                //ReturnType = Returns[0].type;
                string type = getTypeFromNode(Returns[0], varsLocal);
                root.childs[0].token.value = type;
                ast.resualtFunc[root.token.value].token.value = type;
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
        private CommonNode defineCheak(CommonNode root)
        {
            if (root.type != "TYPEIF")
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
            return new CommonNode("AIR", root.token);
        }
        private CommonNode constRemove(CommonNode root)
        {
            if (root.type != "VAR")
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
            if (root.type != "STRUCT")
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
                CommonNode strt = new CommonNode("STRUCT", root.token);
                Console.WriteLine(varsStruct.Count);
                Console.WriteLine(varsStructParent.Count);
                foreach (var child in varsStruct) strt.childs.Add(child.Value);
                Console.WriteLine(strt.childs.Count);
                return root;
            }
            //else return root;
            return root;
        }

        private CommonNode parse(CommonNode root, int z_buffer)
        {
            switch (root.type)
            {
                case "INLINE":
                    return parseInline(root, z_buffer);
                case "FUNC":
                    return parseFunc(root, z_buffer);
                case "CALL":
                    return parseCall(root, z_buffer);
                case "STRUCT":
                    return parseStruct(root, z_buffer);
                case "CLASS":
                    return parseClass(root, z_buffer);
                case "USING":
                    return parseUsing(root, z_buffer);
                case "VAR":
                    return parseVar(root, z_buffer);
                case "MODIFIER":
                    return parseModifier(root, z_buffer);
                case "FOR":
                    return parseFor(root, z_buffer);
                case "ENUMERATOR":
                    return parseEnumerator(root, z_buffer);
                default:
                    if (root.childs.Count == 0)
                        break;
                    for (int i = 0; i < root.childs.Count; i++)
                    {
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
                if (body.childs[i].type == "RETURN")
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
            for (int i = 0; i < signature.childs.Count; i++)
            {
                CommonNode type = signature.childs[i].childs[0];

                if (type.childs.Count > 0 && root.childs[0].type != "OFFSET")
                {
                    type.token.value = generationDeclarationStruct(type, type.childs[0]);
                    type.childs.Clear();
                    signature.childs[i].childs[0] = type;
                }
            }
            if (ast.resualtFunc[root.token.value] != null && ast.resualtFunc[root.token.value].token.value != "void" && !Compiler.types.ContainsKey(ast.resualtFunc[root.token.value].token.value) && ast.resualtFunc[root.token.value].type != "INDICATOR")
            {
                //Console.WriteLine("RESUALTPTR - " + ast.resualtFunc[root.token.value].token.value + " | " + ast.resualtFunc[root.token.value].type);
                CommonNode resualtVar = new CommonNode("VAR", new Token(null, "resualtPtr", signature.token.pos));
                resualtVar.childs.Add(ast.resualtFunc[root.token.value]);
                childs.Add(resualtVar);
            }

            varSpace.OpenSpace();
            foreach (CommonNode child in signature.childs)
            {
                Console.WriteLine($"Func Signature Var : {child.token.value} | {child.childs[0].token.value}");
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
            if (!ast.structs.ContainsKey(root.token.value)) ast.structs.Add(root.token.value, typesVar);
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
                            string childType = root.childs[0].childs[j].type;
                            string functionType = ast.functionOver[root.token.value][i].childs[1].childs[j].token.value;
                            switch (childType)
                            {
                                case "VAR":
                                case "POSTUNAROPER":
                                case "PREUNAROPER":
                                    if (varSpace.GetTypeValue(childValue) != functionType) flag = false;
                                    break;
                                case "SIZEOF":
                                case "TYPEOF":
                                case "NUMBER":
                                case "ADDRESS":
                                case "BINOPER":
                                    if (!Compiler.types.ContainsKey(functionType)) flag = false;
                                    break;
                                case "FLOAT":
                                case "FLOATOPER":
                                    if (functionType != "float" && functionType != "double") flag = false;
                                    break;
                                case "STRING":
                                    if (functionType != "string") flag = false;
                                    break;
                                case "CHAR":
                                    if (functionType != "char" && functionType != "byte" && functionType != "int8") flag = false;
                                    break;
                                case "BOOL":
                                    if (functionType != "bool") flag = false;
                                    break;
                                case "TYPEOPER":
                                    if (functionType != childValue) flag = false;
                                    break;
                                case "CALL":
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
                    argsNew.Add(new CommonNode("VAR", new Token(TokenTypeList.tokenTypes["VAR"], strs[0], root.childs[0].token.pos)));
                    foreach (CommonNode node in root.childs[0].childs) argsNew.Add(node);
                    root.childs[0].childs = argsNew;
                    root.token.value = strs[strs.Length-1] + "_" + type;
                }
            } catch { }
            if (ast.declarotivePatternsFunctions.Keys.Contains(root.token.value)) root = generationDeclarationFunc(root);
            if (take(root, 0).type == "DECLARATOR") Syntax.SyntaxError("Ошибка использывание не декларотивную функцию как декларотивную!", root);

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
                if (!root.token.value.Contains(".") && root.childs[0].type != "OFFSET" && varSpace.ContainsKey(root.token.value))
                    Syntax.SyntaxError($"В текущей области видимости Переменная: {root.token.value} уже объявлена!", root);

                CommonNode type = root.childs[0];
                
                /*if (ast.classVars.ContainsKey(type.token.value) && ast.declarativeClassNames.Contains(type.token.value))
                {
                    CommonNode _ast = ast;
                    string oldType = type.token.value;
                    string oldType2 = type.token.value;
                    Dictionary<string, string> declaratorTypes = new Dictionary<string, string>();
                    type.token.value = generationDeclarationStruct(type, type.childs[0], ref declaratorTypes);
                    oldType = type.token.value;
                    if (ast.declarativeClassNames.Contains(oldType2))
                    {
                        ast.classMethods.Add(oldType, new List<CommonNode>());
                        foreach (CommonNode child in ast.declarotiveClassMethods[oldType2])
                        {
                            CommonNode newMethod = replaceNodes(child, ref declaratorTypes);
                            newMethod.token.value += "_" + type.token.value;
                            newMethod.childs[1].childs[0].childs[0].token.value = type.token.value;
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
                }*/

                if (type.childs.Count > 0 && root.childs[0].type != "OFFSET")
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
                            newMethod.childs[1].childs[0].childs[0].token.value = type.token.value;
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
                if (root.childs[0].type != "OFFSET")
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
                    if (child.childs.Count == 1 && child.childs[0].type == "TYPE") return child;
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
            if (ast.declarotivePatternsStruct.Keys.Contains(root.token.value)) return new CommonNode("AIR", root.token);
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
                if (root.type == "TYPE" && rootNode.childs[i].type == "DECLARATOR")
                {
                    //Console.WriteLine($"{root.token.value} + {root.type}");

                    CommonNode replace = replaceNodes(rootNode.childs[i], ref table);
                    string name = generationDeclarationStruct(root, replace);
                    replace.token.value = name;
                    root = replace;
                    root.type = "TYPE";
                    root.childs.Clear();
                    continue;
                }
                if (root.type == "INDICATOR")
                {
                    //Console.ReadKey(); Console.WriteLine("REPLACE INDICATOR");
                    CommonNode replace = replaceNodes(rootNode.childs[i], ref table);
                    string name = generationDeclarationStruct(root, replace);
                    replace.token.value = name;
                    root = replace;
                    root.type = "INDICATOR";
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
