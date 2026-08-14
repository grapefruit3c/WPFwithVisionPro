# WPFwithVisionPro

> 通用 Cognex VisionPro VPP 宿主控件 —— 加载 `.vpp` 文件，动态枚举终端，喂图运行，显示结果。

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x86-blue)]()
[![.NET](https://img.shields.io/badge/.NET%20Framework-4.8-blueviolet)]()
[![Version](https://img.shields.io/badge/version-v0.2.0--alpha-orange)]()

---

## 项目简介

WPFwithVisionPro 是一个 WPF 用户控件，用于在自有程序中加载和运行 Cognex VisionPro 的 `.vpp` 项目文件。无需打开 VisionPro QuickBuild 或 ToolBlock 编辑器，即可：

- 加载任意 `.vpp` 文件（CogToolBlock / CogJobManager / 单工具）
- 自动识别 VPP 类型并枚举输入/输出终端
- 手动加载测试图像（支持 VisionPro `.idb` 格式和标准位图）
- 运行视觉任务并查看结果记录（叠加图形显示）
- 动态枚举输出终端的返回值

适用于**视觉算法离线调试、产线程序集成、VPP 快速验证**等场景。

---

## 技术栈

| 项目 | 说明 |
|------|------|
| 框架 | WPF (.NET Framework 4.8) |
| 语言 | C# |
| 视觉库 | Cognex VisionPro（32 位 x86） |
| 互操作 | WindowsFormsHost（嵌入 CogDisplay / CogRecordDisplay） |
| 图像格式 | VisionPro `.idb` / `.cdb` + 标准 BMP/JPG/PNG/TIFF |

---

## 项目结构

```
WPFwithVisionPro/
├── Controls/
│   ├── VisionProHostControl.xaml          # UI 布局（显示区 + 控制面板）
│   └── VisionProHostControl.xaml.cs       # 核心逻辑（加载/枚举/运行/显示）
├── Core/
│   └── CogImageHelper.cs                  # 图像转换（Bitmap → ICogImage / .idb 读取）
├── App.xaml / App.xaml.cs                 # 应用入口
├── MainWindow.xaml / MainWindow.xaml.cs   # 主窗口（宿主 VisionProHostControl）
├── VisionProVppHost.csproj                # 工程文件（x86 / net48）
├── .gitignore
├── LICENSE
└── README.md
```

---

## 快速开始

### 环境要求

- Windows 10/11
- Cognex VisionPro 9.x / 8.x（32 位，安装于 `C:\Program Files (x86)\Cognex\VisionPro`）
- Visual Studio 2022（含 .NET Framework 4.8 开发工具包）
- 解决方案平台设为 **x86**

### 编译运行

1. 克隆仓库
   ```bash
   git clone https://github.com/grapefruit3c/WPFwithVisionPro.git
   ```
2. 用 VS2022 打开 `VisionProVppHost.sln`
3. 确认解决方案平台为 **x86**
4. F5 编译运行

### 使用流程

```
加载 VPP → 自动识别类型 → 枚举终端 → 加载图片 → 运行 → 查看结果
```

1. 点击「加载 VPP」选择 `.vpp` 文件
2. 程序自动识别类型（CogToolBlock / CogJobManager / 单工具）
3. **CogToolBlock 模式**：自动枚举输入终端，可在文本框中修改参数
4. **QuickBuild 模式**：列出所有 Job，选择要运行的 Job
5. 点击「加载图片」选择测试图像（`.idb` 或标准位图）
6. 点击「运行」执行视觉任务
7. 显示区展示图像 + 叠加的图形结果
8. 输出结果面板展示终端返回值

### 复用到你的项目

`VisionProHostControl` 是独立的 UserControl，直接拷贝 `Controls/` 和 `Core/` 文件夹到你的 WPF 项目中：

```xml
<!-- 添加命名空间引用 -->
xmlns:ctl="clr-namespace:VisionProVppHost.Controls"

<!-- 使用控件 -->
<ctl:VisionProHostControl/>
```

需在 csproj 中添加 VisionPro DLL 引用（参见下方[依赖说明](#依赖说明)）。

---

## 依赖说明

工程引用以下 VisionPro 程序集（来自 `ReferencedAssemblies` 目录，运行时从 GAC_32 解析）：

| DLL | 用途 |
|-----|------|
| `Cognex.VisionPro.Core.dll` | 核心类型（ICogImage, CogSerializer 等） |
| `Cognex.VisionPro.dll` | 基础工具接口 |
| `Cognex.VisionPro.Display.Controls.dll` | CogDisplay / CogRecordDisplay |
| `Cognex.VisionPro.QuickBuild.Core.dll` | CogJobManager / CogJob |
| `Cognex.VisionPro.ToolGroup.dll` | CogToolGroup |
| `Cognex.VisionPro.ToolBlock.dll` | CogToolBlock |
| `Cognex.VisionPro.Controls.dll` | WinForms 显示控件 |
| `Cognex.VisionPro.ImageFile.dll` | CogImageFile（.idb 读取） |

> 如果你的 `.vpp` 使用了 PMAlign、Caliper、Blob 等特定工具，需在 csproj 中补充对应 DLL 引用。

---

## 核心设计

### VPP 类型自适应

```
CogSerializer.LoadObjectFromFile(path)
         │
         ├── CogToolBlock   → 枚举 Inputs/Outputs 终端
         ├── CogJobManager  → 枚举 Jobs，选择 Job 运行
         ├── CogToolGroup   → 遍历子工具设置图像
         └── ICogTool       → 直接运行
```

### QuickBuild 图像注入策略

QuickBuild 的 Job 内部通过 AcqFifo（图像采集源）自动喂图，不暴露图像输入终端。本项目采用多策略注入：

| 策略 | 适用场景 | 实现方式 |
|------|---------|---------|
| 终端设置 | CogToolBlock | 遍历 Inputs，匹配 ICogImage 类型终端 |
| 脚本终端 | CogToolGroup | 尝试 SetScriptTerminalData 常见终端名 |
| 子工具遍历 | CogToolGroup | 遍历 Tools 集合，对每个子工具递归设置 |
| 反射兜底 | 其他工具 | 反射设置 InputImage 属性 |

---

## 技术难点

### 1. 32 位 / 64 位 mixed-mode 程序集冲突

VisionPro 的 DLL 是 32 位 mixed-mode 程序集，注册在 GAC_32。工程必须设为 **x86**，否则运行时抛出 `BadImageFormatException`。编译引用 `ReferencedAssemblies` 目录的引用程序集，运行时从 GAC 解析（`Private=False` 不复制到输出目录）。

### 2. QuickBuild 图像注入

QuickBuild 的 `CogJobManager` → `CogJob` → `VisionTool` 链路中，图像通过 AcqFifo 内部注入，外部无法直接设置。不同 VisionTool 类型（CogToolBlock / CogToolGroup）的图像注入方式完全不同，需要多策略兼容。`CogToolGroup` 更是只有 `Tools` 集合和 `SetScriptTerminalData`，没有标准终端接口。

### 3. WPF 与 WinForms 互操作

VisionPro 的 `CogDisplay` 和 `CogRecordDisplay` 是 WinForms 控件，需要通过 `WindowsFormsHost` 嵌入 WPF。两个控件叠加在同一个 Grid 中，通过 `Visibility` 切换显示。

### 4. CogImageFile 读取 .idb 格式

VisionPro 的 `.idb` 图像数据库是私有格式，无法用标准 Bitmap 读取。需要通过 `CogImageFile` 类的 `Open` / `Count` / `Item[index]` API 加载。

### 5. Fit 方法版本兼容

不同 VisionPro 版本的 `CogDisplay.Fit()` 签名不同（无参 vs `Fit(bool)`），使用反射兼容调用。

---

## 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| v0.1.0-alpha | 2026-08-14 | 初始版本：支持 CogToolBlock 加载/运行/显示 |
| v0.2.0-alpha | 2026-08-14 | 新增 QuickBuild 支持、.idb 图像格式、CogToolGroup 多策略注入 |

---

## 开发目标

### 近期目标（v0.3.0）

- [ ] 多图浏览：`.idb` 文件包含多张图时，支持翻页切换
- [ ] 批量运行：对 `.idb` 中所有图像批量执行并统计结果
- [ ] 结果导出：输出终端值导出为 CSV / JSON
- [ ] 快捷键支持：F5 运行、Ctrl+O 加载 VPP、Ctrl+I 加载图片

### 中期目标（v0.5.0）

- [ ] 相机集成：支持 Hikvision 相机实时取图喂给 VPP
- [ ] 后台运行：多线程执行，不阻塞 UI
- [ ] 结果判定：支持设置 Pass/Fail 判定规则
- [ ] 统计面板：运行次数、合格率、耗时统计

### 远期目标（v1.0.0）

- [ ] 多 VPP 管理：同时加载多个 VPP，切换运行
- [ ] 参数持久化：保存输入终端参数配置
- [ ] 日志持久化：运行日志写入文件
- [ ] NuGet 打包：发布为 NuGet 包，一行引用集成
- [ ] 插件机制：支持自定义结果处理插件

---

## 已知限制

- **平台限制**：仅支持 x86，无法在 64 位进程中运行
- **QuickBuild 内部图像源**：当 VPP 配置为使用相机 AcqFifo 且未手动加载图片时，运行会尝试触发相机采集
- **工具特定 DLL**：如果 `.vpp` 使用了 PMAlign、Caliper、Blob 等工具，需手动补充对应 DLL 引用
- **CogToolGroup 输出枚举**：CogToolGroup 没有 Outputs 集合，输出结果只能通过显示区记录查看，无法在面板中结构化展示
- **单图运行**：当前 `.idb` 多图文件只取第一张，不支持翻页

---

## 贡献

欢迎提交 Issue 和 Pull Request。

---

## License

[MIT](LICENSE)
