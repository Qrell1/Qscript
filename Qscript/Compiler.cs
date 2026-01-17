using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qscript
{
    public class objProgram
    {
        //public List<CommonNode> functions;
        public StringBuilder data = new StringBuilder();
        public StringBuilder code = new StringBuilder();
    }
    public class Compiler
    {
        public objProgram _objProg = new objProgram();
        public string fasmCompilerPath;
        private List<CommonNode> list;

        private List<string> vars = new List<string>();

        public Dictionary<string, string> types = new Dictionary<string, string>()
        {
            {"int32", "dd"},
            {"int16", "dw"},
            {"int8", "dd"},
            {"byte", "db"},
            {"string", "db"},
            {"char", "db"}
        };

        public int pos;
        public Compiler(string _fasmCompilerPath) { fasmCompilerPath = _fasmCompilerPath; }

        //
        public bool peek(string type)
        {
            if (list[pos].type == type)
            {
                return true;
            }
            return false;
        }
        public CommonNode take(CommonNode node, int i = 0)
        {
            if (node.childs.Count>=i+1)
            {
                return node.childs[i];
            }
            return null;
        }
        public void skip()
        {
            pos++;
        }

        public void Translation (CommonNode root, int z_buffer)
        {
            //Console.WriteLine($"[DEBUG]--PrintAST>{GenSpaces(z_buffer)} |Тип:{root.type} [{root.token.value}] Дочерних узлов:{root.childs.Count}");
            

            switch (root.type)
            {
                case "ROOT":
                    for (int i = 0; i < root.childs.Count; i++)
                    {
                        CommonNode child = root.childs[i];
                        Translation (child, z_buffer+1);
                    }
                    break;
                case "VAR":
                    CommonNode type = take(root, 0);
                    if (!vars.Contains(root.token.value) && type != null)
                        _objProg.data.Append($"{root.token.value} {types[type.token.value]} ??\n");
                    break;
                case "BINOPER":
                    if (root.token.value == "=")
                    {
                        CommonNode var = take(root, 0);
                        CommonNode type2 = take(var, 0);
                        CommonNode oper2 = take(root, 1);
                        if (!vars.Contains(root.token.value) && type2 != null)
                            _objProg.data.Append($"{var.token.value} {types[type2.token.value]} {oper2.token.value}\n");
                    }
                    break;

            }
            /*for (int i = 0; i < root.childs.Count; i++)
            {
                Translation(root.childs[i], z_buffer + 1);
            }*/
        }
        /*public void transNode ()
        {
            if (peek("VAR"))
            {
                string varName = take().token.value;
                if (peek("TYPE"))
                {
                    string typeOper = types[take().token.value];
                    //objProg.data.Add("  ");
                    //if (peek("OPER") && take().token.)
                }
            }
        }*/
        public void Compilation (string nameFile)
        {
            //string path = "/compile" + "/" + nameFile.Replace(".qs", "") + "/" + nameFile;
            string path = $"/compile/{nameFile}/{nameFile}.asm";

            var proc = new Process();
            proc.StartInfo.FileName = fasmCompilerPath;
            proc.StartInfo.Arguments = path;
            proc.Start();

            proc.WaitForExit();
            proc.Close();
        }
    }
}
