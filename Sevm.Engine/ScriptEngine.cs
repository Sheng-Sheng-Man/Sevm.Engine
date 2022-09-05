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
            for (int i = 0; i < Labels.Count; i++) {
                var lab = Labels[i];
                if (lab != null) {
                    if (lab.ScopeType == SirScopeTypes.Public) {
                        if (lab.Name == Name) {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 获取表达式对应的值
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Sevm.MemoryPtr GetValue(SirExpression param) {
            switch (param.Type) {
                case SirExpressionTypes.None: return Sevm.MemoryPtr.None;
                case SirExpressionTypes.Value: return param.Content;
                case SirExpressionTypes.IntPtr:
                    if (param.Content > 0) {
                        return Sevm.MemoryPtr.CreateFromIntPtr(new IntPtr(param.Content));
                    } else {
                        return Sevm.MemoryPtr.None;
                    }
                case SirExpressionTypes.Register: return this.Registers[param.Content];
                case SirExpressionTypes.Variable: return this.Variables[param.Content].MemoryPtr;
                default: throw new SirException($"不支持的表达式获取类型'{param.Type.ToString()}'");
            }
        }

        /// <summary>
        /// 向表达式赋值
        /// </summary>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <exception cref="Exception"></exception>
        public void SetValue(SirExpression param, Sevm.MemoryPtr value) {
            switch (param.Type) {
                case SirExpressionTypes.IntPtr:
                    if (param.Content <= 0) return;
                    throw new SirException($"不支持的表达式赋值类型'{param.Type.ToString()}'");
                case SirExpressionTypes.Register:
                    this.Registers[param.Content] = value.GetLong();
                    break;
                case SirExpressionTypes.Variable:
                    this.Memory.Set(this.Variables[param.Content].MemoryPtr, value);
                    break;
                default: throw new SirException($"不支持的表达式赋值类型'{param.Type.ToString()}'");
            }
        }

        /// <summary>
        /// 向表达式赋值
        /// </summary>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <exception cref="Exception"></exception>
        public void SetValue(SirExpression param, string value) {
            switch (param.Type) {
                case SirExpressionTypes.Variable:
                    this.Variables[param.Content].MemoryPtr = this.Memory.CreateString(value);
                    break;
                default: throw new SirException($"不支持的表达式字符串赋值类型'{param.Type.ToString()}'");
            }
        }


        /// <summary>
        /// 向表达式赋值
        /// </summary>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <exception cref="Exception"></exception>
        public void SetValue(SirExpression param, double value) {
            switch (param.Type) {
                case SirExpressionTypes.IntPtr:
                    // 判断是否为空指针
                    if (param.Content > 0) {
                        this.Memory.Set(Sevm.MemoryPtr.CreateFromIntPtr(new IntPtr(param.Content)), value);
                        break;
                    } else {
                        return;
                    }
                case SirExpressionTypes.Register:
                    this.Registers[param.Content] = (long)value; break;
                case SirExpressionTypes.Variable:
                    this.Memory.Set(this.Variables[param.Content].MemoryPtr, value);
                    break;
                default: throw new SirException($"不支持的表达式赋值类型'{param.Type.ToString()}'");
            }
        }

        /// <summary>
        /// 向表达式赋值
        /// </summary>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <exception cref="Exception"></exception>
        public int Equal(SirExpression param1, SirExpression param2) {
            // 如果两个参数都是值对象，则判断两个值是否相等
            if (param1.Type == SirExpressionTypes.Value && param2.Type == SirExpressionTypes.Value) return param1.Content == param2.Content ? 1 : 0;
            if (param1.Type == SirExpressionTypes.Value || param1.Type == SirExpressionTypes.Register || param2.Type == SirExpressionTypes.Value || param2.Type == SirExpressionTypes.Register) {
                var value1 = GetValue(param1);
                var value2 = GetValue(param2);
                return value1.GetDouble() == value2.GetDouble() ? 1 : 0;
            }
            // 先判断两个参数是否指针相等
            Sevm.MemoryPtr ptr1 = null;
            Sevm.MemoryPtr ptr2 = null;
            switch (param1.Type) {
                case SirExpressionTypes.IntPtr:
                    ptr1 = Sevm.MemoryPtr.CreateFromIntPtr(new IntPtr(param1.Content));
                    break;
                case SirExpressionTypes.Variable:
                    ptr1 = this.Variables[param1.Content].MemoryPtr;
                    break;
                default: throw new SirException($"不支持的表达式赋值类型'{param1.Type.ToString()}'");
            }
            switch (param2.Type) {
                case SirExpressionTypes.IntPtr:
                    ptr2 = Sevm.MemoryPtr.CreateFromIntPtr(new IntPtr(param2.Content));
                    break;
                case SirExpressionTypes.Variable:
                    ptr2 = this.Variables[param2.Content].MemoryPtr;
                    break;
                default: throw new SirException($"不支持的表达式赋值类型'{param2.Type.ToString()}'");
            }
            if (ptr1.IntPtr == ptr2.IntPtr) return 1;
            // 最后判断两个参数的实际值是否相等
            if (ptr1 == ptr2) return 1;
            var val1 = GetValue(param1);
            var val2 = GetValue(param2);
            switch (val1.Type) {
                case MemoryTypes.String:
                    return val1.GetString() == val2.GetString() ? 1 : 0;
                case MemoryTypes.Integer:
                case MemoryTypes.Long:
                case MemoryTypes.Double:
                case MemoryTypes.Value:
                    switch (val2.Type) {
                        case MemoryTypes.Integer:
                        case MemoryTypes.Long:
                        case MemoryTypes.Double:
                        case MemoryTypes.Value:
                            return val1.GetDouble() == val2.GetDouble() ? 1 : 0;
                        default: return 0;
                    }
                default: return 0;
            }
        }

        // 执行函数
        private SirExpression ExecuteFunc(int funcIndex) {
            int line = this.Labels[funcIndex].IntPtr;
            if (line < 0) throw new SirException($"函数'@{funcIndex}'入口缺失");
            try {
                while (line < this.Script.Codes.Count) {
                    Debug.WriteLine($"->{line} {this.Script.Codes[line].ToString()}");
                    var code = this.Script.Codes[line];
                    switch (code.Instruction) {
                        // 空指令
                        case SirCodeInstructionTypes.None:
                        case SirCodeInstructionTypes.Label:
                            line++; break;
                        // 二、数据指令
                        case SirCodeInstructionTypes.Mov:
                            SetValue(code.Exp1, GetValue(code.Exp2));
                            line++; break;
                        case SirCodeInstructionTypes.Ptr:
                            if (code.Exp1.Type != SirExpressionTypes.Variable) throw new SirException($"不支持的表达式赋值类型'{code.Exp1.Type.ToString()}'");
                            if (code.Exp2.Type != SirExpressionTypes.None) {
                                // 新建变量或修改变量指针
                                if (this.Variables[code.Exp1.Content] == null) {
                                    this.Variables[code.Exp1.Content] = new ScriptVariable(SirScopeTypes.Private, "", Sevm.MemoryPtr.CreateFromAddr(GetValue(code.Exp2).GetLong()));
                                } else {
                                    this.Variables[code.Exp1.Content].MemoryPtr = Sevm.MemoryPtr.CreateFromAddr(GetValue(code.Exp2).GetLong());
                                }
                            } else {
                                //// 新建虚拟内存块
                                //int ptr = this.Memory.Count;
                                //this.Memory[ptr] = Engine.Memory.Value.None;
                                // 新建变量或修改变量指针
                                if (this.Variables[code.Exp1.Content] == null) {
                                    this.Variables[code.Exp1.Content] = new ScriptVariable(SirScopeTypes.Private, "", Sevm.MemoryPtr.None);
                                } else {
                                    this.Variables[code.Exp1.Content].MemoryPtr = Sevm.MemoryPtr.None;
                                }
                            }
                            line++; break;
                        case SirCodeInstructionTypes.Lea:
                            if (code.Exp2.Type != SirExpressionTypes.Variable) throw new SirException(code.SourceLine, line, $"不支持的表达式类型'{code.Exp2.Type.ToString()}'");
                            SetValue(code.Exp1, (double)this.Variables[code.Exp2.Content].MemoryPtr.IntPtr);
                            line++; break;
                        case SirCodeInstructionTypes.Int:
                            SetValue(code.Exp1, (double)GetValue(code.Exp2).GetLong());
                            line++; break;
                        case SirCodeInstructionTypes.Frac:
                            SetValue(code.Exp1, GetValue(code.Exp2).GetDouble() - GetValue(code.Exp2).GetLong());
                            line++; break;
                        // 三、类型操作指令
                        case SirCodeInstructionTypes.List:
                            SetValue(code.Exp1, this.Memory.CreateList());
                            line++; break;
                        case SirCodeInstructionTypes.Ptrl: // 设置列表内容指针
                            if (code.Exp1.Type != SirExpressionTypes.Variable) throw new SirException(code.SourceLine, line, $"不支持的表达式类型'{code.Exp1.Type.ToString()}'");
                            Sevm.MemoryList list = this.Variables[code.Exp1.Content].MemoryPtr.GetList(this.Memory);
                            // 动态添加列表项
                            int ptrListIndex = GetValue(code.Exp2).GetInteger();
                            long ptrListValue = GetValue(code.Exp3).GetLong();
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
                            Sevm.MemoryPtr ptrList = this.Variables[code.Exp2.Content].MemoryPtr;
                            SetValue(code.Exp1, (double)ptrList.IntPtr);
                            line++; break;
                        case SirCodeInstructionTypes.Idx: // 获取内容匹配索引
                            list = this.Variables[code.Exp2.Content].MemoryPtr.GetList(this.Memory);
                            // 动态添加列表项
                            int idx = -1;
                            string str = GetValue(code.Exp3).GetString();
                            idx = list.GetIndex(str);
                            // 赋值内容
                            SetValue(code.Exp1, (double)idx);
                            line++; break;
                        case SirCodeInstructionTypes.Join: // 连接字符串
                            list = GetValue(code.Exp2).GetList(this.Memory);
                            string sz = list.ConvertListToString();
                            SetValue(code.Exp1, sz);
                            line++; break;
                        case SirCodeInstructionTypes.Cnt:
                            SetValue(code.Exp1, (double)GetValue(code.Exp2).GetList(this.Memory).Count);
                            line++; break;
                        case SirCodeInstructionTypes.Obj:
                            Sevm.MemoryPtr ptrObj = this.Memory.CreateObject();
                            SetValue(code.Exp1, ptrObj);
                            line++; break;
                        //case SirCodeInstructionTypes.Ptrk: // 设置对象键列表指针
                        //    obj = (Engine.Memory.Object)GetValue(code.Exp1);
                        //    obj.Keys = GetValue(code.Exp2);
                        //    line++; break;
                        //case SirCodeInstructionTypes.Ptrv: // 设置对象值列表指针
                        //    obj = (Engine.Memory.Object)GetValue(code.Exp1);
                        //    obj.Values = GetValue(code.Exp2);
                        //    line++; break;
                        case SirCodeInstructionTypes.Leak: // 获取对象键列表指针
                            Sevm.MemoryObject obj = GetValue(code.Exp2).GetObject(this.Memory);
                            SetValue(code.Exp1, (double)obj.GetKeys().IntPtr);
                            line++; break;
                        case SirCodeInstructionTypes.Leav: // 获取对象值列表指针
                            obj = GetValue(code.Exp2).GetObject(this.Memory);
                            SetValue(code.Exp1, (double)obj.GetValues().IntPtr);
                            line++; break;
                        // 四、运算操作指令
                        case SirCodeInstructionTypes.Add:
                            GetValue(code.Exp1).Add(GetValue(code.Exp2));
                            line++; break;
                        case SirCodeInstructionTypes.Sub:
                            GetValue(code.Exp1).Sub(GetValue(code.Exp2));
                            line++; break;
                        case SirCodeInstructionTypes.Mul:
                            GetValue(code.Exp1).Mul(GetValue(code.Exp2));
                            line++; break;
                        case SirCodeInstructionTypes.Div:
                            GetValue(code.Exp1).Div(GetValue(code.Exp2));
                            line++; break;
                        // 五、逻辑操作指令
                        case SirCodeInstructionTypes.Not:
                            SetValue(code.Exp1, (double)(GetValue(code.Exp1).GetInteger() > 0 ? 0 : 1));
                            line++; break;
                        case SirCodeInstructionTypes.And:
                            SetValue(code.Exp1, (double)(GetValue(code.Exp1).GetInteger() & GetValue(code.Exp2).GetInteger()));
                            line++; break;
                        case SirCodeInstructionTypes.Or:
                            SetValue(code.Exp1, (double)(GetValue(code.Exp1).GetInteger() | GetValue(code.Exp2).GetInteger()));
                            line++; break;
                        case SirCodeInstructionTypes.Xor:
                            SetValue(code.Exp1, (double)(GetValue(code.Exp1).GetInteger() ^ GetValue(code.Exp2).GetInteger()));
                            line++; break;
                        // 六、比较指令
                        case SirCodeInstructionTypes.Equal:
                            double val = (double)Equal(code.Exp2, code.Exp3);
                            SetValue(code.Exp1, val);
                            line++; break;
                        case SirCodeInstructionTypes.Large:
                            SetValue(code.Exp1, GetValue(code.Exp2).GetDouble() > GetValue(code.Exp3).GetDouble() ? 1.0 : 0);
                            line++; break;
                        case SirCodeInstructionTypes.Small:
                            SetValue(code.Exp1, GetValue(code.Exp2).GetDouble() < GetValue(code.Exp3).GetDouble() ? 1.0 : 0);
                            line++; break;
                        // 七、区域操作指令
                        case SirCodeInstructionTypes.Jmp:
                            if (code.Exp1.Type != SirExpressionTypes.Label) throw new SirException(code.SourceLine, line, $"不支持的表达式赋值类型'{code.Exp1.Type.ToString()}'");
                            line = this.Labels[code.Exp1.Content].IntPtr;
                            break;
                        case SirCodeInstructionTypes.Jmpf:
                            if (code.Exp2.Type != SirExpressionTypes.Label) throw new SirException(code.SourceLine, line, $"不支持的表达式赋值类型'{code.Exp1.Type.ToString()}'");
                            if (GetValue(code.Exp1).GetDouble() > 0) {
                                line = this.Labels[code.Exp2.Content].IntPtr;
                                break;
                            }
                            line++; break;
                        case SirCodeInstructionTypes.Call:
                            if (code.Exp2.Type == SirExpressionTypes.Label) {
                                SetValue(code.Exp1, GetValue(ExecuteFunc(code.Exp2.Content)));
                            } else {
                                string name = GetValue(code.Exp2).GetString();
                                // 判断函数是否已经注册
                                if (!this.RegisterFunctions.ContainsKey(name)) throw new SirException(code.SourceLine, line, $"未找到外部函数'{name}'");
                                // 读取注册函数
                                Sevm.MemoryPtr func = this.RegisterFunctions[name];
                                if (func.Type == MemoryTypes.NativeFunction) {
                                    // 生成参数
                                    Sevm.MemoryList arg = Sevm.MemoryPtr.CreateFromIntPtr(new IntPtr(this.Registers[0])).GetList(this.Memory);
                                    Params args = new Params();
                                    for (int i = 0; i < arg.Count; i++) {
                                        args.Add(arg.GetItemContent(i));
                                    }
                                    // 执行原生函数并返回结果
                                    try {
                                        int funcIdx = func.GetInteger();
                                        Sevm.MemoryPtr res = this.NativeFunctions[funcIdx](new NativeFunctionArgs(this.Memory, args));
                                        SetValue(code.Exp1, res);
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
                                        SetValue(code.Exp1, res);
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
                return SirExpression.Value(0);
            } catch (Exception ex) {
                throw new SirException(SirExceptionTypes.General, this.Script.Codes[line].SourceLine, line, $"指令执行'{this.Script.Codes[line].ToString().Trim()}'发生异常", ex);
            }
        }

        // 执行初始化
        private void ExecuteInit(Params args, bool clear) {
            // 初始化
            this.Variables.Clear();
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
                    if (code.Exp1.Type == SirExpressionTypes.Label) {
                        int labIndex = code.Exp1.Content;
                        if (this.Labels[labIndex] != null) {
                            this.Labels[labIndex].IntPtr = i;
                        } else {
                            this.Labels[labIndex] = new ScriptLabel(SirScopeTypes.Private, "", i);
                        }
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
            return GetValue(ExecuteFunc(func));
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
            return GetValue(ExecuteFunc(idx));
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
