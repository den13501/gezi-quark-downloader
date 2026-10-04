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
using System.Collections.Generic;
using System.Text;

namespace GeZi.Core.Support
{
    /// <summary>
    /// 直链导出：把已取到的直链生成可直接粘贴到外部下载器的文本。
    ///
    /// 参考同类开源项目 QuarkDownloader（油猴脚本）的导出实现：
    /// 它在下发直链时**一定同时给出匹配的 User-Agent 与 Cookie**，
    /// 而不是只丢一个裸链接 —— 裸链接粘到 aria2/IDM 里必然 403，
    /// 因为夸克直链有防盗链，缺 UA/Cookie 会被直接拒。
    ///
    /// 本类输出的格式都遵循这一原则：
    ///   1) aria2c 命令（-x/-s 取自同族项目的经验值）
    ///   2) curl 命令（-C - 断点续传）
    ///   3) 纯链接列表（给 IDM/Motrix 等靠「复制到剪贴板」或 URL 协议的场景）
    ///   4) 带文件名的直链列表（文件名 + Tab + 链接，方便肉眼对照）
    /// </summary>
    public static class LinkExporter
    {
        /// <summary>导出格式。</summary>
        public enum Format
        {
            /// <summary>aria2c 命令行（每行一条）。</summary>
            Aria2,
            /// <summary>curl 命令行（每行一条）。</summary>
            Curl,
            /// <summary>纯直链，每行一条（不含 UA/Cookie）。</summary>
            PlainUrls,
            /// <summary>
            /// 带文件名的直链列表：每行「文件名 + 制表符 + 直链」。
            /// 【2026-10-02】纯 URL 一行一条看不出哪条对应哪个文件，加上文件名后方便对照。
            /// 枚举名保留 Labeled（历史命名），界面显示名是「纯直链」——
            /// 用户明确要求「把名字改回原本的名字纯直链」，别再显示成「标注版」。
            /// </summary>
            Labeled,
        }

        /// <summary>
        /// 单条导出项。
        /// <paramref name="OutputPath"/> 是相对目录的相对路径（含子目录），
        /// 用于让外部下载器保持目录结构 —— 对应 QuarkDownloader 的 fullPath 字段。
        /// </summary>
        public class Item
        {
            public string Url { get; set; }
            public string OutputPath { get; set; }
        }

        /// <summary>
        /// curl 与 aria2 的并发参数。取 16 而非更高的值，理由有二：
        ///   1) 同类项目的经验值 —— QuarkDownloader 导出的是 `-x 16 -s 16`，
        ///      云析上限 32、aria2 默认 `-s 5`，16 是各家折中后的共识区间；
        ///   2) 夸克 CDN 按连接限速且对异常并发有反制，16 路已能吃满多数家宽，
        ///      再往上更容易触发限流（GeZi 自身 512 档位的实测也印证了这点）。
        /// </summary>
        private const int ExportConcurrency = 16;

        /// <summary>
        /// 生成导出文本。
        /// </summary>
        /// <param name="items">待导出的条目（URL + 相对输出路径）。</param>
        /// <param name="ua">下载用 User-Agent，必须是取链时同族的客户端 UA。</param>
        /// <param name="cookie">完整 Cookie 串。</param>
        /// <param name="referer">Referer，通常为 https://pan.quark.cn/。</param>
        /// <param name="format">输出格式。</param>
        public static string Build(IList<Item> items, string ua, string cookie,
            string referer, Format format)
        {
            var sb = new StringBuilder();
            if (items == null || items.Count == 0)
                return sb.ToString();

            foreach (var it in items)
            {
                if (it == null || string.IsNullOrEmpty(it.Url))
                    continue;

                // 统一转成 POSIX 风格：外部下载器多是命令行工具，跑在 bash/WSL 下，
                // 反斜杠会被当成转义符而不是分隔符。mkdir 与 -o 必须用同一套分隔符，
                // 否则会「在 A 目录建好、往 B\C 写文件」，白白建出错目录。
                string outPath = string.IsNullOrEmpty(it.OutputPath)
                    ? ""
                    : it.OutputPath.Replace('\\', '/');

                switch (format)
                {
                    case Format.PlainUrls:
                        sb.AppendLine(it.Url);
                        break;

                    case Format.Labeled:
                        // 带文件名的纯直链：文件名（含相对目录）+ 制表符 + 直链。
                        // 制表符对齐方便肉眼对照；用原始 OutputPath（含子目录）更贴近用户看到的路径。
                        sb.Append(string.IsNullOrEmpty(it.OutputPath) ? "(未知文件)" : it.OutputPath)
                          .Append('\t').Append(it.Url).AppendLine();
                        break;

                    case Format.Aria2:
                        // 目录保持：aria2 不会自动建目录，需先 mkdir。
                        AppendMkdir(sb, outPath);
                        sb.Append("aria2c -c -x ").Append(ExportConcurrency)
                          .Append(" -s ").Append(ExportConcurrency)
                          .Append(" \"").Append(it.Url).Append('"');
                        if (!string.IsNullOrEmpty(outPath))
                            sb.Append(" -o \"").Append(outPath).Append('"');
                        if (!string.IsNullOrEmpty(ua))
                            sb.Append(" -U \"").Append(ua).Append('"');
                        // aria2 的自定义 header 需要逐条给出；Cookie 与 Referer
                        // 都是防盗链校验项，缺任一都可能 403。
                        sb.Append(" --header=\"Cookie: ").Append(cookie ?? "").Append('"');
                        if (!string.IsNullOrEmpty(referer))
                            sb.Append(" --header=\"Referer: ").Append(referer).Append('"');
                        sb.AppendLine();
                        sb.AppendLine();
                        break;

                    case Format.Curl:
                        AppendMkdir(sb, outPath);
                        sb.Append("curl -L -C - \"").Append(it.Url).Append('"');
                        if (!string.IsNullOrEmpty(outPath))
                            sb.Append(" -o \"").Append(outPath).Append('"');
                        if (!string.IsNullOrEmpty(ua))
                            sb.Append(" -A \"").Append(ua).Append('"');
                        if (!string.IsNullOrEmpty(cookie))
                            sb.Append(" -b \"").Append(cookie).Append('"');
                        if (!string.IsNullOrEmpty(referer))
                            sb.Append(" -e \"").Append(referer).Append('"');
                        sb.AppendLine();
                        sb.AppendLine();
                        break;
                }
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// 若输出路径含子目录，前置一条 mkdir -p，保证外部下载器能写入。
        /// 入参必须是已转好 POSIX 分隔符的路径（调用方统一处理）。
        /// </summary>
        private static void AppendMkdir(StringBuilder sb, string posix)
        {
            if (string.IsNullOrEmpty(posix))
                return;
            int slash = posix.LastIndexOf('/');
            if (slash <= 0)
                return;
            string dir = posix.Substring(0, slash);
            sb.Append("mkdir -p \"").Append(dir).Append("\" && ");
        }
    }
}
