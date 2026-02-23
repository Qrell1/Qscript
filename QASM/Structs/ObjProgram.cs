using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QASM.Structs
{
    public class objProgram
    {
        public StringData data = new StringData();

        public StringData codeData = new StringData();
        public StringData procData = new StringData();
        public StringData macroData = new StringData();

        public StringBuilder includes = new StringBuilder();

        public StringBuilder stringsConsts = new StringBuilder();
    }
}
