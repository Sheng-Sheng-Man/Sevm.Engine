// See https://aka.ms/new-console-template for more information
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.Principal;
using egg;

namespace sevm {
    /// <summary>
    /// 脚本引擎
    /// </summary>
    public static class Program {

        // 输出帮助信息
        static void Help() {
            System.Console.WriteLine("<path> [option]");
            System.Console.WriteLine();
            System.Console.WriteLine("[options]");
            System.Console.WriteLine("-? -h --help : 帮助");
            System.Console.WriteLine("-b --sbc : 从汇编文件生成字节码");
            System.Console.WriteLine("-c --sc : 从字节码翻译生成汇编文件");
        }

        public static void Main(string[] args) {
            it.Initialize(args, false);
            System.Console.ForegroundColor = ConsoleColor.DarkGreen;
            egg.File.UTF8File.WriteWithBoom = false;
            if (args.Length > 0) {
                string path = args[0];
                if (!path.IsEmpty()) {
                    if (path.Length > 2 && path.StartsWith("\"") && path.EndsWith("\"")) path = path.Substring(1, path.Length - 2);
                    if (path.Length > 2 && path.StartsWith("'") && path.EndsWith("'")) path = path.Substring(1, path.Length - 2);
                    string file = "";
                    for (int i = 1; i < args.Length; i++) {
                        if (args[i] == "-b" || args[i] == "--sbc") file = "+sbc";
                        if (args[i] == "-c" || args[i] == "--sc") file = "+sc";
                        if (args[i] == "-?" || args[i] == "-h" || args[i] == "--help") file = "help";
                    }
                    System.Console.Title = $"SIR语言转换工具 Ver:{it.Version} - {path}";
                    switch (file) {
                        case "+sc":
                            System.Console.WriteLine($"正在加载文件'{path}'...");
                            byte[] bytes = egg.File.BinaryFile.ReadAllBytes(path, false);
                            string targetPath = it.GetClosedDirectoryPath(System.IO.Path.GetDirectoryName(path)) + System.IO.Path.GetFileNameWithoutExtension(path) + ".sc";
                            using (Sevm.Sir.SirScript ss = Sevm.Sir.Parser.GetScript(bytes)) {
                                egg.File.UTF8File.WriteAllText(targetPath, ss.ToString());
                                System.Console.WriteLine($"成功生成字节码文件'{targetPath}'!");
                            }
                            break;
                        case "+sbc":
                            System.Console.WriteLine($"正在加载文件'{path}'...");
                            string script = eggs.IO.GetUtf8FileContent(path);
                            targetPath = it.GetClosedDirectoryPath(System.IO.Path.GetDirectoryName(path)) + System.IO.Path.GetFileNameWithoutExtension(path) + ".sbc";
                            using (Sevm.Sir.SirScript ss = Sevm.Sir.Parser.GetScript(script)) {
                                egg.File.BinaryFile.WriteAllBytes(targetPath, ss.ToBytes());
                                System.Console.WriteLine($"成功生成字节码文件'{targetPath}'!");
                            }
                            break;
                        case "sc+sbc":
                            break;
                        case "help": Help(); break;
                        default:
                            System.Console.WriteLine("不支持的操作方式!");
                            System.Console.WriteLine();
                            System.Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Help();
                            break;
                    }
                    System.Console.ReadKey();
                    return;
                }
            }

            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("缺少必要参数!");
            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.DarkGreen;
            Help();
            System.Console.ReadKey();
        }

    }
}
