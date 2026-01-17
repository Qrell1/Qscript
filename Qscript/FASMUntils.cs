using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Qscript
{
    public static class FASMUntils
    {
        public static void CompileAndRun(string llvmIr, string outputName = "Qtest")
        {
            // Сохраняем IR код
            File.WriteAllText($"{outputName}.ll", llvmIr);

            Console.WriteLine("Скомпилированный LLVM IR сохранен в файл: " + $"{outputName}.ll");

            // Проверяем доступность llc
            string llcPath = FindExecutable("llc");
            if (string.IsNullOrEmpty(llcPath))
            {
                Console.WriteLine("Ошибка: llc (LLVM compiler) не найден. Убедитесь, что LLVM установлен и добавлен в PATH.");
                return;
            }

            // Проверяем доступность gcc
            string gccPath = FindExecutable("gcc");
            if (string.IsNullOrEmpty(gccPath))
            {
                Console.WriteLine("Ошибка: gcc не найден. Убедитесь, что GCC установлен и добавлен в PATH.");
                return;
            }

            try
            {
                // Компилируем в объектный файл
                Console.WriteLine("Компиляция LLVM IR в ассемблер...");
                if (!RunProcess(llcPath, $"{outputName}.ll -o {outputName}.s", "llc"))
                    return;

                // Компилируем в исполняемый файл
                Console.WriteLine("Компиляция ассемблера в исполняемый файл...");
                if (!RunProcess(gccPath, $"{outputName}.s -o {outputName}", "gcc"))
                    return;

                // Запускаем программу
                Console.WriteLine("Запуск программы...");
                Console.WriteLine("=== Вывод программы ===");

                var process = new Process();
                process.StartInfo.FileName = $"./{outputName}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Console.WriteLine(output);
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine("=== Ошибки ===");
                    Console.WriteLine(error);
                }

                Console.WriteLine($"=== Программа завершилась с кодом: {process.ExitCode} ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при компиляции или запуске: {ex.Message}");
            }
        }

        private static bool RunProcess(string executable, string arguments, string processName)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = executable;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"Ошибка при выполнении {processName}:");
                    Console.WriteLine($"Output: {output}");
                    Console.WriteLine($"Error: {error}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске {processName}: {ex.Message}");
                return false;
            }
        }

        private static string FindExecutable(string executableName)
        {
            // Проверяем PATH
            var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? Array.Empty<string>();

            foreach (var dir in pathDirs)
            {
                if (string.IsNullOrEmpty(dir)) continue;

                try
                {
                    var fullPath = Path.Combine(dir, executableName);
                    if (File.Exists(fullPath))
                        return fullPath;

                    // Для Windows добавляем .exe
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        fullPath = Path.Combine(dir, executableName + ".exe");
                        if (File.Exists(fullPath))
                            return fullPath;
                    }
                }
                catch
                {
                    // Игнорируем ошибки доступа к директориям
                }
            }

            // Проверяем стандартные пути для LLVM на Windows
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var possiblePaths = new[]
                {
                    @"C:\Program Files\LLVM\bin\",
                    @"C:\LLVM\bin\",
                    @"C:\msys64\mingw64\bin\",
                    @"C:\msys2\mingw64\bin\"
                };

                foreach (var path in possiblePaths)
                {
                    try
                    {
                        var fullPath = Path.Combine(path, executableName + ".exe");
                        if (File.Exists(fullPath))
                            return fullPath;
                    }
                    catch
                    {
                        // Игнорируем ошибки
                    }
                }
            }

            return null;
        }
    }
}