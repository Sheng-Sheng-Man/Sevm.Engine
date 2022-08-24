using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine.Memory {

    /// <summary>
    /// 空值
    /// </summary>
    public class None : Value {

        /// <summary>
        /// 检测是否为空
        /// </summary>
        /// <returns></returns>
        protected override bool OnCheckEmpty() {
            return true;
        }

        /// <summary>
        /// 获取数据尺寸
        /// </summary>
        /// <returns></returns>
        protected override int OnGetSize() {
            return 0;
        }

    }
}
