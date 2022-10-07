using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Sevm.Engine;
using Sevm.Sir;

namespace Sevm {

    /// <summary>
    /// 脚本引擎
    /// </summary>
    public class ScriptEngine : IDisposable {

        /// <summary>
        /// 原生函数接口
        /// </summary>
        /// <param name="args"></param>
        public delegate Sevm.MemoryPtr NativeFunction(Engine.NativeFunctionArgs args);

        /// <summary>
        /// 注册原生函数集合
        /// </summary>
        internal NativeFunctions NativeFunctions { get; private set; }

        /// <summary>
        /// 目录集合
        /// </summary>
        public List<string> Paths { get; private set; }

        /// <summary>
        /// 外部程序集合
        /// </summary>
        public ScriptLibraries Libraries { get; private set; }

        /// <summary>
        /// 脚本对象
        /// </summary>
        public SirScript Script { get; private set; }

        /// <summary>
        /// 内存集合
        /// </summary>
        public Sevm.Memory Memory { get; private set; }

        /// <summary>
        /// 寄存器集合
        /// </summary>
        public ScriptRegisters Registers { get; private set; }

        /// <summary>
        /// 变量集合
        /// </summary>
        public ScriptVariables Variables { get; private set; }

        /// <summary>
        /// 函数集合
        /// </summary>
        public ScriptLabels Labels { get; private set; }

        /// <summary>
        /// 注册函数集合
        /// </summary>
        internal ScriptFunctions RegisterFunctions { get; private set; }

        /// <summary>
        /// 获取父引擎
        /// </summary>
        public ScriptEngine Parent { get; private set; }

        /// <summary>
        /// 获取根引擎
        /// </summary>
        public ScriptEngine Root {
            get {
                if (this.Parent == null) return this;
                return this.Parent.Root;
            }
        }

        /// <summary>
        /// 获取公共变量值
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Sevm.MemoryPtr GetPublicVariable(string name) {
            for (int i = 0; i < this.Variables.Count; i++) {
                var def = this.Variables[i];
                if (def.ScopeType == SirScopeTypes.Public) {
                    if (def.Name == name) return def.MemoryPtr;
                }
            }
            return Sevm.MemoryPtr.None;
        }

        /// <summary>
        /// 设置公共变量值
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetPublicVariable(string name, Sevm.MemoryPtr value) {
            for (int i = 0; i < this.Variables.Count; i++) {
                var def = this.Variables[i];
                if (def.ScopeType == SirScopeTypes.Public) {
                    if (def.Name == name) {
                        this.Memory.Set(def.MemoryPtr, value);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 设置公共变量值
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetPublicVariable(string name, double value) {
            for (int i = 0; i < this.Variables.Count; i++) {
                var def = this.Variables[i];
                if (def.ScopeType == SirScopeTypes.Public) {
                    if (def.Name == name) {
                        this.Memory.Set(def.MemoryPtr, value);
                        return true;
                    }
                }
            }
            return false;
        }

        // 初始化
        private void init() {
            this.Libraries = new ScriptLibraries();
            this.Variables = new ScriptVariables();
            this.Labels = new ScriptLabels();
            if (this.Paths == null) this.Paths = new List<string>();
            if (this.Registers == null) this.Registers = new ScriptRegisters();
            if (this.Libraries == null) this.Libraries = new ScriptLibraries();
            if (this.Memory == null) this.Memory = new Memory();
            if (this.RegisterFunctions == null) this.RegisterFunctions = new ScriptFunctions();
            if (this.NativeFunctions == null) this.NativeFunctions = new NativeFunctions();
        }

        /// <summary>
        /// 实例化一个脚本引擎
        /// </summary>
        public ScriptEngine() {
            this.Script = new SirScript();
            this.Parent = null;
            this.init();
        }

        /// <summary>
        /// 实例化一个脚本引擎
        /// </summary>
        public ScriptEngine(SirScript script) {
            this.Script = script;
            this.Parent = null;
            this.init();
        }

        /// <summary>
        /// 实例化一个脚本引擎
        /// </summary>
        /// <param name="script"></param>
        /// <param name="engine">调用引擎</param>
        public ScriptEngine(SirScript script, ScriptEngine engine) {
            this.Script = script;
            this.Parent = engine;
            this.Memory = engine.Memory;
            this.RegisterFunctions = engine.RegisterFunctions;
            this.Libraries = engine.Libraries;
            this.Registers = engine.Registers;
            this.Paths = engine.Paths;
            this.NativeFunctions = new NativeFunctions();
            this.init();
        }

        // 获取标签定义行
        private int GetPublicLabelIndex(string Name) {
            foreach (var item in Labels) {
                var lab = item.Value;
                if (lab.ScopeType == SirScopeTypes.Public) {
                    if (lab.Name == Name) {
                        return item.Key;
                    }
                }
            }
            //for (int i = 0; i < Labels.Count; i++) {
            //    var lab = Labels[i];
            //    if (lab != null) {
            //        if (lab.ScopeType == SirScopeTypes.Public) {
            //            if (lab.Name == Name) {
            //                return i;
            //            }
            //        }
            //    }
            //}
            return -1;
        }

        /// <summary>
        /// 向表达式赋值
        /// </summary>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <exception cref="Exception"></exception>
        public int Equal(int param1, int param2) {
            // 先判断两个参数是否指针相等
            Sevm.MemoryPtr ptr1 = this.Variables[param1].MemoryPtr;
            Sevm.MemoryPtr ptr2 = this.Variables[param2].MemoryPtr;
            if (ptr1.Content == ptr2.Content) return 1;
            // 最后判断两个参数的实际值是否相等
            if (ptr1 == ptr2) return 1;
            switch (ptr1.Type) {
                case MemoryTypes.String:
                    return ptr1.GetString() == ptr2.GetString() ? 1 : 0;
                case MemoryTypes.Integer:
                case MemoryTypes.Long:
                case MemoryTypes.Double:
                case MemoryTypes.Value:
                    switch (ptr2.Type) {
                        case MemoryTypes.Integer:
                        case MemoryTypes.Long:
                        case MemoryTypes.Double:
                        case MemoryTypes.Value:
                            return ptr1.GetDouble() == ptr2.GetDouble() ? 1 : 0;
                        default: return 0;
                    }
                default: return 0;
            }
        }

        // 执行函数
        private int ExecuteFunc(int funcIndex) {
            int line = this.Labels[funcIndex].IntPtr;
            if (line < 0) throw new SirException($"函数'@{funcIndex}'入口缺失");
            try {
                while (line < this.Script.Codes.Count) {
                    Debug.WriteLine($"[{this.Script.Codes[line].SourceLine}] -> {line} {this.Script.Codes[line].ToString()}");
                    var code = this.Script.Codes[line];
                    switch (code.Instruction) {
                        // 空指令
                        case SirCodeInstructionTypes.None:
                        case SirCodeInstructionTypes.Label:
                            line++; break;
                        // 二、数据指令
                        case SirCodeInstructionTypes.Mov:
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, this.Variables[code.Exp2].MemoryPtr);
                            //this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, this.Variables[code.Exp2].MemoryPtr);
                            line++; break;
                        case SirCodeInstructionTypes.Ptr:
                            if (code.Exp2 > 0) {
                                // 新建变量或修改变量指针
                                if (this.Variables[code.Exp1] == null) {
                                    this.Variables[code.Exp1] = new ScriptVariable(SirScopeTypes.Private, "", Sevm.MemoryPtr.CreateFromAddr(this.Variables[code.Exp2].MemoryPtr.GetLong()));
                                } else {
                                    this.Variables[code.Exp1].MemoryPtr = Sevm.MemoryPtr.CreateFromAddr(this.Variables[code.Exp2].MemoryPtr.GetLong());
                                }
                            } else {
                                // 新建变量或修改变量指针
                                if (this.Variables[code.Exp1] == null) {
                                    this.Variables[code.Exp1] = new ScriptVariable(SirScopeTypes.Private, "", Sevm.MemoryPtr.None);
                                } else {
                                    this.Variables[code.Exp1].MemoryPtr = Sevm.MemoryPtr.None;
                                }
                            }
                            line++; break;
                        case SirCodeInstructionTypes.Lea:
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, (double)this.Variables[code.Exp2].MemoryPtr.Content);
                            line++; break;
                        case SirCodeInstructionTypes.Int:
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, (double)this.Variables[code.Exp2].MemoryPtr.GetLong());
                            line++; break;
                        case SirCodeInstructionTypes.Frac:
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, this.Variables[code.Exp2].MemoryPtr.GetDouble() - this.Variables[code.Exp2].MemoryPtr.GetLong());
                            line++; break;
                        // 三、类型操作指令
                        case SirCodeInstructionTypes.List:
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, this.Memory.CreateList());
                            line++; break;
                        case SirCodeInstructionTypes.Ptrl: // 设置列表内容指针
                            Sevm.MemoryList list = this.Variables[code.Exp1].MemoryPtr.GetList(this.Memory);
                            // 动态添加列表项
                            int ptrListIndex = this.Variables[code.Exp2].MemoryPtr.GetInteger();
                            long ptrListValue = this.Variables[code.Exp3].MemoryPtr.GetLong();
                            // 判断赋值的指针是否大于0
                            if (ptrListValue > 0) {
                                // 自动添加数量
                                if (list.Count <= ptrListIndex) {
                                    for (int i = list.Count; i <= ptrListIndex; i++) {
                                        list.AddItem();
                                    }
                                }
                                list.SetItemContent(ptrListIndex, new IntPtr(ptrListValue));
                            } else {
                                list.SetItemContent(ptrListIndex, IntPtr.Zero);
                                //// 清理列表
                                //for (int i = ptrList.Values.Count - 1; i >= ptrListIndex; i--) {
                                //    ptrList.Values.RemoveAt(i);
                                //}
                            }
                            line++; break;
                        case SirCodeInstructionTypes.Leal: // 获取列表内容指针
                            Sevm.MemoryPtr ptrList = this.Variables[code.Exp2].MemoryPtr;
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, (double)ptrList.IntPtr);
                            line++; break;
                        case SirCodeInstructionTypes.Idx: // 获取内容匹配索引
                            list = this.Variables[code.Exp2].MemoryPtr.GetList(this.Memory);
                            // 动态添加列表项
                            int idx = -1;
                            string str = this.Variables[code.Exp3].MemoryPtr.GetString();
                            idx = list.GetIndex(str);
                            // 赋值内容
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, (double)idx);
                            line++; break;
                        case SirCodeInstructionTypes.Join: // 连接字符串
                            list = this.Variables[code.Exp2].MemoryPtr.GetList(this.Memory);
                            string sz = list.ConvertListToString();
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, sz);
                            line++; break;
                        case SirCodeInstructionTypes.Cnt:
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, (double)this.Variables[code.Exp2].MemoryPtr.GetList(this.Memory).Count);
                            line++; break;
                        case SirCodeInstructionTypes.Obj:
                            Sevm.MemoryPtr ptrObj = this.Memory.CreateObject();
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, ptrObj);
                            line++; break;
                        //case SirCodeInstructionTypes.Ptrk: // 设置对象键列表指针
                        //    obj = (Engine.Memory.Object)this.Variables[code.Exp1].MemoryPtr;
                        //    obj.Keys = this.Variables[code.Exp2].MemoryPtr;
                        //    line++; break;
                        //case SirCodeInstructionTypes.Ptrv: // 设置对象值列表指针
                        //    obj = (Engine.Memory.Object)this.Variables[code.Exp1].MemoryPtr;
                        //    obj.Values = this.Variables[code.Exp2].MemoryPtr;
                        //    line++; break;
                        case SirCodeInstructionTypes.Leak: // 获取对象键列表指针
                            Sevm.MemoryObject obj = this.Variables[code.Exp2].MemoryPtr.GetObject(this.Memory);
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, (double)obj.GetKeys().IntPtr);
                            line++; break;
                        case SirCodeInstructionTypes.Leav: // 获取对象值列表指针
                            obj = this.Variables[code.Exp2].MemoryPtr.GetObject(this.Memory);
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, (double)obj.GetValues().IntPtr);
                            line++; break;
                        // 四、运算操作指令
                        case SirCodeInstructionTypes.Add:
                            this.Variables[code.Exp1].MemoryPtr.Add(this.Variables[code.Exp2].MemoryPtr);
                            line++; break;
                        case SirCodeInstructionTypes.Sub:
                            this.Variables[code.Exp1].MemoryPtr.Sub(this.Variables[code.Exp2].MemoryPtr);
                            line++; break;
                        case SirCodeInstructionTypes.Mul:
                            this.Variables[code.Exp1].MemoryPtr.Mul(this.Variables[code.Exp2].MemoryPtr);
                            line++; break;
                        case SirCodeInstructionTypes.Div:
                            this.Variables[code.Exp1].MemoryPtr.Div(this.Variables[code.Exp2].MemoryPtr);
                            line++; break;
                        // 五、逻辑操作指令
                        case SirCodeInstructionTypes.Not:
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, (double)(this.Variables[code.Exp1].MemoryPtr.GetInteger() > 0 ? 0 : 1));
                            line++; break;
                        case SirCodeInstructionTypes.And:
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, (double)(this.Variables[code.Exp1].MemoryPtr.GetInteger() & this.Variables[code.Exp2].MemoryPtr.GetInteger()));
                            line++; break;
                        case SirCodeInstructionTypes.Or:
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, (double)(this.Variables[code.Exp1].MemoryPtr.GetInteger() | this.Variables[code.Exp2].MemoryPtr.GetInteger()));
                            line++; break;
                        case SirCodeInstructionTypes.Xor:
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, (double)(this.Variables[code.Exp1].MemoryPtr.GetInteger() ^ this.Variables[code.Exp2].MemoryPtr.GetInteger()));
                            line++; break;
                        // 六、比较指令
                        case SirCodeInstructionTypes.Equal:
                            double val = (double)Equal(code.Exp2, code.Exp3);
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, val);
                            line++; break;
                        case SirCodeInstructionTypes.Large:
                            double dbValue2 = this.Variables[code.Exp2].MemoryPtr.GetDouble();
                            double dbValue3 = this.Variables[code.Exp3].MemoryPtr.GetDouble();
                            Debug.WriteLine($"[{this.Script.Codes[line].SourceLine}] -> {dbValue2}>{dbValue3}?");
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, dbValue2 > dbValue3 ? 1.0 : 0);
                            line++; break;
                        case SirCodeInstructionTypes.Small:
                            this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, this.Variables[code.Exp2].MemoryPtr.GetDouble() < this.Variables[code.Exp2].MemoryPtr.GetDouble() ? 1.0 : 0);
                            line++; break;
                        // 七、区域操作指令
                        case SirCodeInstructionTypes.Jmp:
                            line = this.Labels[code.Exp1].IntPtr;
                            break;
                        case SirCodeInstructionTypes.Jmpf:
                            int nValue1 = this.Variables[code.Exp1].MemoryPtr.GetInteger();
                            if (nValue1 > 0) {
                                //line = this.Labels[(int)this.Variables[code.Exp1].MemoryPtr.Content].IntPtr;
                                line = this.Labels[code.Exp2].IntPtr;
                                break;
                            }
                            line++; break;
                        case SirCodeInstructionTypes.Call:
                            var pf = this.Variables[code.Exp2].MemoryPtr;
                            if (pf.Type == MemoryTypes.Function) {
                                int retIndex = ExecuteFunc((int)this.Variables[code.Exp2].MemoryPtr.Content);
                                if (retIndex > 0) this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, this.Variables[retIndex].MemoryPtr);
                            } else {
                                string name = this.Variables[code.Exp2].MemoryPtr.GetString();
                                // 判断函数是否已经注册
                                if (!this.RegisterFunctions.ContainsKey(name)) throw new SirException(code.SourceLine, line, $"未找到外部函数'{name}'");
                                // 读取注册函数
                                Sevm.MemoryPtr func = this.RegisterFunctions[name];
                                if (func.Type == MemoryTypes.NativeFunction) {
                                    // 生成参数
                                    Sevm.MemoryList arg = Sevm.MemoryPtr.CreateFromIntPtr(new IntPtr(this.Variables[1].MemoryPtr.GetLong())).GetList(this.Memory);
                                    Params args = new Params();
                                    for (int i = 0; i < arg.Count; i++) {
                                        args.Add(arg.GetItemContent(i));
                                    }
                                    // 执行原生函数并返回结果
                                    try {
                                        int funcIdx = func.GetInteger();
                                        Sevm.MemoryPtr res = this.NativeFunctions[funcIdx](new NativeFunctionArgs(this.Memory, args));
                                        this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, res);
                                    } catch (Exception ex) {
                                        throw new SirException(SirExceptionTypes.General, code.SourceLine, line, $"外部函数'{name}'执行发生异常", ex);
                                    }
                                } else if (func.Type == MemoryTypes.Function) {
                                    // 读取自定义函数
                                    var lib = this.Libraries[this.Memory.GetFunctionLibrary(func)];
                                    int fnIndex = lib.Script.Funcs[this.Memory.GetFunctionIndex(func)].Index;
                                    // 执行并返回内容
                                    using (var engine = new ScriptEngine(lib.Script, this.Root)) {
                                        Sevm.MemoryPtr res = engine.Execute(fnIndex, null, false);
                                        this.Memory.Set(this.Variables[code.Exp1].MemoryPtr, res);
                                    }
                                } else {
                                    throw new SirException(code.SourceLine, line, $"函数'{name}'指针指向的内存不为函数");
                                }
                            }
                            line++; break;
                        case SirCodeInstructionTypes.Ret: return code.Exp1;
                        default: throw new SirException(code.SourceLine, line, $"不支持的指令类型'{code.Instruction.ToString()}'");
                    }
                }
                return 0;
            } catch (Exception ex) {
                throw new SirException(SirExceptionTypes.General, this.Script.Codes[line].SourceLine, line, $"指令执行'{this.Script.Codes[line].ToString().Trim()}'发生异常", ex);
            }
        }

        // 执行初始化
        private void ExecuteInit(Params args, bool clear) {
            // 初始化
            this.Variables.Clear();
            for (int i = 1; i <= 10; i++) {
                this.Variables[i] = new ScriptVariable(SirScopeTypes.Private, "", new MemoryPtr() { Type = MemoryTypes.Value });
            }
            this.Labels.Clear();
            // 初始化存储空间
            if (clear) {
                this.Registers.Clear();
                //this.Memory.Clear();
                //this.Memory.Add(Value.None);
                this.Libraries.Clear();
                this.RegisterFunctions.Clear();
            }
            // 加载函数集
            for (int i = 0; i < this.Script.Imports.Count; i++) {
                var import = this.Script.Imports[i];
                switch (import.ImportType) {
                    case SirImportTypes.Use:
                        //this.OnRegFunction(this, new ScrpitEventArgs() { Func = import.Content });
                        // 加载.Net动态库
                        bool found = false;
                        for (int j = 0; j < this.Paths.Count; j++) {
                            string path = this.Paths[j];
                            string file = $"{path}{import.Content}.dll";
                            if (System.IO.File.Exists(file)) {
                                // 判断文件是否已经加载存在
                                // 加载动态库
                                var dll = Assembly.LoadFrom(file);
                                // 获取库中的所有类
                                var tps = dll.GetTypes();
                                foreach (var tp in tps) {
                                    // 读取库中有特性定义的类
                                    ScriptAttribute clsScript = tp.GetCustomAttribute<ScriptAttribute>();
                                    if (clsScript != null) {
                                        // 获取类中所有的函数
                                        var methods = tp.GetMethods();
                                        foreach (var method in methods) {
                                            // 读取库中有特性定义的函数
                                            ScriptAttribute clsMethod = method.GetCustomAttribute<ScriptAttribute>();
                                            if (clsMethod != null) {
                                                string name = clsScript.Name + clsMethod.Name;
                                                // 判断函数是否存在
                                                if (!this.RegisterFunctions.ContainsKey(name)) {
                                                    ScriptEngine.NativeFunction fn = (ScriptEngine.NativeFunction)Delegate.CreateDelegate(typeof(ScriptEngine.NativeFunction), method);
                                                    // 注册新函数
                                                    int funPtr = this.NativeFunctions.Count;
                                                    this.NativeFunctions.Add(fn);
                                                    this.RegisterFunctions[name] = this.Memory.CreateNativeFunction(funPtr);
                                                }
                                            }
                                        }
                                    }
                                }
                                found = true;
                                break;
                            }
                        }
                        if (!found) throw new SirException($"未发现名称为'{import.Content}.dll'的.Net动态库");
                        break;
                    case SirImportTypes.Lib:
                        // 从所有路径中查找sbc文件
                        found = false;
                        for (int j = 0; j < this.Paths.Count; j++) {
                            string path = this.Paths[j];
                            string file = $"{path}{import.Content}.sbc";
                            if (System.IO.File.Exists(file)) {
                                found = true;
                                // 判断文件是否已经加载存在
                                if (this.Libraries.ContainsPath(file)) break;
                                // 加载文件内容
                                using (var f = System.IO.File.Open(file, FileMode.Open)) {
                                    List<byte> ls = new List<byte>();
                                    byte[] buffer = new byte[4096];
                                    int len;
                                    do {
                                        len = f.Read(buffer, 0, buffer.Length);
                                        for (int k = 0; k < len; k++) {
                                            ls.Add(buffer[k]);
                                        }
                                    } while (len > 0);
                                    // 添加库文件脚本
                                    this.Libraries.Add(file, Parser.GetScript(ls.ToArray()));
                                    f.Close();
                                }
                                break;
                            }
                        }
                        if (!found) throw new SirException($"未发现名称为'{import.Content}.sbc'的字节码脚本文件");
                        break;
                    default: throw new SirException($"不支持的加载方式'{import.ImportType.ToString()}'");
                }
            }
            // 注册程序集中的所有函数
            for (int i = 0; i < this.Libraries.Count; i++) {
                var lib = this.Libraries[i];
                for (int j = 0; j < lib.Script.Funcs.Count; j++) {
                    var fun = lib.Script.Funcs[j];
                    // 只注册公开函数
                    if (fun.Scope == SirScopeTypes.Public) {
                        // 判断函数是否存在
                        if (!this.RegisterFunctions.ContainsKey(fun.Name)) {
                            // 注册新函数
                            this.RegisterFunctions[fun.Name] = this.Memory.CreateFunction(i, j);
                        }
                    }
                }
            }
            // 填充数据
            for (int i = 0; i < this.Script.Datas.Count; i++) {
                var data = this.Script.Datas[i];
                switch (data.DataType) {
                    case SirDataTypes.None:
                        // 添加空数据
                        this.Variables[data.Index] = new ScriptVariable(SirScopeTypes.Private, "", Sevm.MemoryPtr.None);
                        //this.Memory[data.IntPtr] = Engine.Memory.Value.None;
                        break;
                    case SirDataTypes.Number:
                        // 添加数值
                        this.Variables[data.Index] = new ScriptVariable(SirScopeTypes.Private, "", this.Memory.CreateDouble(data.GetNumber()));
                        //this.Memory[data.IntPtr] = data.GetNumber();
                        break;
                    case SirDataTypes.String:
                        // 添加数值
                        this.Variables[data.Index] = new ScriptVariable(SirScopeTypes.Private, "", this.Memory.CreateString(data.GetString()));
                        //this.Memory[data.IntPtr] = data.GetString();
                        break;
                    default: throw new SirException($"不支持的数据类型'{data.DataType.ToString()}'");
                }
            }
            // 填充变量
            for (int i = 0; i < this.Script.Defines.Count; i++) {
                var def = this.Script.Defines[i];
                this.Variables[def.Index] = new ScriptVariable(def.Scope, def.Name, Sevm.MemoryPtr.None);
            }
            // 填充标签
            for (int i = 0; i < this.Script.Funcs.Count; i++) {
                var fn = this.Script.Funcs[i];
                this.Labels[fn.Index] = new ScriptLabel(fn.Scope, fn.Name, 0);
            }
            for (int i = 0; i < this.Script.Codes.Count; i++) {
                var code = this.Script.Codes[i];
                if (code.Instruction == SirCodeInstructionTypes.Label) {
                    //int labIndex = (int)this.Variables[code.Exp1].MemoryPtr.Content;
                    if (this.Labels.ContainsKey(code.Exp1)) {
                        this.Labels[code.Exp1].IntPtr = i;
                    } else {
                        this.Labels[code.Exp1] = new ScriptLabel(SirScopeTypes.Private, "", i);
                    }
                }
            }
            if (args != null) {
                // 将参数填充到
                Sevm.MemoryPtr ptr = this.Memory.CreateList();
                //Engine.Memory.List list = new Engine.Memory.List(this.Memory);
                //this.Memory[ptr] = list;
                this.Registers[0] = (long)ptr.IntPtr;
                for (int i = 0; i < args.Count; i++) {
                    this.Memory.AddListItem(ptr, args[i]);
                }
            }
        }

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <param name="func"></param>
        /// <param name="args"></param>
        /// <param name="clear"></param>
        /// <returns></returns>
        public Sevm.MemoryPtr Execute(int func, Params args, bool clear) {
            // 执行初始化
            ExecuteInit(args, clear);
            // 执行函数
            int res = ExecuteFunc(func);
            return res > 0 ? this.Variables[res].MemoryPtr : Sevm.MemoryPtr.None;
        }

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <param name="func"></param>
        /// <param name="args"></param>
        /// <param name="clear"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Sevm.MemoryPtr Execute(string func, Params args, bool clear) {
            // 执行初始化
            ExecuteInit(args, clear);
            // 执行函数
            int idx = GetPublicLabelIndex(func);
            if (idx < 0) throw new SirException($"函数'{func}'入口缺失");
            return ExecuteFunc(idx);
        }

        /// <summary>
        /// 执行程序
        /// </summary>
        /// <returns></returns>
        public Sevm.MemoryPtr Execute() {
            return Execute("main", new Params(), true);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() {
            if (this.Parent == null) {
                this.Libraries.Clear();
                this.Registers.Clear();
                this.Memory.Dispose();
                this.RegisterFunctions.Clear();
            }
            this.Variables.Clear();
            this.Labels.Clear();
        }
    }
}
