using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine.Memory {

    /// <summary>
    /// 字符串
    /// </summary>
    public class String : Value {

        /// <summary>
        /// 获取值
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// 对象实例化
        /// </summary>
        /// <param name="value"></param>
        public String(string value) {
            Value = value;
        }

        /// <summary>
        /// 获取数据尺寸
        /// </summary>
        /// <returns></returns>
        protected override int OnGetSize() {
            return this.Value.Length;
        }

        /// <summary>
        /// 判断是否为数字
        /// </summary>
        /// <returns></returns>
        protected override bool OnCheckDouble() {
            double dbl = 0;
            return double.TryParse(this.Value, out dbl);
        }

        /// <summary>
        /// 检查是否为空
        /// </summary>
        /// <returns></returns>
        protected override bool OnCheckEmpty() {
            if (this.Value == null) return true;
            if (this.Value == "") return true;
            return false;
        }

        /// <summary>
        /// 转化为布尔型
        /// </summary>
        /// <returns></returns>
        protected override bool OnParseBealoon() {
            return this.Value.ToLower() == "true" || this.Value.ToLower() == "yes" || (this.IsNumber() && this.ToDouble() > 0);
        }

        /// <summary>
        /// 转化为字节型
        /// </summary>
        /// <returns></returns>
        protected override byte OnParseByte() {
            return (byte)this.ToDouble();
        }

        /// <summary>
        /// 转化为整型
        /// </summary>
        /// <returns></returns>
        protected override int OnParseInteger() {
            return (int)this.ToDouble();
        }

        /// <summary>
        /// 转化为长整型
        /// </summary>
        /// <returns></returns>
        protected override long OnParseLong() {
            return (long)this.ToDouble();
        }

        /// <summary>
        /// 转化为单精度
        /// </summary>
        /// <returns></returns>
        protected override float OnParseFloat() {
            return (float)this.ToDouble();
        }

        /// <summary>
        /// 转化为双精度
        /// </summary>
        /// <returns></returns>
        protected override double OnParseDouble() {
            //return this.Value.ToDouble();
            double dbl = 0;
            if (double.TryParse(this.Value, out dbl)) return dbl;
            return 0;
        }

        /// <summary>
        /// 转化为字符串
        /// </summary>
        /// <returns></returns>
        protected override string OnParseString() {
            return this.Value;
        }

    }
}
