using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qscript
{
    public static class DataBase
    {
        public static Dictionary<string, string> types = new Dictionary<string, string>()
        {
            {"int64", "dq"},
            {"int32", "dd"},
            {"int16", "dw"},
            {"int8", "db"},
            {"byte", "db"},
            {"string", "du"},
            {"char", "db"},
            {"wchar", "dw"},
            {"float", "dd"},
            {"double", "dq"},
            {"int32_a", "dd"},
            {"bool", "db"},
            {"long", "dd"},
            {"half", "dw"},
            {"function", "dd"},
            {"dq", "dq"},
            {"dd", "dd"},
            {"dw", "dw"},
            {"db", "db"}
        };
        public static Dictionary<string, string> typesarg = new Dictionary<string, string>()
        {
            {"int64", "QWORD"},
            {"int32", "DWORD"},
            {"int16", "WORD"},
            {"int8", "BYTE"},
            {"byte", "BYTE"},
            {"string", "DWORD"},
            {"char", "BYTE"},
            {"wchar", "WORD"},
            {"float", "DWORD"},
            {"double", "QWORD"},
            {"int32_a", "DWORD"},
            {"bool", "BYTE"},
            {"long", "DWORD"},
            {"half", "WORD"},
            {"function", "DWORD"},
            {"dq", "QWORD"},
            {"dd", "DWORD"},
            {"dw", "WORD"},
            {"db", "BYTE"}
        };
        public static Dictionary<string, int> aligns = new Dictionary<string, int>()
        {
            {"int64",   8},
            {"int32",   4},
            {"int16",   2},
            {"int8",    1},
            {"byte",    1},
            //{"string",  2},
            {"char",    1},
            {"wchar",   2},
            {"float",   4},
            {"double",  8},
            {"int32_a", 4},
            {"bool",    1},
            {"long",    4},
            {"half",    2},
            {"function",4},
            {"dq",      8},
            {"dd",      4},
            {"dw",      2},
            {"db",      1}
        };
        public static Dictionary<string, string> typesregs = new Dictionary<string, string>()
        {
            {"int64",   "rax"},
            {"int32",   "eax"},
            {"int16",   "ax"},
            {"int8",    "al"},
            {"byte",    "al"},
            {"string",  "ax"},
            {"char",    "al"},
            {"wchar",   "ax"},
            {"float",   "eax"},
            {"double",  "rax"},
            {"int32_a", "eax"},
            {"bool",    "al"},
            {"long",    "eax"},
            {"half",    "ax"},
            {"function", "eax"},
            {"dq",      "rax"},
            {"dd",      "eax"},
            {"dw",      "ax"},
            {"db",      "al"}
        };
        public static Dictionary<string, string[]> regs = new Dictionary<string, string[]>()
        {
            {"eax", new string[] {"rax","eax","ax","al"}},
            {"ecx", new string[] {"rcx","ecx","cx","cl"}},
            {"edx", new string[] {"rdx","edx","dx","dl"}},
            {"ebx", new string[] {"rbx","ebx","bx","bl"}},
            {"edi", new string[] {"rdi","edi","di","ah"}},
            {"esi", new string[] {"rsi","esi","si","ch"}}
        };
        public static Dictionary<string, string> regschars = new Dictionary<string, string>()
        {
            {"eax", "a"},
            {"ebx", "b"},
            {"edx", "d"},
            {"ecx", "c"}
        };
    }
}
