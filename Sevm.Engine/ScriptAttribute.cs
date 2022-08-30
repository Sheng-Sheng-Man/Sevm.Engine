using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine {

    /// <summary>
    /// 脚本特性
    /// </summary>
    public class ScriptAttribute : System.Attribute {

        /// <summary>
        /// 获取或设置名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 对象实例化
        /// </summary>
        /// <param name="name"></param>
        public ScriptAttribute(string name) { this.Name = name; }

    }
}
