# SEVM(Script Execution Virtual Machine)

使用SIR中间语言的脚本执行虚拟机

## 设计初衷

脚本语言从诞生开始，就一直饱受执行效率的困扰，在很多领域，比如ERP、OA、游戏等项目中，低代码或者说脚本语言又是不可获取的存在。

在翻阅一些资料以后，我决定采用LLVM的概念，将脚本语言也进行前后端分离，前端为我们自定义的高级语言脚本，后端则使用宿主语言设计一个完整的脚本执行虚拟机，而中间则采用一套专用于虚拟机工作的可二进制化中间指令语言来提高执行效率。

## 中间语言

SIR：Script Inter-language <https://github.com/inmount/SIR>