using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine {

    /// <summary>
    /// 原生函数参数对象
    /// </summary>
    public class NativeFunctionArgs {

        /// <summary>
        /// 获取关联的内存管理器
        /// </summary>
        public Sevm.Memory Memory { get; private set; }

        /// <summary>
        /// 参数集合
        /// </summary>
        public Params Params { get; private set; }

        /// <summary>
        /// 对象实例化
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="ps"></param>
        public NativeFunctionArgs(Sevm.Memory memory, Params ps) {
            this.Memory = memory;
            this.Params = ps;
        }

    }
}
