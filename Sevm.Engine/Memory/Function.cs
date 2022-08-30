using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine.Memory {

    /// <summary>
    /// 整型数值
    /// </summary>
    public class Function : Value {

        /// <summary>
        /// 获取库索引
        /// </summary>
        public int Library { get; private set; }

        /// <summary>
        /// 获取获取标签
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// 对象实例化
        /// </summary>
        /// <param name="lib"></param>
        /// <param name="idx"></param>
        public Function(int lib, int idx) {
            Library = lib;
            Index = idx;
        }

        /// <summary>
        /// 判断是否为函数
        /// </summary>
        /// <returns></returns>
        protected override bool OnCheckFunction() { return true; }

    }
}
