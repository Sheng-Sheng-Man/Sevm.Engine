using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine.Memory {

    /// <summary>
    /// 对象
    /// </summary>
    public class NativeObject<T> : Value {

        /// <summary>
        /// 获取关联对象
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// 对象实例化
        /// </summary>
        /// <param name="value"></param>
        public NativeObject(T value) {
            Value = value;
        }

        /// <summary>
        /// 检测是否为列表
        /// </summary>
        /// <returns></returns>
        protected override bool OnCheckNativeObject() { return true; }

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <returns></returns>
        protected override object OnParseObject() {
            return this.Value;
        }

        /// <summary>
        /// 检查是否为空
        /// </summary>
        /// <returns></returns>
        protected override bool OnCheckEmpty() {
            return this.Value == null;
        }

    }
}
