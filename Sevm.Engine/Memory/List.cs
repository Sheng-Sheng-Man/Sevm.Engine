using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine.Memory {

    /// <summary>
    /// 列表
    /// </summary>
    public class List : Value {

        /// <summary>
        /// 获取内存储器
        /// </summary>
        public Memories Memories { get; private set; }

        /// <summary>
        /// 获取值集合
        /// </summary>
        public List<int> Values { get; private set; }

        /// <summary>
        /// 对象实例化
        /// </summary>
        public List(Memories mem) {
            this.Memories = mem;
            this.Values = new List<int>();
        }

        /// <summary>
        /// 检测是否为列表
        /// </summary>
        /// <returns></returns>
        protected override bool OnCheckList() { return true; }

        /// <summary>
        /// 获取或设置值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Value this[int index] {
            get { return this.Memories[this.Values[index]]; }
            set { this.Memories[this.Values[index]] = value; }
        }

        /// <summary>
        /// 获取元素数量
        /// </summary>
        public int Count { get { return this.Values.Count; } }

        /// <summary>
        /// 获取数据尺寸
        /// </summary>
        /// <returns></returns>
        protected override int OnGetSize() {
            return this.Values.Count;
        }

    }
}
