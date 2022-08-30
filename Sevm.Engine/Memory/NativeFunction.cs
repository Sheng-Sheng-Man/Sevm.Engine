using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine.Memory {

    /// <summary>
    /// 整型数值
    /// </summary>
    public class NativeFunction : Value {

        /// <summary>
        /// 获取函数
        /// </summary>
        public ScriptEngine.NativeFunction Function { get; private set; }

        /// <summary>
        /// 对象实例化
        /// </summary>
        /// <param name="lib"></param>
        /// <param name="idx"></param>
        public NativeFunction(ScriptEngine.NativeFunction func) {
            this.Function = func;
        }

        /// <summary>
        /// 判断是否为原生函数
        /// </summary>
        /// <returns></returns>
        protected override bool OnCheckNativeFunction() { return true; }

    }
}
