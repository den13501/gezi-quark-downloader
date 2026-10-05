// GeZi —— 夸克網盤下載器 (Quark netdisk downloader)
// Copyright (C) 2026  Yi Yuan
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Globalization;
using System.Windows;

namespace GeZi
{
    /// <summary>
    /// 固定繁體中文介面的字串存取入口。
    /// 所有使用者可見文案集中在 Resources/Strings.zh-Hant.xaml；
    /// 目前不提供執行期語系切換，但保留未來擴充資源字典的空間。
    /// </summary>
    internal static class UiText
    {
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            try
            {
                var value = Application.Current?.TryFindResource(key) as string;
                return value ?? key;
            }
            catch
            {
                return key;
            }
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, Get(key), args ?? new object[0]);
        }
    }
}