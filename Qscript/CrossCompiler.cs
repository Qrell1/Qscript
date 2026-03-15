using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qscript
{
    public class AsmStruct
    {
        public string Handle;
        public string Name;
        public Dictionary<string, string> Vars = new Dictionary<string, string>();
        public List<string> Includes = new List<string>();

        public StringData Data;

        public AsmStruct (List<string> data, List<string> includes)
        { // data[0] => struct Point { Point ==
            Handle = data[0].Trim();
            Name = data[0].Trim().Split(' ')[1];
            Includes = includes;

            for (int i = 1; i < data.Count-1; i++)
            {
                string[] strs = data[i].Split(' ');
                // strs[0] == var
                // strs[1] == type
                Vars.Add(strs[0], strs[1]);
            }
        }
        public string ToString (ref List<AsmStruct> allStructs, ref List<string> allNames)
        {
            Dictionary<string, int> structs = new Dictionary<string, int>();
            List<string> sort8b = new List<string>();
            List<string> sort4b = new List<string>();
            List<string> sort2b = new List<string>();
            List<string> sort1b = new List<string>();

            StringData resualt = new StringData();
            resualt.Data.Add(Handle);

            foreach (var v in Vars)
            {
                if (!Compiler.aligns.ContainsKey(v.Value)) structs.Add(v.Key, allStructs[allNames.IndexOf(v.Value)].getStructSize(ref allStructs, ref allNames));
                else
                {
                    switch (Compiler.aligns[v.Value])
                    {
                        case 8: sort8b.Add(v.Key); break;
                        case 4: sort4b.Add(v.Key); break;
                        case 2: sort2b.Add(v.Key); break;
                        case 1: sort1b.Add(v.Key); break;
                    }
                }
            }
            int offset = 0;
            
            foreach (var v in sort8b) resualt.Append($"    {v} {Vars[v]} 0");
            foreach (var v in sort4b) resualt.Append($"    {v} {Vars[v]} 0");
            foreach (var v in sort2b) resualt.Append($"    {v} {Vars[v]} 0");
            foreach (var v in sort1b) resualt.Append($"    {v} {Vars[v]} 0");

            offset +=
                (sort8b.Count * 8) +
                (sort4b.Count * 4) +
                (sort2b.Count * 2) +
                (sort1b.Count * 1);

            foreach (var v in structs)
            {
                int align = 8 - (offset % 8);
                if (offset != 0) resualt.Append($"    align {align}");
                offset += align + v.Value;
                resualt.Append($"    {v.Key} {Vars[v.Key]} 0");
            }

            return resualt.ToString();
        }
        public int getStructSize (ref List<AsmStruct> allStructs, ref List<string> allNames)
        {
            int fullSize = 0;
            foreach (var v in Vars)
            {
                if (Compiler.aligns.ContainsKey(v.Value)) continue;
                if (!Compiler.aligns.ContainsKey(v.Value))
                {
                    int size = allStructs[allNames.IndexOf(v.Value)].getStructSize(ref allStructs, ref allNames);
                    int a = 8 - (fullSize % 8);
                    if (fullSize != 0) size += a;
                    fullSize += size;
                }
            }
            
            foreach (var v in Vars)
            {
                if (Compiler.aligns.ContainsKey(v.Value)) fullSize += Compiler.aligns[v.Value];
            }

            return fullSize;
        }
        public int getSize (string type, ref List<AsmStruct> allStructs, ref List<string> allNames)
        {
            int size = 0;
            if (!Compiler.aligns.ContainsKey(type)) size = allStructs.ElementAt(allNames.IndexOf(type)).getStructSize(ref allStructs, ref allNames);
            else size = Compiler.aligns[type];            
            return size;
        }
    }

    public class CrossCompiler
    {
        public objProgram Compile(objProgram _objProgram)
        {
            objProgram objProgramResualt = _objProgram;

            StringData structsSpace = AlingingStructs(_objProgram.macroData);

            return objProgramResualt;
        }

        public StringData AlingingStructs (StringData structSpace)
        {
            List<AsmStruct> asmStructs = new List<AsmStruct>();
            List<int> asmStructsIndex = new List<int>();
            List<string> asmStructsName = new List<string>();
            StringData tempAsm = new StringData();
            List<string> tempIncludes = new List<string>();
            
            bool flag = false;
            int index = 0;
            foreach (string str in structSpace.Data)
            {
                if (str.Trim().StartsWith("struct"))
                {
                    tempAsm.Append(str);
                    flag = true;
                    continue;
                }
                else if (str.Trim().StartsWith("ends"))
                {
                    tempAsm.Append(str);
                    asmStructs.Add(new AsmStruct(new List<string>(tempAsm.Data), new List<string>(tempIncludes)));
                    asmStructsIndex.Add(index);
                    asmStructsName.Add(asmStructs.Last().Name);
                    tempAsm.Data.Clear();
                    tempIncludes.Clear();
                    flag = false;
                    index++;
                    continue;
                }
                else if (flag)
                {
                    tempAsm.Append(str);
                    string type = str.Trim().Split(' ')[1];
                    if (!Compiler.types.ContainsKey(type)) tempIncludes.Add(type);
                    continue;
                }
                else
                {
                    continue;
                }
            }

            for (int i = 0; i < asmStructs.Count; i++)
            {
                if (asmStructs[i].Includes.Count == 0) continue;
                else
                {
                    int pos = asmStructsIndex[i];
                    string si = asmStructs[i].Name;
                    foreach (var s in asmStructs[i].Includes) { var p = asmStructsName.IndexOf(s);
                        if (p > pos)
                        { pos = p; si = s; } }
                    AsmStruct ass = asmStructs[asmStructsName.IndexOf(si)];
                    string siTemp = asmStructsName[asmStructsName.IndexOf(si)];
                    string siTemp2 = asmStructsName[i];
                    int iss = asmStructsName.IndexOf(si);
                    int iss2 = asmStructsIndex[i];

                    asmStructsIndex[asmStructsName.IndexOf(si)] = i;
                    asmStructsIndex[i] = pos;

                    asmStructs[asmStructsName.IndexOf(si)] = asmStructs[i];
                    asmStructs[i] = ass;

                    asmStructsName[pos] = siTemp;
                    asmStructsName[iss2] = siTemp2;
                }
            }

            List<AsmStruct> sortList = new List<AsmStruct>();
            foreach (int i in asmStructsIndex) sortList.Add(asmStructs[i]);

            StringData newStructSpace = new StringData();
            foreach (AsmStruct asmStruct in sortList) newStructSpace.Append(asmStruct.ToString());

            return newStructSpace;
        }
    }
}
