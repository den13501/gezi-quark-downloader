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

using System.ComponentModel;
using GeZi.Core.Download;

namespace GeZi
{
    /// <summary>任务列表条目（带变更通知，供进度条/状态实时刷新）。</summary>
    public class TaskItem : INotifyPropertyChanged
    {
        public DownloadJob Job { get; }
        public string Name { get; }

        /// <summary>是否已写入下载历史。终态只记一次，防止重复上报把历史刷屏。</summary>
        public bool HistoryRecorded { get; set; }

        public TaskItem(DownloadJob job, string name)
        {
            Job = job;
            Name = name;
        }

        private JobState _state = JobState.Queued;
        public JobState State
        {
            get => _state;
            set { _state = value; OnChanged(nameof(State)); OnChanged(nameof(StatusText)); OnChanged(nameof(DetailText)); OnChanged(nameof(SpeedText)); OnChanged(nameof(CanPlay)); }
        }

        /// <summary>
        /// 这个任务现在能不能「播放」—— 决定任务行右边那个「播放」按钮显不显示。
        /// </summary>
        /// <remarks>
        /// 【2026-10-03】用户要求：「我希望当我选择边下边播的模式的时候文件的右边才会有播放选项」。
        ///
        /// 为什么要有这个判断（两种写入方式看到的文件不一样）：
        /// · **共写同一文件**：目标文件一开始就 `SetLength(total)` **预分配成完整大小**，
        ///   未下完时中间是大片全 0 的空洞 → 播放器要么判损坏，要么一拖进度条就花屏。
        /// · **独立分片+边合并**：不预分配，目标文件**从 0 连续增长** → 播放器看到的是
        ///   "被截断但内容连续"的文件，这才是能正常边下边播的情况。
        ///
        /// ⚠️ 但**已完成的任务两种模式都能播**（文件是完整的），所以完成态一律显示 ——
        /// 否则共写模式下下完的文件反而没有播放入口了，那是功能倒退。
        /// </remarks>
        public bool CanPlay
        {
            get
            {
                if (Job == null) return false;
                if (State == JobState.Completed) return true;
                return Job.WriteMode == WriteMode.SegmentedFiles;
            }
        }

        private string _error = "";
        public string Error
        {
            get => _error;
            set { _error = value ?? ""; OnChanged(nameof(Error)); OnChanged(nameof(DetailText)); }
        }

        private double _percent;
        public double Percent
        {
            get => _percent;
            set { _percent = value; OnChanged(nameof(Percent)); OnChanged(nameof(PercentText)); }
        }

        private double _speed;
        public double Speed
        {
            get => _speed;
            // SpeedText 要看它决定显示速度还是留空；StatusText 也要看它决定
            // 显示「暂停中…」还是「已暂停」（见下）。
            set { _speed = value; OnChanged(nameof(SpeedText)); OnChanged(nameof(StatusText)); }
        }

        /// <summary>
        /// 用户**已经按过暂停**，但下载器还在收尾（worker 要跑完手上那一次读）。
        /// </summary>
        /// <remarks>
        /// 【为什么需要这个标志】暂停是**闸门式**的：`PauseToken.WaitIfPausedAsync` 让 worker
        /// 在每次读完之后才阻塞，所以在途的请求要跑完、速度是**渐降**到 0 的（不是瞬间归零）。
        /// 而核心层的进度上报 `JobProgressAdapter` **永远报 `Downloading`**
        /// （见 DownloadScheduler），于是 UI 刚设好的 `Paused` 会被下一帧进度覆盖掉 ——
        /// 表现就是用户看到的「图标先变成三角、随后又变回两个竖杠」。
        ///
        /// 所以：置位后，<c>OnJobUpdate</c> 会**忽略核心报来的 Downloading**，
        /// 状态文案在速度归零前显示「暂停中…」，归零后才显示「已暂停」。
        /// </remarks>
        private bool _pauseRequested;
        public bool PauseRequested
        {
            get => _pauseRequested;
            set
            {
                _pauseRequested = value;
                OnChanged(nameof(PauseRequested));
                OnChanged(nameof(StatusText));
                OnChanged(nameof(DetailText));
            }
        }

        private long _done;
        public long Done
        {
            get => _done;
            set { _done = value; OnChanged(nameof(DoneText)); }
        }

        private long _total;
        public long Total
        {
            get => _total;
            set { _total = value; OnChanged(nameof(DoneText)); }
        }

        public string StatusText
        {
            get
            {
                switch (State)
                {
                    case JobState.Queued: return "排队中";
                    case JobState.Downloading: return "下载中";
                    case JobState.Paused:
                        // 暂停是渐进的（在途请求要跑完）→ 速度还没归零时显示「暂停中…」，
                        // 真正停下来（速度 0）才显示「已暂停」。
                        return _pauseRequested && _speed > 0.5 ? "暂停中…" : "已暂停";
                    case JobState.Completed: return "完成";
                    case JobState.Failed: return "失败";
                    case JobState.Cancelled: return "已取消";
                    case JobState.Cancelling: return "取消中…";
                    default: return "";
                }
            }
        }

        /// <summary>状态 + 失败原因，用于任务列表显示，确保错误一眼可见。</summary>
        public string DetailText
        {
            get
            {
                if (State == JobState.Failed && !string.IsNullOrEmpty(Error))
                    return "失败: " + Error;
                return StatusText;
            }
        }

        public string PercentText => _percent.ToString("F1") + "%";

        /// <summary>
        /// 速度文本。只有在"下载中"才展示速度，其余状态返回空串——
        /// 否则任务完成/暂停后最后一帧的残留速度会一直挂在界面上，看起来像还在跑。
        /// </summary>
        public string SpeedText
        {
            get
            {
                // 暂停中（在途请求还在跑）也把速度显示出来 —— 这样用户能看到
                // 它是"正在停"而不是"卡住了"，速度归零后自然变成「已暂停」。
                if (State != JobState.Downloading &&
                    !(State == JobState.Paused && _pauseRequested && _speed > 0.5))
                    return "";
                if (_speed <= 0)
                    return "…";      // 刚开始还没测出速度
                return Util.FormatSize((long)_speed) + "/s";
            }
        }

        public string DoneText => _total > 0
            ? Util.FormatSize(_done) + " / " + Util.FormatSize(_total)
            : Util.FormatSize(_done);

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
