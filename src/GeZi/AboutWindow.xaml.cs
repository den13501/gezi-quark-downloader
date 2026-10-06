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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GeZi
{
    /// <summary>
    /// 「关于」窗口：软件介绍 + 赞赏码（可点击放大）。
    ///
    /// 【来由】用户要求「和 Python Flet 版那个差不多」——Flet 版有个 `_show_about()`
    /// 带品牌行、B 站链接、应用简介、功能特性、赞赏支持。这里按当前 C# 版的实际情况
    /// **重写了介绍内容**（Flet 版那套功能描述已经对不上了）。
    ///
    /// ⚠️ 赞赏码用的是 Flet 版里那份 `APP_REWARD_LG_B64`（360×360）解出来的
    /// `Resources/reward-qr.png` —— 同一张码，没有重画。
    /// </summary>
    public partial class AboutWindow : Window
    {
        /// <summary>作者 B 站主页（沿用 Flet 版那个短链）。</summary>
        private const string BiliUrl = "https://b23.tv/gIoEfQM";

        /// <summary>
        /// 项目开源地址（GitHub）。
        /// 【为什么放显眼处】这类工具的用户最怕"来路不明的 exe"（见过太多捆绑木马的），
        /// 源码公开是他们判断可信度的第一依据 —— 比任何"本软件无毒"的声明都有用。
        /// </summary>
        private const string RepoUrl = "https://github.com/den13501/gezi-quark-downloader";

        /// <summary>功能特性（面向普通用户，一条一句话）。</summary>
        /// <remarks>
        /// 【2026-10-03】用户提醒「怎么没在简介里写免转存功能，这不是很大的一个特点吗」——
        /// 确实漏了。补上后顺带把「多账号」也加了（也是别人常问的）。
        /// 写法要求：**说清"对你有什么好处"**，别只写功能名。
        /// </remarks>
        private static readonly string[] Features =
        {
            UiText.Get("String.Code.AboutWindow.xaml.a93c14bf6a"),
            UiText.Get("String.Code.AboutWindow.xaml.22ac6b3816"),
            UiText.Get("String.Code.AboutWindow.xaml.1bd96c1ec5"),
            UiText.Get("String.Code.AboutWindow.xaml.3d496a44cc"),
            UiText.Get("String.Code.AboutWindow.xaml.cbe8423ab2"),
            UiText.Get("String.Code.AboutWindow.xaml.c713a62c26"),
            UiText.Get("String.Code.AboutWindow.xaml.ca3ec28049"),
            UiText.Get("String.Code.AboutWindow.xaml.4f51c3922b"),
        };

        public AboutWindow()
        {
            InitializeComponent();

            // 版本号从程序集读，别再手写一份（写死了迟早对不上）
            try
            {
                var ver = Assembly.GetExecutingAssembly().GetName().Version;
                VersionText.Text = UiText.Get("String.Code.AboutWindow.xaml.cbf5538ec2") + (ver != null ? ver.ToString(3) : "2.2.0");
            }
            catch { }

            BuildFeatureList();

            Loaded += (s, e) => DialogChrome.ApplyRoundedCorners(this);
        }

        private void BuildFeatureList()
        {
            var bullet = (Brush)FindResource("PrimaryBrush");
            var text = (Brush)FindResource("TextPrimaryBrush");
            foreach (var f in Features)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 5) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var dot = new TextBlock
                {
                    Text = "·",
                    FontSize = 13,
                    Width = 14,
                    Foreground = bullet,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                Grid.SetColumn(dot, 0);
                row.Children.Add(dot);

                var t = new TextBlock
                {
                    Text = f,
                    FontSize = 11.5,
                    LineHeight = 18,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = text,
                };
                Grid.SetColumn(t, 1);
                row.Children.Add(t);

                FeatureList.Children.Add(row);
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            try { Close(); } catch { }
        }

        private void OnBiliClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ShellLaunch.OpenUrl(BiliUrl))
                    AppDialog.Show(this, UiText.Get("String.Code.AboutWindow.xaml.18109627c4") + BiliUrl, UiText.Get("String.Code.AboutWindow.xaml.d948bd90f2"));
            }
            catch { }
        }

        /// <summary>点「开源地址」→ 用默认浏览器打开 GitHub 仓库。</summary>
        private void OnRepoClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ShellLaunch.OpenUrl(RepoUrl))
                    AppDialog.Show(this, UiText.Get("String.Code.AboutWindow.xaml.18109627c4") + RepoUrl, UiText.Get("String.Code.AboutWindow.xaml.d948bd90f2"));
            }
            catch { }
        }

        /// <summary>点赞赏码 → 弹一个放大的窗口（和 Flet 版的「赞赏支持」弹窗同一个意思）。</summary>
        private void OnQrClick(object sender, RoutedEventArgs e)
        {
            try { ShowReward(); } catch { }
        }

        private void ShowReward()
        {
            var win = new Window
            {
                Owner = this,
                Title = UiText.Get("String.Code.AboutWindow.xaml.c43224900a"),
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = (Brush)FindResource("SurfaceBrush"),
                FontFamily = (FontFamily)FindResource("AppFontFamily"),
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
            };
            TextOptions.SetTextFormattingMode(win, TextFormattingMode.Display);

            // 跟小窗/关于窗一样：WindowChrome（非分层窗口，文字才清晰）+ DWM 圆角
            System.Windows.Shell.WindowChrome.SetWindowChrome(win,
                new System.Windows.Shell.WindowChrome
                {
                    CaptionHeight = 40,
                    ResizeBorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(0),
                    GlassFrameThickness = new Thickness(0),
                    UseAeroCaptionButtons = false,
                });

            var img = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/Resources/reward-qr.png")),
                Width = 340,
                Height = 340,
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

            var close = new Button
            {
                Content = UiText.Get("String.Code.AboutWindow.xaml.09614cef6c"),
                MinWidth = 96,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 14, 0, 0),
                Style = FindResource("PrimaryButton") as Style,
            };
            close.Click += (s2, e2) => { try { win.Close(); } catch { } };

            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock
            {
                Text = UiText.Get("String.Code.AboutWindow.xaml.9a6465f23b"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
            });
            panel.Children.Add(new TextBlock
            {
                Text = UiText.Get("String.Code.AboutWindow.xaml.c8a128a55c"),
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)FindResource("TextTertiaryBrush"),
            });
            panel.Children.Add(img);
            panel.Children.Add(close);

            win.Content = new Border
            {
                BorderBrush = (Brush)FindResource("BorderBrushSoft"),
                BorderThickness = new Thickness(1),
                Child = panel,
            };

            win.Loaded += (s2, e2) => DialogChrome.ApplyRoundedCorners(win);
            win.ShowDialog();
        }
    }
}
