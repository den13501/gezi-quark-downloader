#!/usr/bin/env python3
"""Apply the fixed Traditional Chinese (Taiwan) localization.

This is a development-time migration/rebase helper.  The application does not
invoke it at runtime.  It deliberately limits C# conversion to the WPF project
so protocol parsers and server-facing text in GeZi.Core remain untouched.
"""

from __future__ import annotations

import argparse
import ctypes
import hashlib
import html
import re
from pathlib import Path
from xml.sax.saxutils import escape


ROOT = Path(__file__).resolve().parents[1]
UI_ROOT = ROOT / "src" / "GeZi"
RESOURCE_PATH = UI_ROOT / "Resources" / "Strings.zh-Hant.xaml"

XAML_FILES = [
    UI_ROOT / "MainWindow.xaml",
    UI_ROOT / "MiniWindow.xaml",
    UI_ROOT / "AboutWindow.xaml",
    UI_ROOT / "AppDialog.xaml",
]

CS_FILES = [
    UI_ROOT / "AboutWindow.xaml.cs",
    UI_ROOT / "App.xaml.cs",
    UI_ROOT / "AppDialog.xaml.cs",
    UI_ROOT / "MainWindow.xaml.cs",
    UI_ROOT / "MiniWindow.xaml.cs",
    UI_ROOT / "ScanQrWindow.cs",
    UI_ROOT / "ShareFileItem.cs",
    UI_ROOT / "TaskItem.cs",
    UI_ROOT / "TrayIcon.cs",
]

VISIBLE_ATTRIBUTES = (
    "Text",
    "Content",
    "Header",
    "Title",
    "ToolTip",
    "AutomationProperties.Name",
)

# Windows performs character conversion.  These replacements then select
# terminology customary in Taiwanese desktop software instead of Mainland
# Chinese wording that happens to use Traditional glyphs.
TERMS = (
    ("誇克", "夸克"),  # Brand name: intentionally keep 夸克.
    ("鴿子下載", "鴿子下載"),
    ("文件夾", "資料夾"),
    ("文件", "檔案"),
    ("剪貼板", "剪貼簿"),
    ("托盤", "系統匣"),
    ("線程", "執行緒"),
    ("帳號", "帳號"),
    ("賬號", "帳號"),
    ("登錄", "登入"),
    ("默認", "預設"),
    ("設置", "設定"),
    ("鏈接", "連結"),
    ("程序", "程式"),
    ("軟件", "軟體"),
    ("應用簡介", "應用程式簡介"),
    ("應用內", "應用程式內"),
    ("應用圖標", "應用程式圖示"),
    ("圖標", "圖示"),
    ("窗口", "視窗"),
    ("刷新", "重新整理"),
    ("加載", "載入"),
    ("導入", "匯入"),
    ("導出", "匯出"),
    ("瀏覽目錄", "瀏覽資料夾"),
    ("目錄", "目錄"),
    ("複製鏈接", "複製連結"),
    ("點擊", "按一下"),
    ("雙擊", "按兩下"),
    ("右鍵", "按右鍵"),
    ("勾選", "勾選"),
    ("用戶", "使用者"),
    ("網絡", "網路"),
    ("服務器", "伺服器"),
    ("硬盤", "硬碟"),
    ("信息", "資訊"),
    ("視頻", "影片"),
    ("二維碼", "QR Code"),
    ("關于", "關於"),
    ("之后", "之後"),
    ("此后", "此後"),
    ("登入后", "登入後"),
    ("下載后", "下載後"),
    ("掃碼后", "掃碼後"),
    ("完成后", "完成後"),
    ("關閉后", "關閉後"),
    ("后臺", "後台"),
    ("磁盤", "磁碟"),
    ("占用", "佔用"),
    ("粘貼", "貼上"),
    ("復制", "複製"),
    ("打開", "開啟"),
    ("保存", "儲存"),
    ("日志", "記錄"),
    ("運行記錄", "執行記錄"),
    ("運行", "執行"),
    ("訪問", "存取"),
    ("昵稱", "暱稱"),
    ("獲取", "取得"),
    ("檢測", "偵測"),
    ("創建", "建立"),
    ("主頁", "首頁"),
    ("清空", "清除"),
    ("個性化", "個人化"),
    ("云盤", "雲端硬碟"),
    ("網盤檔案", "網盤檔案"),
    ("配置", "設定"),
    ("這里", "這裡"),
    ("那里", "那裡"),
    ("里邊", "裡面"),
    ("里面", "裡面"),
    ("剩余", "剩餘"),
    ("干擾", "干擾"),
    ("支持的格式", "支援的格式"),
    ("支持三種方式", "支援三種方式"),
    ("支持斷點續傳", "支援斷點續傳"),
    ("路由器支持", "路由器支援"),
    ("贊賞支持", "贊賞支持"),
    ("登入資訊經系統加密后", "登入資訊經系統加密後"),
    ("獲取QR Code", "取得 QR Code"),
    ("按一下QR Code", "按一下 QR Code"),
    ("QR Code識別", "QR Code 識別"),
    ("識別QR Code", "識別 QR Code"),
    ("取得QR Code", "取得 QR Code"),
    ("分享QR Code", "分享 QR Code"),
    ("到QR Code", "到 QR Code"),
    ("按按右鍵", "按右鍵"),
    ("本地設定里", "本機設定中"),
    ("電腦里", "電腦中"),
    (" App 里", " App 中"),
    ("客戶端里", "用戶端中"),
    ("支持", "支援"),
    ("贊賞支援", "贊賞支持"),
    ("連接數", "連線數"),
    ("個連接", "個連線"),
    ("單擊", "按一下"),
    ("退出", "結束程式"),
    ("結束程式確認", "結束確認"),
    ("直接結束程式程式", "直接結束程式"),
    ("截圖后", "截圖後"),
    ("刪除后", "刪除後"),
    ("勾選后", "勾選後"),
    ("分享頁后", "分享頁後"),
    ("選擇后", "選擇後"),
    ("接口", "介面"),
    ("隊列", "佇列"),
    ("緩存", "快取"),
    ("字節", "位元組"),
    ("處于", "處於"),
    ("低于", "低於"),
    ("不低于", "不低於"),
    ("并發", "並行"),
    ("并接", "並接"),
    ("并清", "並清"),
    ("選中", "選取"),
    ("地址", "位址"),
    ("自定義", "自訂"),
    ("活躍連接", "作用中連線"),
    ("慢連接", "慢速連線"),
    ("連接接近", "連線接近"),
    ("連接", "連線"),
    ("後臺", "背景"),
    ("存到本地", "儲存到本機"),
    ("本地檔案", "本機檔案"),
    ("本地完整路徑", "本機完整路徑"),
    ("點這裡", "按這裡"),
    ("點「", "按「"),
    ("點下方", "按下方"),
    ("一鍵", "快速"),
    ("中轉檔案", "暫存檔案"),
    ("內置", "內建"),
    ("回退", "改用"),
    ("占一份", "佔用一份"),
    ("檔案里", "檔案中"),
    ("設定里", "設定中"),
    ("稍后", "稍後"),
    ("重啟后", "重新啟動後"),
    ("指定后", "指定後"),
    ("切換后", "切換後"),
    ("后兩者", "後兩者"),
    ("范圍", "範圍"),
    ("文本", "文字"),
    ("繪制", "繪製"),
    ("等同于", "等同於"),
    ("屬于", "屬於"),
    ("等于", "等於"),
    ("并", "並"),
    ("后", "後"),
    ("里", "裡"),
    ("于", "於"),
    ("QR Code可", "QR Code 可"),
    ("QR Code可能", "QR Code 可能"),
    ("掃描QR Code", "掃描 QR Code"),
    ("QR Code繪", "QR Code 繪"),
    ("QR Code已", "QR Code 已"),
    ("QR Code圖片", "QR Code 圖片"),
    ("QR Code（", "QR Code（"),
    ("直接粘", "直接貼上"),
    ("粘進來", "貼進來"),
    ("改用到", "改用"),
    ("已改用為", "已改用"),
)


def windows_traditional(text: str) -> str:
    if not any("\u3400" <= ch <= "\u9fff" for ch in text):
        return text
    fn = ctypes.windll.kernel32.LCMapStringEx
    fn.argtypes = [
        ctypes.c_wchar_p, ctypes.c_uint, ctypes.c_wchar_p, ctypes.c_int,
        ctypes.c_wchar_p, ctypes.c_int, ctypes.c_void_p, ctypes.c_void_p,
        ctypes.c_longlong,
    ]
    fn.restype = ctypes.c_int
    flags = 0x04000000  # LCMAP_TRADITIONAL_CHINESE
    needed = fn("zh-TW", flags, text, len(text), None, 0, None, None, 0)
    if needed <= 0:
        raise ctypes.WinError()
    buffer = ctypes.create_unicode_buffer(needed)
    if fn("zh-TW", flags, text, len(text), buffer, needed, None, None, 0) <= 0:
        raise ctypes.WinError()
    return buffer.value


def localize(text: str) -> str:
    result = windows_traditional(text)
    for old, new in TERMS:
        result = result.replace(old, new)
    return result


def unescape_entities(text: str) -> str:
    previous = None
    while text != previous:
        previous = text
        text = html.unescape(text)
    return text


def read_text(path: Path) -> tuple[str, bool]:
    raw = path.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    return raw.decode("utf-8-sig"), bom


def write_text(path: Path, text: str, bom: bool) -> None:
    data = text.encode("utf-8")
    if bom:
        data = b"\xef\xbb\xbf" + data
    path.write_bytes(data)


def xml_value(text: str) -> str:
    return (escape(text, {'"': "&quot;"})
            .replace("\r", "&#xD;")
            .replace("\n", "&#xA;")
            .replace("\t", "&#x9;"))


def decode_csharp_string(body: str, verbatim: bool) -> str:
    if verbatim:
        return body.replace('""', '"')
    replacements = {
        "\\": "\\",
        '"': '"',
        "'": "'",
        "0": "\0",
        "a": "\a",
        "b": "\b",
        "f": "\f",
        "n": "\n",
        "r": "\r",
        "t": "\t",
        "v": "\v",
    }
    out: list[str] = []
    i = 0
    while i < len(body):
        if body[i] != "\\" or i + 1 >= len(body):
            out.append(body[i])
            i += 1
            continue
        code = body[i + 1]
        if code in replacements:
            out.append(replacements[code])
            i += 2
            continue
        if code == "u" and i + 5 < len(body):
            out.append(chr(int(body[i + 2:i + 6], 16)))
            i += 6
            continue
        if code == "U" and i + 9 < len(body):
            out.append(chr(int(body[i + 2:i + 10], 16)))
            i += 10
            continue
        if code == "x":
            match = re.match(r"[0-9A-Fa-f]{1,4}", body[i + 2:])
            if match:
                out.append(chr(int(match.group(0), 16)))
                i += 2 + len(match.group(0))
                continue
        # Unknown escape: preserve it instead of silently changing semantics.
        out.append("\\" + code)
        i += 2
    return "".join(out)


def load_resources() -> dict[str, str]:
    if not RESOURCE_PATH.exists():
        return {}
    text, _ = read_text(RESOURCE_PATH)
    return {
        key: xml_value(localize(unescape_entities(value)))
        for key, value in re.findall(
            r'<sys:String\s+x:Key="([^"]+)">(.*?)</sys:String>', text, re.S
        )
    }


def resource_key(file_stem: str, owner: str, attr: str, source: str) -> str:
    owner = re.sub(r"[^A-Za-z0-9]+", "", owner) or "Element"
    attr = attr.replace("AutomationProperties.", "Automation")
    digest = hashlib.sha1(source.encode("utf-8")).hexdigest()[:8]
    return f"String.{file_stem}.{owner}.{attr}.{digest}"


def convert_xaml(path: Path, resources: dict[str, str]) -> int:
    text, bom = read_text(path)
    changed = 0
    parts = re.split(r"(<!--.*?-->)", text, flags=re.S)
    attr_pattern = re.compile(
        r"(?P<name>" + "|".join(re.escape(x) for x in VISIBLE_ATTRIBUTES)
        + r')="(?P<value>[^"]*)"'
    )

    def convert_tag(match: re.Match[str]) -> str:
        nonlocal changed
        tag = match.group(0)
        tag_name_match = re.match(r"<([\w:.]+)", tag)
        if not tag_name_match:
            return tag
        tag_name = tag_name_match.group(1).split(":")[-1]
        named = re.search(r'\bx:Name="([^"]+)"', tag)
        owner = named.group(1) if named else tag_name

        def replace_attr(attr_match: re.Match[str]) -> str:
            nonlocal changed
            name = attr_match.group("name")
            value = attr_match.group("value")
            if value.startswith("{") or not any("\u3400" <= c <= "\u9fff" for c in value):
                return attr_match.group(0)
            localized = localize(unescape_entities(value))
            key = resource_key(path.stem, owner, name, value)
            resources[key] = xml_value(localized)
            changed += 1
            return f'{name}="{{DynamicResource {key}}}"'

        return attr_pattern.sub(replace_attr, tag)

    for i in range(0, len(parts), 2):
        parts[i] = re.sub(r"<[A-Za-z][^<>]*>", convert_tag, parts[i], flags=re.S)
    output = "".join(parts)
    if output != text:
        write_text(path, output, bom)
    return changed


def csharp_resource_key(path: Path, source: str) -> str:
    digest = hashlib.sha1(source.encode("utf-8")).hexdigest()[:10]
    return f"String.Code.{path.stem}.{digest}"


def convert_csharp(path: Path, resources: dict[str, str]) -> int:
    text, bom = read_text(path)
    output: list[str] = []
    i = 0
    changed = 0
    state = "code"
    while i < len(text):
        if state == "line_comment":
            end = text.find("\n", i)
            if end < 0:
                output.append(text[i:])
                break
            output.append(text[i:end + 1])
            i = end + 1
            state = "code"
            continue
        if state == "block_comment":
            end = text.find("*/", i)
            if end < 0:
                output.append(text[i:])
                break
            output.append(text[i:end + 2])
            i = end + 2
            state = "code"
            continue

        if text.startswith("//", i):
            state = "line_comment"
            continue
        if text.startswith("/*", i):
            state = "block_comment"
            continue

        prefix_len = 0
        verbatim = False
        if text.startswith('$@"', i) or text.startswith('@$"', i):
            prefix_len = 3
            verbatim = True
        elif text.startswith('@"', i):
            prefix_len = 2
            verbatim = True
        elif text.startswith('$"', i):
            prefix_len = 2
        elif text[i] == '"':
            prefix_len = 1

        if prefix_len:
            start = i
            i += prefix_len
            while i < len(text):
                if verbatim:
                    if text.startswith('""', i):
                        i += 2
                        continue
                    if text[i] == '"':
                        i += 1
                        break
                    i += 1
                else:
                    if text[i] == "\\":
                        i += 2
                        continue
                    if text[i] == '"':
                        i += 1
                        break
                    i += 1
            literal = text[start:i]
            converted = localize(literal)
            body_start = prefix_len
            body = converted[body_start:-1]
            has_chinese = any("\u3400" <= ch <= "\u9fff" for ch in body)
            is_interpolated = "$" in converted[:body_start]
            statement_start = max(text.rfind(";", 0, start), text.rfind("{", 0, start), text.rfind("}", 0, start)) + 1
            prefix = text[statement_start:start]
            is_case_label = bool(re.search(r"\bcase\s*$", prefix))
            if has_chinese and not is_interpolated and not is_case_label:
                key = csharp_resource_key(path, literal)
                resources[key] = xml_value(decode_csharp_string(body, verbatim))
                output.append(f'UiText.Get("{key}")')
                changed += 1
            else:
                if converted != literal:
                    changed += 1
                output.append(converted)
            continue

        if text[i] == "'":
            start = i
            i += 1
            while i < len(text):
                if text[i] == "\\":
                    i += 2
                    continue
                if text[i] == "'":
                    i += 1
                    break
                i += 1
            output.append(text[start:i])
            continue

        output.append(text[i])
        i += 1

    result = "".join(output)
    # A resource lookup is not a compile-time constant.  The WPF project only
    # has class-level localized const strings; make those fields readonly.
    result = re.sub(
        r"\bprivate\s+const\s+string\s+(\w+)\s*=\s*(UiText\.Get\([^;]+\));",
        r"private static readonly string \1 = \2;",
        result,
    )
    if result != text:
        write_text(path, result, bom)
    return changed


def write_resources(resources: dict[str, str]) -> None:
    lines = [
        '<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"',
        '                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"',
        '                    xmlns:sys="clr-namespace:System;assembly=mscorlib">',
        '    <!-- Fixed Traditional Chinese (Taiwan) UI strings. -->',
    ]
    for key in sorted(resources):
        lines.append(f'    <sys:String x:Key="{key}">{resources[key]}</sys:String>')
    lines.append('</ResourceDictionary>')
    RESOURCE_PATH.write_text("\n".join(lines) + "\n", encoding="utf-8")


def referenced_resource_keys() -> set[str]:
    """Return localized string keys referenced by the migrated UI sources."""
    keys: set[str] = set()
    for path in XAML_FILES + CS_FILES:
        text, _ = read_text(path)
        keys.update(re.findall(r'UiText\.Get\("([^"]+)"\)', text))
        keys.update(re.findall(
            r'\{(?:Dynamic|Static)Resource\s+(String\.[^}\s]+)', text
        ))
    return keys


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apply", action="store_true", help="write localized files")
    args = parser.parse_args()
    if not args.apply:
        parser.error("This migration tool only runs with --apply")

    resources = load_resources()
    xaml_count = sum(convert_xaml(path, resources) for path in XAML_FILES)
    cs_count = sum(convert_csharp(path, resources) for path in CS_FILES
                   if path.name != "UiText.cs")
    referenced = referenced_resource_keys()
    resources = {key: value for key, value in resources.items()
                 if key in referenced}
    write_resources(resources)
    print(f"Localized {xaml_count} XAML attributes and {cs_count} C# string literals.")
    print(f"Resource keys: {len(resources)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())