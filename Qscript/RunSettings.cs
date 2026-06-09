using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qscript
{
    public enum TypeApp
    {
        dll, program, asmmodule, h, gui, bin
    }
    public enum ArchApp
    {
        x86_32, x86_64,
        arm_32, arm_64,    // Может быть на будущие
        riscv_32, riscv_64,// Может быть на будущие
    }
    public enum ModeApp
    {
        release, debug
    }
    public enum FormatApp
    {
        windows, linux
    }
}
