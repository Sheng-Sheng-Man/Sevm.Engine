using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine {

    /// <summary>
    /// 定义信息
    /// </summary>
    public class Define {

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
        public int IntPtr { get; set; }

        /// <summary>
        /// 对象实例化
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ptr"></param>
        public Define(Sir.SirScopeTypes scope, string name, int ptr) {
            this.ScopeType = scope;
            Name = name;
            IntPtr = ptr;
        }

        /// <summary>
        /// 对象实例化
        /// </summary>
        public Define() {
            this.ScopeType = Sir.SirScopeTypes.Private;
            Name = "";
            IntPtr = 0;
        }

    }
}
