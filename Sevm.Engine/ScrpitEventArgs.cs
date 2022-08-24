using System;
using System.Collections.Generic;
using System.Text;

namespace Sevm.Engine {

    /// <summary>
    /// 脚本事件参数
    /// </summary>
    public class ScrpitEventArgs : System.EventArgs {

        /// <summary>
        /// 获取或设置名称
        /// </summary>
        public string Func { get; set; }

    }
}
