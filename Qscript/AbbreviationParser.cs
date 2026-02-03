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
        public AbbreviationParser() { }

        public CommonNode take(CommonNode node, int i = 0)
        {
            if (node.childs.Count >= i + 1)
            {
                return node.childs[i];
            }
            return null;
        }

        public ProgramNode abbParse (ProgramNode root)
        {
            ast = root;
            ast.childs = parse(root, 0).childs;
            return ast;
        }

        public CommonNode parse (CommonNode root, int z_buffer)
        {
            switch (root.type)
            {
                case "BINOPER":
                    CommonNode leftNode = take(root, 0);
                    CommonNode rightNode = take(root, 1);

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
                    }

                    break;
                case "CONST":
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

                    root.childs[0] = parse(root.childs[0],z_buffer + 1);

                    break;
                case "STRING":
                    root.token.value = $"'{root.token.value}', 0";
                    break;
                case "INLINE":
                    CommonNode body = take(root, 1);
                    for (int i = 0; i < body.childs.Count; i++)
                    {
                        if (body.childs[i].type == "RETURN")
                            throw new Exception("Ошибка в инлайн функции не может быть return");
                    }
                    ast.inlineNames.Add(root.token.value);
                    return root;
                    break;
                /*case "REFVAR":
                    CommonNode var = take(root, 0);
                    return root;
                    if (var.type == "REFVAR")
                    {
                        CommonNode vr = parse(var, z_buffer + 1);
                        var.token.value = root.token.value + "." + vr.token.value;
                        var.type = vr.type;
                        var.childs = vr.childs;
                    }
                    else
                    {
                        CommonNode vr = parse(var, z_buffer + 1);
                        var.token.value = root.token.value + "." + vr.token.value;
                    }
                    return var;
                    break;*/
                default:
                    if (root.childs.Count == 0)
                        break;
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        root.childs[i] = parse(root.childs[i], z_buffer + 1);
                    }
                    break;
            }
            return root;
        }
    }
}
