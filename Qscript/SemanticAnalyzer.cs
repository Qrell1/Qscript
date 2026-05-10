using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Qscript
{
    public static class SemanticAnalyzer
    {
        static public ProgramNode ast;

        static public VarSpace varSpace = new VarSpace();

        public static CommonNode take(CommonNode node, int i = 0)
        {
            if (node.childs.Count >= i + 1)
            {
                return node.childs[i];
            }
            return null;
        }

        public static void startAnalis(ProgramNode _root)
        {
            ast = _root;
            analis(_root, 0);
        }

        public static void analis(CommonNode root, int z_buffer)
        {
            switch (root.type)
            {
                case "BINOPER":
                    analisBinoper(root, z_buffer);
                    break;
                case "FLOATBINOPER":
                    //analisFloatoper(root, z_buffer);
                    break;
                case "INLINE":
                    analisInline(root, z_buffer);
                    break;
                case "FUNC":
                    analisFunc(root, z_buffer);
                    break;
                case "FOR":
                    analisFor(root, z_buffer);
                    break;
                case "WHILE":
                    analisWhile(root, z_buffer);
                    break;
                case "ITER":
                    analisIter(root, z_buffer);
                    break;
                case "ENUMERATOR":
                    analisEnumerator(root, z_buffer);
                    break;
                case "CALL":
                    analisCall(root, z_buffer);
                    break;
                case "VAR":
                    analisVar(root, z_buffer);
                    break;
                default:
                    //if (root.childs.Count == 0 || root.type == "SIGNATURE" || root.type == "CMP" || root.type == "STRUCT" || root.type == "FUNC")
                    //break;
                    if (root.type == "STRUCT") break;
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        analis(root.childs[i], z_buffer + 1);
                    }
                    break;
            }
            return;
        }

        private static void analisBinoper(CommonNode root, int z_buffer)
        {
            CommonNode leftNode = take(root, 0);
            CommonNode rightNode = take(root, 1);

            analis(leftNode, z_buffer + 1);
            analis(rightNode, z_buffer + 1);
            int leftSize = getFormulaNodeSize(leftNode);
            int rightSize = getFormulaNodeSize(rightNode);
            string leftType = getFormulaType(leftNode);
            string rightType = getFormulaType(rightNode);
            if (!ast.operatorFunctions.ContainsKey((root.token.value, leftType, rightType)) && leftSize != rightSize && root.token.value != "=") Syntax.SyntaxError($"Нельзя оперировать: {leftNode.token.value} с {rightNode.token.value}", root);
        }
        private static void analisFloatoper(CommonNode root, int z_buffer)
        {
            CommonNode leftNode = take(root, 0);
            CommonNode rightNode = take(root, 1);

            analis(leftNode, z_buffer + 1);
            analis(rightNode, z_buffer + 1);
            int leftSize = getFormulaNodeSize(leftNode);
            int rightSize = getFormulaNodeSize(rightNode);
            if (leftSize != rightSize && root.token.value != "=") Syntax.SyntaxError($"Нельзя оперировать: {leftNode.token.value} с {rightNode.token.value}", root);
        }
        private static void analisInline(CommonNode root, int z_buffer)
        {
            CommonNode signatureNode = take(root, 0);
            CommonNode bodyNode = take(root, 1);

            bool isInlineCall(CommonNode node)
            {
                bool isNode = false;

                foreach (var child in node.childs)
                {
                    if (ast.inlineNames.Contains(child.token.value)) return true;
                    else if (child.childs.Count > 0) isNode = (isInlineCall(child)) ? true : isNode;
                }

                return isNode;
            }
            bool isVarDeclaration(CommonNode node)
            {
                bool isNode = false;

                foreach (var child in node.childs)
                {
                    if (node.type == "VAR" && node.childs.Count > 0 && node.childs[0].type == "TYPE") return true;
                    else if (child.childs.Count > 0) isNode = (isVarDeclaration(child)) ? true : isNode;
                }

                return isNode;
            }

            varSpace.OpenSpace();
            foreach (CommonNode child in signatureNode.childs) varSpace.AddVar(child.token.value, child.childs[0]);
            if (isVarDeclaration(bodyNode)) Syntax.SyntaxError($"Невозможно объявление переменных в инлайне", bodyNode);
            if (isInlineCall(bodyNode)) Syntax.SyntaxError($"Невозможно вызывать инлайны в инлайне", bodyNode);
            foreach (CommonNode child in bodyNode.childs) analis(child, z_buffer + 2);
            varSpace.CloseSpace();
        }
        private static void analisFunc(CommonNode root, int z_buffer)
        {
            //Console.WriteLine($"ANALIS FUNCTION: {root.token.value} : {z_buffer}");
            CommonNode signatureNode = take(root, 0);
            varSpace.OpenSpace();
            for (int i = 0; i < ast.typesArgsFunc[root.token.value].childs.Count; i++)
                varSpace.AddVar(ast.typesArgsFunc[root.token.value].childs[i].token.value, ast.typesArgsFunc[root.token.value].childs[i].childs[0]);
            analis(take(root, 2), z_buffer + 1);
            varSpace.CloseSpace();
        }
        private static void analisVar(CommonNode root, int z_buffer)
        {
            if (root.childs.Count > 0 && root.childs[0].type != "OFFSET")
            {

                if (varSpace.PeekContainsKey(root.token.value))
                    Syntax.SyntaxError($"Нельзя объявлять две переменных с одним именем! {root.childs[0].type}", root);
                CommonNode type = root.childs[0];
                varSpace.AddVar(root.token.value, type);
            }
            else
            {
                if (!root.token.value.Contains('.') && !root.token.value.Contains(',') && !varSpace.ContainsKey(root.token.value)) Syntax.SyntaxError($"[S]В текущей области видимости не существует Переменной: ", root);
            }
        }
        private static void analisCall(CommonNode root, int z_buffer)
        {
            //Console.WriteLine($"Call {root.token.value}");
            if (varSpace.ContainsKey(root.token.value) && varSpace.GetTypeValue(root.token.value) == "function") return;
            foreach (var externLibrary in ast.externFuncs)
            {
                if (externLibrary.Value.Contains(root.token.value)) return;
            }
            if (!ast.resualtFunc.ContainsKey(root.token.value) && !ast.inlineNames.Contains(root.token.value))
                Syntax.SyntaxError($"Функции: {root.token.value} не сущестует чтобы её вызывать!", root);
            else
            {
                foreach (CommonNode child in root.childs[0].childs) analis(child, z_buffer + 2);
                if (ast.inlineNames.Contains(root.token.value)) return;

                CommonNode funcSignatureNode = ast.typesArgsFunc[root.token.value];
                CommonNode callSignatureNode = root.childs[0];

                if (funcSignatureNode.childs.Count != callSignatureNode.childs.Count)
                    Syntax.SyntaxError($"Ошибка вызова Функции: {root.token.value} ты пердаёшь {callSignatureNode.childs.Count} аргументов,\nНо функция принемает {funcSignatureNode.childs.Count} аргументов", root);
                else
                {
                    /*int[] sizes = new int[callSignatureNode.childs.Count];

                    for (int i = 0; i < sizes.Length; i++)
                    {
                        sizes[i] = getFormulaNodeSize(callSignatureNode.childs[i]);
                    }


                    for (int i = 0; i < funcSignatureNode.childs.Count; i++)
                    {
                        if (sizes[i] != getFormulaNodeSize(funcSignatureNode.childs[i]))
                            Syntax.SyntaxError($"Тип аргумента {i} вызываемой Функции: {root.token.value} несовподает с {funcSignatureNode.childs[i].token.value}!", callSignatureNode.childs[i]);
                    }*/
                }
            }
        }

        // cycle 
        private static void analisFor(CommonNode root, int z_buffer)
        {
            CommonNode initNode = take(root, 0);
            CommonNode cmpNode = take(root, 1);
            CommonNode stepNode = take(root, 2);
            CommonNode bodyNode = take(root, 3);
            varSpace.OpenSpace();
            analis(initNode, z_buffer + 1);
            analis(cmpNode, z_buffer + 1);
            analis(stepNode, z_buffer + 1);
            foreach (CommonNode child in bodyNode.childs) analis(child, z_buffer + 2);
            varSpace.CloseSpace();
        }
        private static void analisWhile(CommonNode root, int z_buffer)
        {
            CommonNode signatureNode = take(root, 0);
            CommonNode bodyNode = take(root, 1);
            varSpace.OpenSpace();
            analis(signatureNode, z_buffer + 1);
            foreach (CommonNode child in bodyNode.childs) analis(child, z_buffer + 2);
            varSpace.CloseSpace();
        }
        private static void analisIter(CommonNode root, int z_buffer)
        {
            CommonNode signatureNode = take(root, 0);
            CommonNode bodyNode = take(root, 1);
            varSpace.OpenSpace();
            analis(signatureNode, z_buffer + 1);
            foreach (CommonNode child in bodyNode.childs) analis(child, z_buffer + 2);
            varSpace.CloseSpace();
        }
        private static void analisEnumerator(CommonNode root, int z_buffer)
        {
            CommonNode varNode = take(root, 0);
            CommonNode signatureNode = take(root, 1);
            CommonNode bodyNode = take(root, 2);
            varSpace.OpenSpace();
            analis(varNode, z_buffer + 1);
            analis(signatureNode, z_buffer + 1);
            foreach (CommonNode child in bodyNode.childs) analis(child, z_buffer + 2);
            varSpace.CloseSpace();
        }

        private static bool isNodeType(string type, CommonNode root)
        {
            bool isNode = false;

            foreach (var child in root.childs)
            {
                if (child.type == type) return true;
                else if (child.childs.Count > 0) isNode = (isNodeType(type, child)) ? true : isNode;
            }

            return isNode;
        }
        private static bool isNodeValue(string value, CommonNode root)
        {
            bool isNode = false;

            foreach (var child in root.childs)
            {
                if (child.token.value == value) return true;
                else if (child.childs.Count > 0) isNode = (isNodeValue(value, child)) ? true : isNode;
            }

            return isNode;
        }
        private static int getFormulaNodeSize(CommonNode node)
        {
            int size = 0;
            string type;
            try
            {
                switch (node.type)
                {
                    case "VAR":
                    case "POSTUNAROPER":
                    case "PREUNAROPER":
                        type = varSpace.GetType(node.token.value).token.value;
                        if (varSpace.GetType(node.token.value).type == "INDICATOR") size = 4;
                        else if (Compiler.aligns.ContainsKey(type)) size = Compiler.aligns[type];
                        else size = getStructSize(type);
                        break;
                    case "SIZEOF":
                    case "TYPEOF":
                    case "NUMBER":
                    case "ADDRESS":
                        size = 4;
                        break;
                    case "BINOPER":
                        size = 4;

                        int _size1 = getFormulaNodeSize(node.childs[0]);
                        int _size2 = getFormulaNodeSize(node.childs[1]);

                        string _type1 = getFormulaType(node.childs[0]);
                        string _type2 = getFormulaType(node.childs[1]);

                        if (_size1 == _size2) size = _size1;

                        if (ast.operatorFunctions.ContainsKey((node.token.value, _type1, _type2)))
                        {
                            string OperatorName = ast.operatorFunctions[(node.token.value, _type1, _type2)];

                            type = ast.resualtFunc[OperatorName].token.value;
                            if (Compiler.aligns.ContainsKey(type)) size = Compiler.aligns[type];
                            else size = getStructSize(type);
                        }
                        break;
                    case "FLOAT":
                    case "FLOATOPER":
                        size = 4;
                        break;
                    case "STRING":
                        size = 2;
                        break;
                    case "CHAR":
                        size = 2;
                        break;
                    case "BOOL":
                        size = 1;
                        break;
                    case "TYPEOPER":
                        type = node.token.value;
                        if (Compiler.aligns.ContainsKey(type)) size = Compiler.aligns[type];
                        else size = getStructSize(type);
                        break;
                    case "CALL":
                        type = varSpace.GetType(node.token.value).token.value;
                        if (Compiler.aligns.ContainsKey(type)) size = Compiler.aligns[type];
                        else size = getStructSize(type);
                        break;
                    default:
                        size = 4;
                        break;
                }
            } catch { size = 4; }
            return size;
        }
        private static string getFormulaType(CommonNode node)
        {
            string type;
            try
            {
                switch (node.type)
                {
                    case "VAR":
                    case "POSTUNAROPER":
                    case "PREUNAROPER":
                        type = varSpace.GetType(node.token.value).token.value;
                        break;
                    case "SIZEOF":
                    case "TYPEOF":
                    case "NUMBER":
                    case "ADDRESS":
                        type = "int";
                        break;
                    case "BINOPER":
                        type = "BINOPER";

                        string _type1 = getFormulaType(node.childs[0]);
                        string _type2 = getFormulaType(node.childs[0]);

                        if (_type1 == _type2) type = _type1;

                        string leftType = (_type1 == "STRING") ? "string" : _type1;
                        string rightType = (_type2 == "STRING") ? "string" : _type2;

                        if (ast.operatorFunctions.ContainsKey((node.token.value, leftType, rightType)))
                        {
                            string OperatorName = ast.operatorFunctions[(node.token.value, leftType, rightType)];

                            type = ast.resualtFunc[OperatorName].token.value;
                        }
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
                        type = node.token.value;
                        break;
                    case "CALL":
                        type = varSpace.GetType(node.token.value).token.value;
                        break;
                    default:
                        type = "int";
                        break;
                }
            }
            catch { type = "BINOPER"; }
            return type;
        }
        public static int getStructSize(string type)
        {
            int size = 0;

            foreach (var _var in ast.structs[type].Values)
            {
                if (_var.type == "INDICATOR") size += 4;
                else if (Compiler.types.ContainsKey(_var.token.value))
                {
                    string classsize = Compiler.types[_var.token.value];
                    if (_var.token.value == "dq") size += 8;
                    else if (_var.token.value == "dd") size += 4;
                    else if (_var.token.value == "dw") size += 2;
                    else if (_var.token.value == "db") size += 1;
                }
                else if (_var.token.value == "dq") size += 8;
                else if (_var.token.value == "dd") size += 4;
                else if (_var.token.value == "dw") size += 2;
                else if (_var.token.value == "db") size += 1;
                else size += getStructSize(_var.token.value);
            }

            return size;
        }
    }
}