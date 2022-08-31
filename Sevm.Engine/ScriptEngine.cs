﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Sevm.Engine;
using Sevm.Engine.Memory;
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
        public delegate Engine.Memory.Value NativeFunction(Engine.Params args);

        /// <summary>
        /// 原生函数集合
        /// </summary>
        public Engine.NativeFunctions NativeFunctions { get; private set; }

        /// <summary>
        /// 注册函数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fn"></param>
        public void Reg(string name, NativeFunction fn) {
            this.NativeFunctions[name] = fn;
        }

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
        public Memories Memories { get; private set; }

        /// <summary>
        /// 寄存器集合
        /// </summary>
        public ScriptRegisters Registers { get; private set; }

        /// <summary>
        /// 变量集合
        /// </summary>
        public Defines Variables { get; private set; }

        /// <summary>
        /// 函数集合
        /// </summary>
        public Defines Labels { get; private set; }

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
        public Value GetPublicVariable(string name) {
            for (int i = 0; i < this.Variables.Count; i++) {
                var def = this.Variables[i];
                if (def.ScopeType == SirScopeTypes.Public) {
                    if (def.Name == name) return this.Memories[def.IntPtr];
                }
            }
            return Value.None;
        }

        /// <summary>
        /// 设置公共变量值
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetPublicVariable(string name, Value value) {
            for (int i = 0; i < this.Variables.Count; i++) {
                var def = this.Variables[i];
                if (def.ScopeType == SirScopeTypes.Public) {
                    if (def.Name == name) {
                        if (def.IntPtr > 0) {
                            this.Memories[def.IntPtr] = value;
                            return true;
                        } else {
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        // 初始化
        private void init() {
            this.NativeFunctions = new NativeFunctions();
            this.Libraries = new ScriptLibraries();
            this.Variables = new Defines();
            this.Labels = new Defines();
            if (this.Paths == null) this.Paths = new List<string>();
            if (this.Registers == null) this.Registers = new ScriptRegisters();
            if (this.Libraries == null) this.Libraries = new ScriptLibraries();
            if (this.Memories == null) this.Memories = new Memories();
            if (this.RegisterFunctions == null) this.RegisterFunctions = new ScriptFunctions();
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
            this.Memories = engine.Memories;
            this.RegisterFunctions = engine.RegisterFunctions;
            this.Libraries = engine.Libraries;
            this.Registers = engine.Registers;
            this.Paths = engine.Paths;
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
        public Engine.Memory.Value GetValue(SirExpression param) {
            switch (param.Type) {
                case SirExpressionTypes.None: return Engine.Memory.Value.None;
                case SirExpressionTypes.Value: return param.Content;
                case SirExpressionTypes.IntPtr:
                    if (param.Content > 0) {
                        return this.Memories[param.Content];
                    } else {
                        return Engine.Memory.Value.None;
                    }
                case SirExpressionTypes.Register: return this.Registers[param.Content];
                case SirExpressionTypes.Variable: return this.Memories[this.Variables[param.Content].IntPtr];
                default: throw new SirException($"不支持的表达式获取类型'{param.Type.ToString()}'");
            }
        }

        /// <summary>
        /// 向表达式赋值
        /// </summary>
        /// <param name="param"></param>
        /// <param name="value"></param>
        /// <exception cref="Exception"></exception>
        public void SetValue(SirExpression param, Engine.Memory.Value value) {
            switch (param.Type) {
                case SirExpressionTypes.IntPtr:
                    if (param.Content > 0) {
                        this.Memories[param.Content] = value;
                        break;
                    } else {
                        return;
                    }
                case SirExpressionTypes.Register: this.Registers[param.Content] = value.ToInteger(); break;
                case SirExpressionTypes.Variable: this.Memories[this.Variables[param.Content].IntPtr] = value; break;
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
            if (param1.Type == SirExpressionTypes.Value && param2.Type == SirExpressionTypes.Value) return param1.Content == param2.Content ? 1 : 0;
            if (param1.Type == SirExpressionTypes.Value || param1.Type == SirExpressionTypes.Register || param2.Type == SirExpressionTypes.Value || param2.Type == SirExpressionTypes.Register) {
                var value1 = GetValue(param1);
                var value2 = GetValue(param2);
                if (value1.IsNumber() && value2.IsNumber()) return value1.ToDouble() == value2.ToDouble() ? 1 : 0;
                return value1.ToString() == value2.ToString() ? 1 : 0;
            }
            int ptr1 = 0;
            int ptr2 = 0;
            switch (param1.Type) {
                case SirExpressionTypes.IntPtr:
                    ptr1 = param1.Content;
                    break;
                case SirExpressionTypes.Variable:
                    ptr1 = this.Variables[param1.Content].IntPtr;
                    break;
                default: throw new SirException($"不支持的表达式赋值类型'{param1.Type.ToString()}'");
            }
            switch (param2.Type) {
                case SirExpressionTypes.IntPtr:
                    ptr2 = param2.Content;
                    break;
                case SirExpressionTypes.Variable:
                    ptr2 = this.Variables[param2.Content].IntPtr;
                    break;
                default: throw new SirException($"不支持的表达式赋值类型'{param2.Type.ToString()}'");
            }
            if (ptr1 <= 0 && ptr2 <= 0) return 1;
            if (ptr1 == ptr2) return 1;
            var val1 = GetValue(param1);
            var val2 = GetValue(param2);
            if (val1.IsNumber() && val2.IsNumber()) return val1.ToDouble() == val2.ToDouble() ? 1 : 0;
            return val1.ToString() == val2.ToString() ? 1 : 0;
        }

        // 执行函数
        private SirExpression ExecuteFunc(int funcIndex) {
            int line = this.Labels[funcIndex].IntPtr;
            if (line < 0) throw new SirException($"函数'@{funcIndex}'入口缺失");
            while (line < this.Script.Codes.Count) {
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
                                this.Variables[code.Exp1.Content] = new Define(SirScopeTypes.Private, "", GetValue(code.Exp2));
                            } else {
                                this.Variables[code.Exp1.Content].IntPtr = GetValue(code.Exp2);
                            }
                        } else {
                            // 新建虚拟内存块
                            int ptr = this.Memories.Count;
                            this.Memories[ptr] = Engine.Memory.Value.None;
                            // 新建变量或修改变量指针
                            if (this.Variables[code.Exp1.Content] == null) {
                                this.Variables[code.Exp1.Content] = new Define(SirScopeTypes.Private, "", ptr);
                            } else {
                                this.Variables[code.Exp1.Content].IntPtr = ptr;
                            }
                        }
                        line++; break;
                    case SirCodeInstructionTypes.Lea:
                        if (code.Exp2.Type != SirExpressionTypes.Variable) throw new SirException(code.SourceLine, line, $"不支持的表达式类型'{code.Exp2.Type.ToString()}'");
                        SetValue(code.Exp1, this.Variables[code.Exp2.Content].IntPtr);
                        line++; break;
                    case SirCodeInstructionTypes.Int:
                        SetValue(code.Exp1, GetValue(code.Exp2).ToInteger());
                        line++; break;
                    case SirCodeInstructionTypes.Frac:
                        SetValue(code.Exp1, GetValue(code.Exp2).ToDouble() - GetValue(code.Exp2).ToInteger());
                        line++; break;
                    // 三、类型操作指令
                    case SirCodeInstructionTypes.List:
                        SetValue(code.Exp1, new Engine.Memory.List(this.Memories));
                        line++; break;
                    case SirCodeInstructionTypes.Ptrl: // 设置列表内容指针
                        Engine.Memory.List ptrList = (Engine.Memory.List)this.Memories[this.Variables[code.Exp1.Content].IntPtr];
                        // 动态添加列表项
                        int ptrListIndex = GetValue(code.Exp2);
                        int ptrListValue = GetValue(code.Exp3);
                        // 判断赋值的指针是否大于0
                        if (ptrListValue > 0) {
                            // 设置值
                            if (ptrList.Values.Count <= ptrListIndex) {
                                for (int i = ptrList.Values.Count; i <= ptrListIndex; i++) {
                                    ptrList.Values.Add(0);
                                }
                            }
                            ptrList.Values[ptrListIndex] = ptrListValue;
                        } else {
                            // 清理列表
                            for (int i = ptrList.Values.Count - 1; i >= ptrListIndex; i--) {
                                ptrList.Values.RemoveAt(i);
                            }
                        }
                        line++; break;
                    case SirCodeInstructionTypes.Leal: // 获取列表内容指针
                        ptrList = (Engine.Memory.List)this.Memories[this.Variables[code.Exp2.Content].IntPtr];
                        SetValue(code.Exp1, ptrList.Values[GetValue(code.Exp3)]);
                        line++; break;
                    case SirCodeInstructionTypes.Idx: // 获取内容匹配索引
                        ptrList = (Engine.Memory.List)this.Memories[this.Variables[code.Exp2.Content].IntPtr];
                        // 动态添加列表项
                        int idx = -1;
                        string str = GetValue(code.Exp3);
                        // 遍历列表
                        for (int i = 0; i < ptrList.Count; i++) {
                            string lsStr = this.Memories[ptrList.Values[i]].ToString();
                            if (str == lsStr) {
                                idx = i;
                                break;
                            }
                        }
                        // 赋值内容
                        SetValue(code.Exp1, idx);
                        line++; break;
                    case SirCodeInstructionTypes.Join:
                        Engine.Memory.List list = (Engine.Memory.List)GetValue(code.Exp2);
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < list.Values.Count; i++) {
                            sb.Append(this.Memories[list.Values[i]].ToString());
                        }
                        SetValue(code.Exp1, sb.ToString());
                        line++; break;
                    case SirCodeInstructionTypes.Cnt:
                        SetValue(code.Exp1, GetValue(code.Exp2).GetSize());
                        line++; break;
                    case SirCodeInstructionTypes.Obj:
                        Engine.Memory.Object obj = new Engine.Memory.Object(this.Memories);
                        SetValue(code.Exp1, obj);
                        obj.Keys = this.Memories.Count;
                        this.Memories[obj.Keys] = new Engine.Memory.List(this.Memories);
                        obj.Values = this.Memories.Count;
                        this.Memories[obj.Values] = new Engine.Memory.List(this.Memories);
                        line++; break;
                    case SirCodeInstructionTypes.Ptrk: // 设置对象键列表指针
                        obj = (Engine.Memory.Object)GetValue(code.Exp1);
                        obj.Keys = GetValue(code.Exp2);
                        line++; break;
                    case SirCodeInstructionTypes.Ptrv: // 设置对象值列表指针
                        obj = (Engine.Memory.Object)GetValue(code.Exp1);
                        obj.Values = GetValue(code.Exp2);
                        line++; break;
                    case SirCodeInstructionTypes.Leak: // 获取对象键列表指针
                        obj = (Engine.Memory.Object)GetValue(code.Exp2);
                        SetValue(code.Exp1, obj.Keys);
                        line++; break;
                    case SirCodeInstructionTypes.Leav: // 获取对象值列表指针
                        obj = (Engine.Memory.Object)GetValue(code.Exp2);
                        SetValue(code.Exp1, obj.Values);
                        line++; break;
                    // 四、运算操作指令
                    case SirCodeInstructionTypes.Add:
                        SetValue(code.Exp1, GetValue(code.Exp1).ToDouble() + GetValue(code.Exp2).ToDouble());
                        line++; break;
                    case SirCodeInstructionTypes.Sub:
                        SetValue(code.Exp1, GetValue(code.Exp1).ToDouble() - GetValue(code.Exp2).ToDouble());
                        line++; break;
                    case SirCodeInstructionTypes.Mul:
                        SetValue(code.Exp1, GetValue(code.Exp1).ToDouble() * GetValue(code.Exp2).ToDouble());
                        line++; break;
                    case SirCodeInstructionTypes.Div:
                        SetValue(code.Exp1, GetValue(code.Exp1).ToDouble() / GetValue(code.Exp2).ToDouble());
                        line++; break;
                    // 五、逻辑操作指令
                    case SirCodeInstructionTypes.Not:
                        SetValue(code.Exp1, GetValue(code.Exp1).ToInteger() > 0 ? 0 : 1);
                        line++; break;
                    case SirCodeInstructionTypes.And:
                        SetValue(code.Exp1, GetValue(code.Exp1).ToInteger() & GetValue(code.Exp2).ToInteger());
                        line++; break;
                    case SirCodeInstructionTypes.Or:
                        SetValue(code.Exp1, GetValue(code.Exp1).ToInteger() | GetValue(code.Exp2).ToInteger());
                        line++; break;
                    case SirCodeInstructionTypes.Xor:
                        SetValue(code.Exp1, GetValue(code.Exp1).ToInteger() ^ GetValue(code.Exp2).ToInteger());
                        line++; break;
                    // 六、比较指令
                    case SirCodeInstructionTypes.Equal:
                        SetValue(code.Exp1, Equal(code.Exp2, code.Exp3));
                        line++; break;
                    case SirCodeInstructionTypes.Large:
                        SetValue(code.Exp1, GetValue(code.Exp2).ToDouble() > GetValue(code.Exp3).ToDouble() ? 1 : 0);
                        line++; break;
                    case SirCodeInstructionTypes.Small:
                        SetValue(code.Exp1, GetValue(code.Exp2).ToDouble() < GetValue(code.Exp3).ToDouble() ? 1 : 0);
                        line++; break;
                    // 七、区域操作指令
                    case SirCodeInstructionTypes.Jmp:
                        if (code.Exp1.Type != SirExpressionTypes.Label) throw new SirException(code.SourceLine, line, $"不支持的表达式赋值类型'{code.Exp1.Type.ToString()}'");
                        line = this.Labels[code.Exp1.Content].IntPtr;
                        break;
                    case SirCodeInstructionTypes.Jmpf:
                        if (code.Exp2.Type != SirExpressionTypes.Label) throw new SirException(code.SourceLine, line, $"不支持的表达式赋值类型'{code.Exp1.Type.ToString()}'");
                        if (GetValue(code.Exp1).ToDouble() > 0) {
                            line = this.Labels[code.Exp2.Content].IntPtr;
                            break;
                        }
                        line++; break;
                    case SirCodeInstructionTypes.Call:
                        if (code.Exp2.Type == SirExpressionTypes.Label) {
                            SetValue(code.Exp1, GetValue(ExecuteFunc(code.Exp2.Content)));
                        } else {
                            string name = GetValue(code.Exp2);
                            // 判断函数是否已经注册
                            if (!this.RegisterFunctions.ContainsKey(name)) throw new SirException(code.SourceLine, line, $"未找到外部函数'{name}'");
                            // 读取注册函数
                            var func = this.Memories[this.RegisterFunctions[name]];
                            if (func.IsNativeFunction()) {
                                // 生成参数
                                Engine.Memory.List arg = (Engine.Memory.List)this.Memories[this.Registers[0]];
                                Params args = new Params();
                                for (int i = 0; i < arg.Values.Count; i++) {
                                    args.Add(this.Memories[arg.Values[i]]);
                                }
                                // 执行原生函数并返回结果
                                SetValue(code.Exp1, ((Engine.Memory.NativeFunction)func).Function(args));
                            } else if (func.IsFunction()) {
                                // 读取自定义函数
                                var fn = (Engine.Memory.Function)func;
                                var lib = this.Libraries[fn.Library];
                                string funName = lib.Script.Funcs[fn.Index].Name;
                                // 执行并返回内容
                                using (var engine = new ScriptEngine(lib.Script, this.Root)) {
                                    SetValue(code.Exp1, engine.Execute(funName, null, false));
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
        }

        // 执行初始化
        private void ExecuteInit(Params args, bool clear) {
            // 初始化
            this.NativeFunctions.Clear();
            this.Variables.Clear();
            this.Labels.Clear();
            // 初始化存储空间
            if (clear) {
                this.Registers.Clear();
                this.Memories.Clear();
                this.Memories.Add(Value.None);
                this.Libraries.Clear();
                this.RegisterFunctions.Clear();
            }
            // 加载函数集
            for (int i = 0; i < this.Script.Imports.Count; i++) {
                var import = this.Script.Imports[i];
                switch (import.ImportType) {
                    case SirImportTypes.Use:
                        //this.OnRegFunction(this, new ScrpitEventArgs() { Func = import.Content });
                        // 加载原生插件
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
                                                    int funPtr = this.Memories.Count;
                                                    this.Memories.Add(new Engine.Memory.NativeFunction(fn));
                                                    this.RegisterFunctions[name] = funPtr;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        }
                        break;
                    case SirImportTypes.Lib:
                        // 从所有路径中查找sbc文件
                        for (int j = 0; j < this.Paths.Count; j++) {
                            string path = this.Paths[j];
                            string file = $"{path}{import.Content}.sbc";
                            if (System.IO.File.Exists(file)) {
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
                            int funPtr = this.Memories.Count;
                            this.Memories.Add(new Function(i, j));
                            this.RegisterFunctions[fun.Name] = funPtr;
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
                        int idxData = this.Memories.Count;
                        this.Memories.Add(Engine.Memory.Value.None);
                        this.Variables[data.Index] = new Define(SirScopeTypes.Private, "", idxData);
                        //this.Memories[data.IntPtr] = Engine.Memory.Value.None;
                        break;
                    case SirDataTypes.Number:
                        // 添加数值
                        idxData = this.Memories.Count;
                        this.Memories.Add(data.GetNumber());
                        this.Variables[data.Index] = new Define(SirScopeTypes.Private, "", idxData);
                        //this.Memories[data.IntPtr] = data.GetNumber();
                        break;
                    case SirDataTypes.String:
                        // 添加数值
                        idxData = this.Memories.Count;
                        this.Memories.Add(data.GetString());
                        this.Variables[data.Index] = new Define(SirScopeTypes.Private, "", idxData);
                        //this.Memories[data.IntPtr] = data.GetString();
                        break;
                    default: throw new SirException($"不支持的数据类型'{data.DataType.ToString()}'");
                }
            }
            // 填充变量
            for (int i = 0; i < this.Script.Defines.Count; i++) {
                var def = this.Script.Defines[i];
                this.Variables[def.Index] = new Define(def.Scope, def.Name, 0);
            }
            // 填充标签
            for (int i = 0; i < this.Script.Funcs.Count; i++) {
                var fn = this.Script.Funcs[i];
                this.Labels[fn.Index] = new Define(fn.Scope, fn.Name, 0);
            }
            for (int i = 0; i < this.Script.Codes.Count; i++) {
                var code = this.Script.Codes[i];
                if (code.Instruction == SirCodeInstructionTypes.Label) {
                    if (code.Exp1.Type == SirExpressionTypes.Label) {
                        int labIndex = code.Exp1.Content;
                        if (this.Labels[labIndex] != null) {
                            this.Labels[labIndex].IntPtr = i;
                        } else {
                            this.Labels[labIndex] = new Define(SirScopeTypes.Private, "", i);
                        }
                    }
                }
            }
            if (args != null) {
                // 将参数填充到
                int ptr = this.Memories.Count;
                Engine.Memory.List list = new Engine.Memory.List(this.Memories);
                this.Memories[ptr] = list;
                this.Registers[0] = ptr;
                for (int i = 0; i < args.Count; i++) {
                    ptr = this.Memories.Count;
                    this.Memories[ptr] = args[i];
                    list.Values.Add(ptr);
                }
            }
        }

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <param name="func"></param>
        /// <param name="args"></param>
        /// <param name="clearMemories"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Engine.Memory.Value Execute(int func, Params args, bool clear) {
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
        /// <param name="clearMemories"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Engine.Memory.Value Execute(string func, Params args, bool clear) {
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
        public Engine.Memory.Value Execute() {
            return Execute("main", new Params(), true);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() {
            this.NativeFunctions.Clear();
            this.Libraries.Clear();
            this.Registers.Clear();
            this.Memories.Clear();
            this.Variables.Clear();
            this.Labels.Clear();
        }
    }
}
