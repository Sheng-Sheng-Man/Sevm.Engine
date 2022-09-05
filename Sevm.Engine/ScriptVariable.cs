using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine {

    /// <summary>
    /// 变量信息
    /// </summary>
    public class ScriptVariable {

        /// <summary>
        /// 获取或设置名称
        /// </summary>
        public Sir.SirScopeTypes ScopeType { get; set; }

        /// <summary>
        /// 获取或设置名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置指针
        /// </summary>
        public Sevm.MemoryPtr MemoryPtr { get; set; }

        /// <summary>
        /// 对象实例化
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="name"></param>
        /// <param name="ptr"></param>
        public ScriptVariable(Sir.SirScopeTypes scope, string name, MemoryPtr ptr) {
            this.ScopeType = scope;
            Name = name;
            MemoryPtr = ptr;
        }

        /// <summary>
        /// 对象实例化
        /// </summary>
        public ScriptVariable() {
            this.ScopeType = Sir.SirScopeTypes.Private;
            Name = "";
            MemoryPtr = new MemoryPtr() {
                Type = MemoryTypes.None,
                Size = 0,
                IntPtr = IntPtr.Zero
            };
        }

    }
}
