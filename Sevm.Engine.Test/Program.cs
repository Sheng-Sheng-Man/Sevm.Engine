// See https://aka.ms/new-console-template for more information
// "X:\Projects.Svn\Egg\ShengShengMan\ScriptExample\函数.ssm.sc"
using Sevm.Engine;
using Sevm.Sir;

namespace Sevm.Engine.Test {
    public static class Program {

        public static void Main(string[] args) {
            //Console.WriteLine("Hello, World!");
            if (args.Length > 0) {
                string path = args[0];
                using (var f = System.IO.File.Open(path, FileMode.Open)) {
                    List<byte> ls = new List<byte>();
                    byte[] buffer = new byte[1024];
                    int len;
                    do {
                        len = f.Read(buffer, 0, buffer.Length);
                        for (int i = 0; i < len; i++) {
                            ls.Add(buffer[i]);
                        }
                    } while (len > 0);
                    using (var script = Parser.GetScript(System.Text.Encoding.UTF8.GetString(ls.ToArray()))) {
                        Console.WriteLine("[SIR]");
                        Console.WriteLine(script.ToString());
                        Console.WriteLine("[EXECUTE]");
                        using (Sevm.ScriptEngine engine = new Sevm.ScriptEngine(script)) {
                            engine.OnRegFunction += Engine_OnRegFunction;
                            engine.Execute();
                        }
                    }
                    f.Close();
                }
                return;
            }
            // 默认输出测试程序
            using (SirScript sir = new SirScript()) {

                /*
                 * 此段代码模拟简单的程序
                 * string str="Hello World"
                 * print(str)
                 */

                // 添加引入
                sir.Imports.Add(SirImportTypes.Use, "控制台");
                // 添加数据
                sir.Datas.Add(1, "Hello World");
                sir.Datas.Add(2, "print");
                // 添加变量定义
                sir.Defines.Add(3, "str");
                // 添加函数定义
                sir.Funcs.Add(SirScopeTypes.Public, 1, "main");
                // @1
                sir.Codes.Add(SirCodeInstructionTypes.Label, SirExpression.Label(1));
                // lea #2, $1
                sir.Codes.Add(SirCodeInstructionTypes.Lea, SirExpression.Storage(2), SirExpression.Variable(1));
                // ptr $3, #2
                sir.Codes.Add(SirCodeInstructionTypes.Ptr, SirExpression.Variable(3), SirExpression.Storage(2));
                // ptr $4
                sir.Codes.Add(SirCodeInstructionTypes.Ptr, SirExpression.Variable(4));
                // list $4
                sir.Codes.Add(SirCodeInstructionTypes.List, SirExpression.Variable(4));
                // lea #2, $3
                sir.Codes.Add(SirCodeInstructionTypes.Lea, SirExpression.Storage(2), SirExpression.Variable(3));
                // ptrl $4, 0, #2
                sir.Codes.Add(SirCodeInstructionTypes.Ptrl, SirExpression.Variable(4), 0, SirExpression.Storage(2));
                // lea #0, $4
                sir.Codes.Add(SirCodeInstructionTypes.Lea, SirExpression.Storage(0), SirExpression.Variable(4));
                // call [0], $2
                sir.Codes.Add(SirCodeInstructionTypes.Call, SirExpression.IntPtr(0), SirExpression.Variable(2));
                Console.WriteLine("[SIR]");
                Console.WriteLine(sir.ToString());
                Console.WriteLine("[EXECUTE]");
                using (Sevm.ScriptEngine engine = new Sevm.ScriptEngine(sir)) {
                    engine.OnRegFunction += Engine_OnRegFunction;
                    engine.Execute();
                }
            }
        }

        private static void Engine_OnRegFunction(object sender, Sevm.Engine.ScrpitEventArgs e) {
            Sevm.ScriptEngine engine = (Sevm.ScriptEngine)sender;
            switch (e.Func) {
                case "控制台":
                    engine.Reg("print", (Params args) => {
                        Console.Write(args[0].ToString());
                        return 0;
                    });
                    // 输出函数
                    engine.Reg("控制台输出", (Params args) => {
                        Sevm.Engine.Memory.Object obj = (Sevm.Engine.Memory.Object)args[0];
                        //for (int i = 0; i < obj.KeyList.Count; i++) {
                        //    Console.WriteLine($"{obj.KeyList[i].ToString()}={obj.ValueList[i].ToString()}");
                        //}
                        if (obj.ContainsKey("内容")) {
                            Console.Write(obj["内容"].ToString());
                        } else {
                            Console.Write(obj.ValueList[0].ToString());
                        }
                        return 0;
                    });
                    // 输出函数
                    engine.Reg("控制台换行输出", (Params args) => {
                        Sevm.Engine.Memory.Object obj = (Sevm.Engine.Memory.Object)args[0];
                        if (obj.ContainsKey("内容")) {
                            Console.WriteLine(obj["内容"].ToString());
                        } else {
                            Console.WriteLine(obj.ValueList[0].ToString());
                        }
                        return 0;
                    });
                    // 读取数字
                    engine.Reg("控制台读取数字", (Params args) => {
                        return double.Parse(Console.ReadLine());
                    });
                    break;
                case "系统":
                    engine.Reg("获取系统运行毫秒数", (Params args) => {
                        return Environment.TickCount;
                    });
                    break;
            }
        }

    }
}

