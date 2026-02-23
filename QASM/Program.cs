using System;


namespace QASM
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("---==||<<<' QASM '>>>||==---");

            string Program = "MyVar dd 0";
            string[] codes = { Program };
            string[] codes2 = {
                "MyVar dd 0",
                "MyVar1 dw 0",
                "MyVar2 db 0",
                "RefVar.Var dd 0",
                "mov eax, [mem]",
                "mov [mem], eax",
                "mov eax, ebx",
                "mov ecx, 300"
                //"mov eax, 10"
            };

            Compiler.Translation(Preprocessor.PreTranslation(codes2));
        }
    }
}