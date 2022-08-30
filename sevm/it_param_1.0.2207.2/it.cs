using egg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

/// <summary>
/// 此应用的快捷使用通道
/// </summary>
public static partial class it {
    //// 日志管理器
    //private static egg.Logger logger;

    /// <summary>
    /// 目录分隔符
    /// </summary>
    //internal static egg.Console.EParams Arguments { get; private set; }

    /// <summary>
    /// 工作目录
    /// </summary>
    internal static string WorkPath { get; private set; }

    /// <summary>
    /// 执行目录
    /// </summary>
    internal static string ExecPath { get; private set; }

    /// <summary>
    /// 执行文件
    /// </summary>
    internal static string ExecFile { get; private set; }

    /// <summary>
    /// 程序版本
    /// </summary>
    internal static string Version { get; private set; }

    /// <summary>
    /// 当前IP地址
    /// </summary>
    internal static string IPAddress { get; private set; }

    /// <summary>
    /// 获取是否为调试模式
    /// </summary>
    internal static bool DebugConsole { get; private set; }

    /// <summary>
    /// 获取是否为调试模式
    /// </summary>
    internal static bool DebugLogger { get; private set; }

    /// <summary>
    /// 输出内容
    /// </summary>
    /// <param name="content"></param>
    public static void Debug(string content) {
        // 输出日志
        //if (it.DebugLogger) logger.Write(content);
        if (it.DebugConsole) Console.Write(content);
    }

    /// <summary>
    /// 输出内容
    /// </summary>
    /// <param name="content"></param>
    public static void Debug(string content, ConsoleColor color) {
        // 输出日志
        //if (it.DebugLogger) logger.Write(content);
        if (it.DebugConsole) {
            ConsoleColor colorBefore = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(content);
            Console.ForegroundColor = colorBefore;
        }
    }

    /// <summary>
    /// 输出内容
    /// </summary>
    /// <param name="content"></param>
    internal static void DebugLine(string content = null, bool hasTime = true) {
        if (content.IsEmpty()) {
            it.Debug("\r\n");
        } else {
            if (hasTime) {
                it.Debug($"{eggs.Time.GetNowTime()} {content}\r\n");
            } else {
                it.Debug($"{content}\r\n");
            }
        }
    }

    /// <summary>
    /// 输出普通内容
    /// </summary>
    /// <param name="content"></param>
    internal static void DebugInfo(string content) {
        it.Debug("* ", ConsoleColor.DarkGreen);
        it.DebugLine(content);
    }

    /// <summary>
    /// 输出添加内容
    /// </summary>
    /// <param name="content"></param>
    internal static void DebugAdd(string content) {
        it.Debug("+ ", ConsoleColor.Yellow);
        it.DebugLine(content);
    }

    /// <summary>
    /// 输出添加内容
    /// </summary>
    /// <param name="content"></param>
    internal static void DebugDel(string content) {
        it.Debug("- ", ConsoleColor.Gray);
        it.DebugLine(content);
    }

    /// <summary>
    /// 输出警告内容
    /// </summary>
    /// <param name="content"></param>
    internal static void DebugWarnning(string content) {
        it.Debug("! ", ConsoleColor.Red);
        it.DebugLine(content);
    }

    /// <summary>
    /// 输出带空格的分级调试内容
    /// </summary>
    /// <param name="content"></param>
    internal static void DebugInside(string content, int lv = 1) {
        // 输出空格
        for (int i = 0; i < lv; i++) {
            it.Debug("    ");
        }
        it.Debug($"{content}\r\n");
    }

    /// <summary>
    /// 获取路径
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    internal static string GetPath(string path) {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return path.Replace('/', '\\');
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// 获取闭合文件夹路径
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    internal static string GetClosedDirectoryPath(string path) {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            if (!path.EndsWith("\\")) path += "\\";
        } else {
            if (!path.EndsWith("/")) path += "/";
        }
        return path;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="args"></param>
    /// <param name="debugConsole"></param>
    /// <param name="debugLogger"></param>
    internal static void Initialize(string[] args, bool debugConsole = true, bool debugLogger = false) {
        // 读取程序版本号
        it.Version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

        // 获取执行目录
        string execPath = AppContext.BaseDirectory;
        it.ExecPath = GetClosedDirectoryPath(execPath);
        it.ExecFile = Process.GetCurrentProcess().MainModule.FileName;

        // 初始化日志管理器
        it.DebugLogger = debugLogger;
        it.DebugConsole = debugConsole;
        string logPath = $"{it.ExecPath}Logs";
        if (!System.IO.Directory.Exists(logPath)) System.IO.Directory.CreateDirectory(logPath);
        //logger = new Logger(logPath);
        it.DebugInfo($"Version {it.Version}");
        it.DebugInfo($"Program.ExecPath {it.ExecPath}");

        // 获取所有参数
        //it.Arguments = new egg.Console.EParams(args);
        //foreach (var key in it.Arguments.Keys) {
        //    it.DebugInfo($"[Param] {key} = {it.Arguments[key]}");
        //}
        //it.DebugInfo($"[Params] {it.Arguments.ToString()}");

        // 设置默认工作目录为当前目录
        string workpath = Environment.CurrentDirectory;

        // 获取文件参数
        //string file = it.Arguments["file"];
        //if (!file.IsEmpty()) {
        //    workpath = System.IO.Path.GetDirectoryName(file);
        //}

        // 获取工作目录
        //if (it.Arguments.ContainsKey("path")) {
        //    workpath = it.Arguments["path"];
        //}
        it.WorkPath = GetClosedDirectoryPath(workpath);
        it.DebugInfo($"Program.WorkPath {it.WorkPath}");

        // 获取当前IP地址
        it.IPAddress = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
            .Select(p => p.GetIPProperties())
            .SelectMany(p => p.UnicastAddresses)
            .Where(p => p.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !System.Net.IPAddress.IsLoopback(p.Address))
            .FirstOrDefault()?.Address.ToString();

        // 初始化Egger

        // 初始化设置类
        //string cfgPath = $"{it.ExecPath}conf";
        //if (!System.IO.Directory.Exists(cfgPath)) System.IO.Directory.CreateDirectory(cfgPath);
        //it.App.Init(cfgPath);
        //it.DebugInfo($"Config.WorkFolder {it.Config.WorkFolder}");

        //// 设置开发者模式
        //it.DebugInfo($"Config.DebugLogger {it.Config.Program.DebugLogger}");
        //it.DebugLogger = it.Config.Program.DebugLogger;
        //it.DebugInfo($"Config.DebugConsole {it.Config.Program.DebugConsole}");
        //it.DebugConsole = it.Config.Program.DebugConsole;

        // 初始化数据库设置
        // it.Database.Initialize();
    }
}