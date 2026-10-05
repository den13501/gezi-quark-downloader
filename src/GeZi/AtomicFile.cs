// GeZi —— 夸克网盘下载器 (Quark netdisk downloader)
// Copyright (C) 2026  Yi Yuan
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.IO;

namespace GeZi
{
    /// <summary>同目錄暫存、flush 後原子替換，並保留一份可復原備份。</summary>
    internal static class AtomicFile
    {
        public static void Write(string path, Action<Stream> writer)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("寫入路徑不可為空。", nameof(path));
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string tmp = path + ".tmp";
            string backup = path + ".bak";
            try
            {
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write,
                    FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    writer(fs);
                    fs.Flush(true);
                }

                if (File.Exists(path))
                {
                    if (File.Exists(backup))
                        File.Delete(backup);
                    File.Replace(tmp, path, backup, true);
                }
                else
                {
                    File.Move(tmp, path);
                }
            }
            catch
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                throw;
            }
        }
    }
}