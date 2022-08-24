using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Sevm.Engine;
using Sevm.Sir;

namespace Sevm {

    /// <summary>
    /// 脚本引擎
    /// </summary>
    public class ScriptEngine : IDisposable {

        /// <summary>
        /// 脚本事件参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ScriptEventHandle(object sender, ScrpitEventArgs e);

        /// <summary>
        /// 执行事件
        /// </summary>
        public event ScriptEventHandle OnExecuting;

        /// <summary>
        /// 函数注册事件
        /// </summary>
        public event ScriptEventHandle OnRegFunction;

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
        public List<string> Pathes { get; private set; }

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
        public Storages Storages { get; private set; }

        /// <summary>
        /// 变量集合
        /// </summary>
        public Defines Variables { get; private set; }

        /// <summary>
        /// 函数集合
        /// </summary>
        public Defines Labels { get; private set; }

        // 初始化
        private void init() {
            this.OnExecuting += (object sender, ScrpitEventArgs e) => { };
            this.OnRegFunction += (object sender, ScrpitEventArgs e) => { };
            this.Pathes = new List<string>();
            this.NativeFunctions = new NativeFunctions();
            this.Libraries = new ScriptLibraries();
            this.Storages = new Storages();
            this.Memories = new Memories();
            this.Variables = new Defines();
            this.Labels = new Defines();
        }

        /// <summary>
        /// 实例化一个脚本引擎
        /// </summary>
        public ScriptEngine() {
            this.Script = new SirScript();
            this.init();
        }

        /// <summary>
        /// 实例化一个脚本引擎
        /// </summary>
        public ScriptEngine(SirScript script) {
            this.Script = script;
            this.init();
        }

        // 获取标签定义行
        private int GetLabelIndex(string Name) {
            for (int i = 0; i < Labels.Count; i++) {
                if (Labels[i] != null) {
                    if (Labels[i].Name == Name) {
                        return i;
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
                case SirExpressionTypes.Storage: return this.Storages[param.Content];
                case SirExpressionTypes.Variable: return this.Memories[this.Variables[param.Content].IntPtr];
                default: throw new Exception($"不支持的表达式获取类型'{param.Type.ToString()}'");
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
                case SirExpressionTypes.Storage: this.Storages[param.Content] = value.ToInteger(); break;
                case SirExpressionTypes.Variable: this.Memories[this.Variables[param.Content].IntPtr] = value; break;
                default: throw new Exception($"不支持的表达式赋值类型'{param.Type.ToString()}'");
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
            if (param1.Type == SirExpressionTypes.Value || param1.Type == SirExpressionTypes.Storage || param2.Type == SirExpressionTypes.Value || param2.Type == SirExpressionTypes.Storage) {
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
                default: throw new Exception($"不支持的表达式赋值类型'{param1.Type.ToString()}'");
            }
            switch (param2.Type) {
                case SirExpressionTypes.IntPtr:
                    ptr2 = param2.Content;
                    break;
                case SirExpressionTypes.Variable:
                    ptr2 = this.Variables[param2.Content].IntPtr;
                    break;
                default: throw new Exception($"不支持的表达式赋值类型'{param2.Type.ToString()}'");
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
            if (line < 0) throw new Exception($"函数'@{funcIndex}'入口缺失");
            while (line < this.Script.Codes.Count) {
                var code = this.Script.Codes[line];
                switch (code.Instruction) {
                    // 空指令
                    case SirCodeInstructionTypes.None:
                    case SirCodeInstructionTypes.Label:
                        line++; break;
                    // 二、数据指令
                    case SirCodeInstructionTypes.Mov:
                        SetValue(code.Target, GetValue(code.Source));
                        line++; break;
                    case SirCodeInstructionTypes.New:
                        if (code.Target.Type != SirExpressionTypes.Variable) throw new Exception($"不支持的表达式赋值类型'{code.Target.Type.ToString()}'");
                        if (code.Source.Type != SirExpressionTypes.None) {
                            // 新建变量并指定地址
                            this.Variables[code.Target.Content] = new Define("", GetValue(code.Source));
                        } else {
                            // 新建虚拟内存块
                            int ptr = this.Memories.Count;
                            this.Memories[ptr] = Engine.Memory.Value.None;
                            // 新建变量
                            this.Variables[code.Target.Content] = new Define("", ptr);
                        }
                        line++; break;
                    case SirCodeInstructionTypes.Ptr:
                        if (code.Target.Type != SirExpressionTypes.Variable) throw new Exception($"不支持的表达式赋值类型'{code.Target.Type.ToString()}'");
                        switch (this.Storages[0]) {
                            case 0: // 处理变量指针
                                this.Variables[code.Target.Content].IntPtr = GetValue(code.Source);
                                break;
                            case 0x01: // 处理列表指针
                                Engine.Memory.List ptrList = (Engine.Memory.List)this.Memories[this.Variables[code.Target.Content].IntPtr];
                                // 动态添加列表项
                                int ptrListIndex = this.Storages[1];
                                if (ptrList.Values.Count <= ptrListIndex) {
                                    for (int i = ptrList.Values.Count; i <= ptrListIndex; i++) {
                                        ptrList.Values.Add(0);
                                    }
                                }
                                ptrList.Values[ptrListIndex] = GetValue(code.Source);
                                break;
                            case 0x11: // 处理对象键集合指针
                                Engine.Memory.Object ptrObj = (Engine.Memory.Object)this.Memories[this.Variables[code.Target.Content].IntPtr];
                                ptrObj.Keys = GetValue(code.Source).ToInteger();
                                break;
                            case 0x12: // 处理对象值集合指针
                                ptrObj = (Engine.Memory.Object)this.Memories[this.Variables[code.Target.Content].IntPtr];
                                ptrObj.Values = GetValue(code.Source).ToInteger();
                                break;
                            default: throw new Exception($"不支持的处理类型'{this.Storages[0]}'");
                        }
                        line++; break;
                    case SirCodeInstructionTypes.Lea:
                        if (code.Source.Type != SirExpressionTypes.Variable) throw new Exception($"不支持的表达式类型'{code.Source.Type.ToString()}'");
                        switch (this.Storages[0]) {
                            case 0: // 处理变量指针
                                SetValue(code.Target, this.Variables[code.Source.Content].IntPtr);
                                break;
                            case 0x01: // 处理列表指针
                                Engine.Memory.List ptrList = (Engine.Memory.List)this.Memories[this.Variables[code.Source.Content].IntPtr];
                                SetValue(code.Target, ptrList.Values[this.Storages[1]]);
                                break;
                            case 0x11: // 处理对象键集合指针
                                Engine.Memory.Object ptrObj = (Engine.Memory.Object)this.Memories[this.Variables[code.Source.Content].IntPtr];
                                SetValue(code.Target, ptrObj.Keys);
                                break;
                            case 0x12: // 处理对象值集合指针
                                ptrObj = (Engine.Memory.Object)this.Memories[this.Variables[code.Source.Content].IntPtr];
                                SetValue(code.Target, ptrObj.Values);
                                break;
                            default: throw new Exception($"不支持的处理类型'{this.Storages[0]}'");
                        }
                        line++; break;
                    case SirCodeInstructionTypes.Int:
                        SetValue(code.Target, GetValue(code.Source).ToInteger());
                        line++; break;
                    case SirCodeInstructionTypes.Frac:
                        SetValue(code.Target, GetValue(code.Source).ToDouble() - GetValue(code.Source).ToInteger());
                        line++; break;
                    // 三、类型操作指令
                    case SirCodeInstructionTypes.List:
                        SetValue(code.Target, new Engine.Memory.List(this.Memories));
                        line++; break;
                    case SirCodeInstructionTypes.Join:
                        Engine.Memory.List list = (Engine.Memory.List)GetValue(code.Source);
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < list.Values.Count; i++) {
                            sb.Append(this.Memories[list.Values[i]].ToString());
                        }
                        SetValue(code.Target, sb.ToString());
                        line++; break;
                    case SirCodeInstructionTypes.Cnt:
                        SetValue(code.Target, GetValue(code.Source).GetSize());
                        line++; break;
                    case SirCodeInstructionTypes.Obj:
                        Engine.Memory.Object obj = new Engine.Memory.Object(this.Memories);
                        SetValue(code.Target, obj);
                        obj.Keys = this.Memories.Count;
                        this.Memories[obj.Keys] = new Engine.Memory.List(this.Memories);
                        obj.Values = this.Memories.Count;
                        this.Memories[obj.Values] = new Engine.Memory.List(this.Memories);
                        line++; break;
                    // 四、运算操作指令
                    case SirCodeInstructionTypes.Add:
                        SetValue(code.Target, GetValue(code.Target).ToDouble() + GetValue(code.Source).ToDouble());
                        line++; break;
                    case SirCodeInstructionTypes.Sub:
                        SetValue(code.Target, GetValue(code.Target).ToDouble() - GetValue(code.Source).ToDouble());
                        line++; break;
                    case SirCodeInstructionTypes.Mul:
                        SetValue(code.Target, GetValue(code.Target).ToDouble() * GetValue(code.Source).ToDouble());
                        line++; break;
                    case SirCodeInstructionTypes.Div:
                        SetValue(code.Target, GetValue(code.Target).ToDouble() / GetValue(code.Source).ToDouble());
                        line++; break;
                    // 五、逻辑操作指令
                    case SirCodeInstructionTypes.Not:
                        SetValue(code.Target, GetValue(code.Target).ToInteger() > 0 ? 0 : 1);
                        line++; break;
                    case SirCodeInstructionTypes.And:
                        SetValue(code.Target, GetValue(code.Target).ToInteger() & GetValue(code.Source).ToInteger());
                        line++; break;
                    case SirCodeInstructionTypes.Or:
                        SetValue(code.Target, GetValue(code.Target).ToInteger() | GetValue(code.Source).ToInteger());
                        line++; break;
                    case SirCodeInstructionTypes.Xor:
                        SetValue(code.Target, GetValue(code.Target).ToInteger() ^ GetValue(code.Source).ToInteger());
                        line++; break;
                    // 六、比较指令
                    case SirCodeInstructionTypes.Equal:
                        this.Storages[0] = Equal(code.Target, code.Source);
                        line++; break;
                    case SirCodeInstructionTypes.Large:
                        this.Storages[0] = GetValue(code.Target).ToDouble() > GetValue(code.Source).ToDouble() ? 1 : 0;
                        line++; break;
                    case SirCodeInstructionTypes.Small:
                        this.Storages[0] = GetValue(code.Target).ToDouble() < GetValue(code.Source).ToDouble() ? 1 : 0;
                        line++; break;
                    // 七、区域操作指令
                    case SirCodeInstructionTypes.Jmp:
                        if (code.Target.Type != SirExpressionTypes.Label) throw new Exception($"不支持的表达式赋值类型'{code.Target.Type.ToString()}'");
                        line = this.Labels[code.Target.Content].IntPtr;
                        break;
                    case SirCodeInstructionTypes.Jmpf:
                        if (code.Target.Type != SirExpressionTypes.Label) throw new Exception($"不支持的表达式赋值类型'{code.Target.Type.ToString()}'");
                        if (this.Storages[0] > 0) {
                            line = this.Labels[code.Target.Content].IntPtr;
                        } else {
                            if (code.Source.Type == SirExpressionTypes.None) break;
                            if (code.Source.Type != SirExpressionTypes.Label) throw new Exception($"不支持的表达式赋值类型'{code.Target.Type.ToString()}'");
                            line = this.Labels[code.Source.Content].IntPtr;
                        }
                        break;
                    case SirCodeInstructionTypes.Call:
                        if (code.Source.Type == SirExpressionTypes.Label) {
                            SetValue(code.Target, GetValue(ExecuteFunc(code.Source.Content)));
                        } else {
                            string name = GetValue(code.Source);
                            // 生成参数
                            Engine.Memory.List arg = (Engine.Memory.List)this.Memories[this.Storages[0]];
                            Params args = new Params();
                            for (int i = 0; i < arg.Values.Count; i++) {
                                args.Add(this.Memories[arg.Values[i]]);
                            }
                            // 优先从注册的函数中执行
                            if (this.NativeFunctions.ContainsKey(name)) {
                                // 执行并返回内容
                                SetValue(code.Target, this.NativeFunctions[name](args));
                            } else {
                                // 从第三方程序中查找函数并执行
                                bool found = false;
                                for (int i = 0; i < this.Libraries.Count; i++) {
                                    var lib = this.Libraries[i];
                                    for (int j = 0; j < lib.Funcs.Count; j++) {
                                        string funName = lib.Funcs[j].Name;
                                        if (funName == name) {
                                            // 执行并返回内容
                                            using (var engine = new ScriptEngine(lib)) {
                                                engine.OnExecuting += (object sender, ScrpitEventArgs e) => { this.OnExecuting(sender, e); };
                                                engine.OnRegFunction += (object sender, ScrpitEventArgs e) => { this.OnRegFunction(sender, e); };
                                                SetValue(code.Target, engine.Execute(funName, args));
                                                found = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (found) break;
                                }
                                if (!found) throw new Exception($"未找到外部函数'{name}'");
                            }
                        }
                        line++; break;
                    case SirCodeInstructionTypes.Ret: return code.Target;
                    default: throw new Exception($"不支持的指令类型'{code.Instruction.ToString()}'");
                }
            }
            return SirExpression.Value(0);
        }

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public Engine.Memory.Value Execute(string func, Params args) {
            // 初始化
            this.NativeFunctions.Clear();
            this.Libraries.Clear();
            this.Storages.Clear();
            this.Memories.Clear();
            this.Variables.Clear();
            this.Labels.Clear();
            // 加载函数集
            for (int i = 0; i < this.Script.Imports.Count; i++) {
                var import = this.Script.Imports[i];
                switch (import.ImportType) {
                    case SirImportTypes.Use:
                        this.OnRegFunction(this, new ScrpitEventArgs() { Func = import.Content });
                        break;
                    case SirImportTypes.Lib:
                        for (int j = 0; j < this.Pathes.Count; j++) {
                            string path = this.Pathes[j];
                            string file = $"{path}{import.Content}.sbc";
                            if (System.IO.File.Exists(file)) {
                                using (var f = System.IO.File.Open(file, FileMode.Open)) {
                                    List<byte> ls = new List<byte>();
                                    byte[] buffer = new byte[1024];
                                    int len;
                                    do {
                                        len = f.Read(buffer, 0, buffer.Length);
                                        for (int k = 0; k < len; k++) {
                                            ls.Add(buffer[k]);
                                        }
                                    } while (len > 0);
                                    this.Libraries.Add(Parser.GetScript(ls.ToArray()));
                                    f.Close();
                                }
                                break;
                            }
                        }
                        break;
                    default: throw new Exception($"不支持的加载方式'{import.ImportType.ToString()}'");
                }
            }
            // 填充数据
            for (int i = 0; i < this.Script.Datas.Count; i++) {
                var data = this.Script.Datas[i];
                switch (data.DataType) {
                    case SirDataTypes.None:
                        this.Memories[data.IntPtr] = Engine.Memory.Value.None;
                        break;
                    case SirDataTypes.Number:
                        this.Memories[data.IntPtr] = data.GetNumber();
                        break;
                    case SirDataTypes.String:
                        this.Memories[data.IntPtr] = data.GetString();
                        break;
                    default: throw new Exception($"不支持的数据类型'{data.DataType.ToString()}'");
                }
            }
            // 填充变量
            for (int i = 0; i < this.Script.Defines.Count; i++) {
                var def = this.Script.Defines[i];
                this.Variables[def.Index] = new Define(def.Name, def.IntPtr);
            }
            // 填充标签
            for (int i = 0; i < this.Script.Funcs.Count; i++) {
                var fn = this.Script.Funcs[i];
                this.Labels[fn.Index] = new Define(fn.Name, 0);
            }
            for (int i = 0; i < this.Script.Codes.Count; i++) {
                var code = this.Script.Codes[i];
                if (code.Instruction == SirCodeInstructionTypes.Label) {
                    if (code.Target.Type == SirExpressionTypes.Label) {
                        int labIndex = code.Target.Content;
                        if (this.Labels[labIndex] != null) {
                            this.Labels[labIndex].IntPtr = i;
                        } else {
                            this.Labels[labIndex] = new Define("", i);
                        }
                    }
                }
            }
            // 触发执行事件
            this.OnExecuting(this, new ScrpitEventArgs() { Func = func });
            // 将参数填充到
            int ptr = this.Memories.Count;
            Engine.Memory.List list = new Engine.Memory.List(this.Memories);
            this.Memories[ptr] = list;
            this.Storages[0] = ptr;
            for (int i = 0; i < args.Count; i++) {
                ptr = this.Memories.Count;
                this.Memories[ptr] = args[i];
                list.Values.Add(ptr);
            }
            // 执行函数
            int idx = GetLabelIndex(func);
            if (idx < 0) throw new Exception($"函数'{func}'入口缺失");
            return GetValue(ExecuteFunc(idx));
        }

        /// <summary>
        /// 执行程序
        /// </summary>
        /// <returns></returns>
        public Engine.Memory.Value Execute() {
            return Execute("main", new Params());
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() {
            this.NativeFunctions.Clear();
            this.Libraries.Clear();
            this.Storages.Clear();
            this.Memories.Clear();
            this.Variables.Clear();
            this.Labels.Clear();
        }
    }
}
