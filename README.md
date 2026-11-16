# VisionMaster 二次开发上位机（WinForms）

基于 VisionMaster 4.4 SDK 的 WinForms 上位机，演示如何用 C# 代码操作 VM 的方案、流程、模块以及全局模块。

## 环境要求

- Windows 10 / 11 x64
- 已安装 VisionMaster 4.4（SDK 相关 DLL 需已注册）
- Visual Studio 2019 及以上，安装 .NET Framework 4.7.2 开发工具
- 项目 Debug 平台为 x64

> 没装 VisionMaster 时，`VM.Core`、`VM.PlatformSDKCS`、各模块 DLL 的引用会找不到，项目无法编译。

## 功能

### 方案

- 选择方案路径、加载（支持方案密码、静默执行）
- 保存、另存为
- 执行方案、查看方案状态
- 关闭方案、释放方案

### 流程

- 流程列表与选择
- 单次执行、连续执行（可设间隔 ms）、停止
- 启用流程
- 导出 / 导入流程文件（`.prc`）
- 删除流程

### 模块与参数

- 流程模块列表浏览
- 模块参数名列表、参数值读取 / 写入
- 流程输入参数读取 / 写入
- 流程输出结果读取（INT / FLOAT / STRING / IMAGE）

### 全局模块

| 模块 | 已实现功能 |
| :--- | :--- |
| 全局变量 | 列出变量名，读取 / 写入变量值 |
| 数据队列 | 按索引读取字符串数据 |
| 通信管理 | 显示接收数据、回车发送、查询设备连接状态 |
| 全局相机 | 相机列表、相机名称 / SN / 连接状态 |
| 全局光源 | 控制器类型、通道列表（仅填充界面，暂不下发） |
| 全局触发 | 列出方案中的全局触发模块名 |
| 全局脚本 | 显示连续运行状态 |

## 编译

用 Visual Studio 打开 `VM二开.slnx` 直接生成。

命令行编译（按本机 VS 安装路径调整）：

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
  "VM二开.csproj" /t:Build /p:Configuration=Debug /p:Platform=AnyCPU
```

生成结果在 `bin\Debug\VM二开.exe`。

## 目录说明

| 文件 | 说明 |
| :--- | :--- |
| `Form1.cs` | 主逻辑：方案 / 流程 / 模块 / 全局模块操作 |
| `Form1.Designer.cs` | 界面布局与事件绑定 |
| `VM二开.csproj` | 项目文件与 VisionMaster 程序集引用 |
| `App.config` / `App.txt` | 运行配置 |

## 说明

- `bin`、`obj`、`.vs` 已加入 `.gitignore`
- 全局模块默认取方案里第一个同类模块
- 通信地址默认 `-1`，设备 ID 默认 `0`
- 数据队列、光源等操作需配合方案或流程执行才会生效