using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine {

    /// <summary>
    /// 原生函数集合
    /// </summary>
    public class ScriptLibraries : List<ScriptLibrary> {

        /// <summary>
        /// 检测路径是否已经加载
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool ContainsPath(string path) {
            for (int i = 0; i < base.Count; i++) {
                if (this[i].Path == path) return true;
            }
            return false;
        }

        /// <summary>
        /// 添加一个库
        /// </summary>
        /// <param name="path"></param>
        /// <param name="script"></param>
        public void Add(string path, Sir.SirScript script) {
            base.Add(new ScriptLibrary(path, script));
        }

    }

}
