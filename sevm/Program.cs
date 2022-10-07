// See https://aka.ms/new-console-template for more information
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.Principal;
using egg;
using System.Runtime.CompilerServices;

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
            System.Console.WriteLine("-b --sbc : 字节码方式运行");
            System.Console.WriteLine("-c --sc : 汇编文件方式运行");
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
                        if (args[i] == "-b" || args[i] == "--sbc") file = "sbc";
                        if (args[i] == "-c" || args[i] == "--sc") file = "sc";
                        if (args[i] == "-?" || args[i] == "-h" || args[i] == "--help") file = "help";
                    }
                    System.Console.Title = $"SIR语言转换工具 Ver:{it.Version} - {path}";
                    string libsPath = $"{it.ExecPath}libs";
                    eggs.IO.CreateFolder(libsPath);
                    switch (file) {
                        case "sbc":
                            byte[] bytes = egg.File.BinaryFile.ReadAllBytes(path, false);
                            using (Sevm.Sir.SirScript ss = Sevm.Sir.Parser.GetScript(bytes)) {
                                using (Sevm.ScriptEngine engine = new Sevm.ScriptEngine(ss)) {
                                    engine.Paths.Add(it.GetClosedDirectoryPath(System.IO.Path.GetDirectoryName(path)));
                                    engine.Paths.Add(it.GetClosedDirectoryPath(libsPath));
                                    engine.Execute();
                                }
                            }
                            break;
                        case "sc":
                            string script = eggs.IO.GetUtf8FileContent(path);
                            using (Sevm.Sir.SirScript ss = Sevm.Sir.Parser.GetScript(script)) {
                                Console.WriteLine("[SIR]");
                                Console.WriteLine(ss.ToString());
                                using (Sevm.ScriptEngine engine = new Sevm.ScriptEngine(ss)) {
                                    engine.Paths.Add(it.GetClosedDirectoryPath(System.IO.Path.GetDirectoryName(path)));
                                    engine.Paths.Add(it.GetClosedDirectoryPath(libsPath));
                                    Console.WriteLine("[Excute]");
                                    engine.Execute();
                                }
                            }
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
