using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine.Memory {

    /// <summary>
    /// 对象
    /// </summary>
    public class Object : Value {

        /// <summary>
        /// 获取内存储器
        /// </summary>
        public Memories Memories { get; private set; }

        /// <summary>
        /// 获取键集合
        /// </summary>
        public int Keys { get; set; }

        /// <summary>
        /// 获取值集合
        /// </summary>
        public int Values { get; set; }

        /// <summary>
        /// 键列表
        /// </summary>
        public List KeyList { get { return (List)this.Memories[Keys]; } }

        /// <summary>
        /// 值列表
        /// </summary>
        public List ValueList { get { return (List)this.Memories[Values]; } }

        /// <summary>
        /// 获取或设置值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Value this[int index] {
            get { return this.ValueList[index]; }
            set { this.ValueList[index] = value; }
        }

        /// <summary>
        /// 获取或设置值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Value this[string key] {
            get {
                for (int i = 0; i < this.KeyList.Count; i++) {
                    if (this.KeyList[i] == key) return this.ValueList[i];
                }
                throw new Exception($"未找到键'{key}'");
            }
            set {
                for (int i = 0; i < this.KeyList.Count; i++) {
                    if (this.KeyList[i] == key) this.ValueList[i] = value;
                }
                throw new Exception($"未找到键'{key}'");
            }
        }

        /// <summary>
        /// 判断键是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key) {
            for (int i = 0; i < this.KeyList.Count; i++) {
                if (this.KeyList[i] == key) return true;
            }
            return false;
        }

        /// <summary>
        /// 对象实例化
        /// </summary>
        public Object(Memories mem) {
            this.Memories = mem;
            this.Keys = 0;
            this.Values = 0;
        }

        /// <summary>
        /// 检测是否为对象
        /// </summary>
        /// <returns></returns>
        protected override bool OnCheckObject() { return true; }
    }
}
