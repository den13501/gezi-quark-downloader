// GeZi —— 夸克网盘下载器 (Quark netdisk downloader)
// Copyright (C) 2026  Yi Yuan
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Runtime.InteropServices;

namespace GeZi
{
    /// <summary>
    /// 平台原生调用集中处（net48、零 NuGet，只能走 P/Invoke）。
    ///
    /// 注意：本工作区内的 exe 会被以**低完整性**启动（沙箱给目录打了
    /// Low 标签），低完整性进程不能与 shell 交互 —— 所以涉及外部程序启动/激活的
    /// 调用，**必须在工作区外运行才能验证成功**。
    /// </summary>
    internal static class NativeMethods
    {
        // ---------------- 文件删除（占用时登记重启后删除） ----------------

        internal const int MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, int dwFlags);

        // ---------------- 窗口激活（单实例：把已有实例的主窗拉起来） ----------------

        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowTextW(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        internal const int SW_SHOWNORMAL = 1;
        internal const int SW_RESTORE = 9;

        // ---------------- 单实例：跨进程「请把主窗拉起来」通知 ----------------
        //
        // 为什么不用「新进程自己 SetForegroundWindow」：Windows 有**前台锁定**，
        // 后台进程调 SetForegroundWindow 常常只让任务栏闪一下，窗口并不会真的前置
        // （尤其是旧窗口处于隐藏/最小化时）。可靠做法是让**旧进程自己**去激活 ——
        // 它收到消息后调 Show()+Activate()，属于"响应用户操作"，允许抢前台。
        //
        // 用 RegisterWindowMessage 注册一条全局唯一消息 + HWND_BROADCAST 广播：
        // 不需要先找到窗口句柄，也不需要跨进程管道。
        // ⚠️ 广播消息对**隐藏**的顶层窗口同样会送达（IsWindowVisible 不影响投递），
        //    所以"最小化到托盘"状态下再点 exe 也能被唤醒。

        internal const int HWND_BROADCAST = 0xFFFF;
        internal const int ASFW_ANY = -1;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int RegisterWindowMessageW(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool AllowSetForegroundWindow(int dwProcessId);

        [DllImport("user32.dll")]
        internal static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetActiveWindow(IntPtr hWnd);

        // ---------------- 原生标题栏深浅（深色模式） ----------------

        /// <summary>
        /// 让窗口的非客户区（标题栏）按新设置重画。
        /// 改完 `DWMWA_USE_IMMERSIVE_DARK_MODE` 后用 SWP_FRAMECHANGED 触发一次重算，
        /// 否则个别 Windows 版本上标题栏要等窗口被激活才刷新。
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOMOVE = 0x0002;
        internal const uint SWP_NOZORDER = 0x0004;
        internal const uint SWP_NOACTIVATE = 0x0010;
        internal const uint SWP_FRAMECHANGED = 0x0020;
    }
}
