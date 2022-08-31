using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine {

    /// <summary>
    /// 脚本库文件
    /// </summary>
    public class ScriptLibrary {

        /// <summary>
        /// 文件路径
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 获取脚本对象
        /// </summary>
        public Sevm.Sir.SirScript Script { get; private set; }

        /// <summary>
        /// 实例化对象
        /// </summary>
        /// <param name="path"></param>
        /// <param name="script"></param>
        public ScriptLibrary(string path, Sevm.Sir.SirScript script) {
            this.Path = path;
            this.Name = System.IO.Path.GetFileNameWithoutExtension(path);
            this.Script = script;
        }

    }
}
