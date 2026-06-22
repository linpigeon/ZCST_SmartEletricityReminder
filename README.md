# ZCST_SmartEletricityReminder

![应用截图](pic/屏幕截图%202026-05-27%20204521.png)

珠海科技学院宿舍电费查询与推送系统，基于"完美校园"（17wanxiao）API，支持邮箱、噔噔和 MQTT 三种推送通知方式。

## 功能

- 查询绑定宿舍的电费余额和用量
- 余额低于阈值时自动发送提醒（邮件 / 噔噔推送 / MQTT）
- 定时循环查询与推送
- 桌面 GUI（Avalonia），Fluent Design 风格，支持浅色/深色主题切换
- 侧边栏导航（支持折叠动画）
- 系统托盘驻留后台运行

## 技术栈

- .NET 10.0
- Avalonia 12.0（跨平台桌面 UI）
- CommunityToolkit.Mvvm 8.4（MVVM 工具包）
- System.Net.Mail（SMTP 邮件发送）
- MQTTnet 5.0（MQTT 协议推送）

## 技术架构

```
┌──────────────────────────────────────────────────────────┐
│                      Program.cs                          │
│              GUI 模式  │  --auto CLI 模式                 │
└─────────┬──────────────┴───────────┬────────────────────┘
          │                          │
    ┌─────▼──────┐            ┌──────▼─────────┐
    │    App     │            │  定时循环调度    │
    │ (Avalonia) │            │ while + delay   │
    └─────┬──────┘            └──────┬─────────┘
          │                          │
    ┌─────▼──────────────────────────▼─────────┐
    │              ViewModels                   │
    │  ┌──────────────┐  ┌──────────────────┐   │
    │  │MainWindowVM  │  │ PushSettingsVM   │   │
    │  │ 导航·主题     │  │ 配置·自动推送     │   │
    │  └──────────────┘  └──────────────────┘   │
    │  ┌──────────────┐                         │
    │  │ QueryVM      │                         │
    │  │ 查询·展示     │                         │
    │  └──────┬───────┘                         │
    └─────────┼─────────────────────────────────┘
              │
    ┌─────────▼─────────────────────────────────┐
    │               Services                     │
    │  ┌──────────────────────────────────────┐  │
    │  │  PerfectCampusApiClient               │  │
    │  │  POST → 17wanxiao API                │  │
    │  └──────────────┬───────────────────────┘  │
    │                 │                          │
    │  ┌──────────────▼───────────────────────┐  │
    │  │  JsonResponseParser                  │  │
    │  │  解析嵌套 JSON → List<RoomInfo>       │  │
    │  └──────────────┬───────────────────────┘  │
    │                 │                          │
    │     ┌───────────┼───────────┐              │
    │     │           │           │              │
    │  ┌──▼──────┐ ┌──▼──────┐ ┌──▼────────┐    │
    │  │ Email   │ │DengDeng │ │  Mqtt     │    │
    │  │ Service │ │PushSvc  │ │ Service   │    │
    │  │ SMTP    │ │HTTP GET │ │ MQTTnet   │    │
    │  └─────────┘ └─────────┘ └───────────┘    │
    │                                             │
    │  ┌──────────────────────────────────────┐   │
    │  │  SettingsService                     │   │
    │  │  appsettings.json 读写                │   │
    │  └──────────────────────────────────────┘   │
    └─────────────────────────────────────────────┘
```

### 架构说明

**MVVM 模式**：项目采用 Model-View-ViewModel 分层架构，View（`*.axaml`）负责 UI 布局，ViewModel 通过数据绑定驱动视图状态更新，Model 承载数据实体与配置。

**数据流**：

1. `QueryViewModel` 接收用户输入的学号，调用 `PerfectCampusApiClient.GetBoundRoomsAsync()` 向完美校园 API 发送 POST 请求
2. API 返回的 JSON 经 `JsonResponseParser` 解析为 `List<RoomInfo>`，包含房间名称、余额、用量、在线状态
3. ViewModel 将结果绑定到 View 展示；如需推送，调用 `EmailService` / `DengDengPushService` / `MqttService` 发送通知
4. 当任一房间余额低于 `LowBalanceThreshold` 时，推送内容会包含低余额警告标记

**双模式运行**：

| 模式 | 入口 | 说明 |
|------|------|------|
| GUI | `Program.cs` → `App.axaml.cs` | 完整桌面界面，侧边栏切换查询/设置页面，系统托盘后台驻留 |
| `--auto` | `Program.cs` 内循环 | 无 UI，按 `IntervalMinutes` 定时轮询并推送，适合服务器部署 |

**主题系统**：`SettingsService` 持久化 `Theme` 字段（`Light` / `Dark`），`App.axaml.cs` 在启动时读取并加载对应 XAML 资源词典。

## 项目结构

```
├── App.axaml / App.axaml.cs          # 应用入口与主题加载
├── MainWindow.axaml / .cs            # 主窗口（导航、侧边栏、托盘）
├── Program.cs                        # 启动入口（GUI / --auto 模式）
├── appsettings.json                  # 配置文件
├── Models/
│   ├── EmailSettings.cs              # SMTP / 查询 / 噔噔 / MQTT 配置模型
│   └── RoomInfo.cs                   # 房间数据模型
├── Converters.cs                     # 通用值转换器（BoolToDouble 等）
├── Services/
│   ├── PerfectCampusApiClient.cs     # 完美校园 API 客户端
│   ├── JsonResponseParser.cs         # API 响应 JSON 解析
│   ├── EmailService.cs              # 邮件发送服务
│   ├── DengDengPushService.cs       # 噔噔推送服务
│   ├── MqttService.cs               # MQTT 推送服务
│   └── SettingsService.cs           # 配置文件读写
├── ViewModels/
│   ├── MainWindowViewModel.cs        # 主窗口 VM（导航、主题）
│   ├── QueryViewModel.cs            # 房间查询 VM
│   └── PushSettingsViewModel.cs     # 推送设置 VM
└── Views/
    ├── QueryView.axaml / .cs         # 查询页面（学号输入、房间详情）
    ├── PushSettingsView.axaml / .cs  # 推送配置页面
    └── CloseDialog.axaml / .cs       # 关闭确认对话框
```

## 配置

编辑 `appsettings.json`：

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.qq.com",
    "SmtpPort": 587,
    "SenderEmail": "your_email@qq.com",
    "SenderName": "水电查询助手",
    "AuthCode": "your_smtp_auth_code",
    "RecipientEmails": ["recipient@example.com"],
    "EnableSsl": true
  },
  "QuerySettings": {
    "Account": "你的学号",
    "IntervalMinutes": 60,
    "LowBalanceThreshold": 20.0
  },
  "DengDengSettings": {
    "BaseUrl": "https://your-dengdeng-server.com",
    "DeviceId": "your_device_id",
    "Enabled": false
  },
  "MqttSettings": {
    "BrokerAddress": "your-mqtt-broker-ip",
    "Port": 1883,
    "Username": "",
    "Password": "",
    "Topic": "electricity/reminder",
    "Enabled": false
  }
}
```

| 字段 | 说明 |
|------|------|
| `EmailSettings.AuthCode` | QQ 邮箱 SMTP 授权码（非密码） |
| `QuerySettings.Account` | 完美校园绑定的学号 |
| `QuerySettings.LowBalanceThreshold` | 低余额告警阈值（度） |
| `DengDengSettings.Enabled` | 是否启用噔噔推送 |
| `MqttSettings.BrokerAddress` | MQTT Broker 地址（支持 `mqtt://` / `tcp://` 前缀） |
| `MqttSettings.Enabled` | 是否启用 MQTT 推送 |

## 运行

项目提供两种发行版本：

| 版本 | 文件名 | 大小 | 需要 .NET 10 |
|------|--------|------|:---:|
| **Standard** | `Standard.zip` | ~12 MB | 是 |
| **Portable** | `Protable.zip` | ~42 MB | 否 |

### Standard（依赖框架）

适合已安装 .NET 10 运行时的用户，体积小。下载 `Standard.zip` 解压后：

**GUI 模式：**
```bash
SmartEletricityReminder.exe
```

**自动推送模式（无 GUI）：**
```bash
SmartEletricityReminder.exe --auto
```

### Portable（独立便携）

无需安装任何运行时，即拷即用。下载 `Protable.zip` 解压后直接双击 `SmartEletricityReminder.exe` 启动。

如需自动推送模式，在命令行执行：
```bash
SmartEletricityReminder.exe --auto
```

### 开发构建

```bash
dotnet run                  # GUI 模式
dotnet run -- --auto        # 自动推送模式
dotnet publish -c Release -o ./publish   # 构建发布
```

## 发行版本使用方法

1. 从 [Releases](../../releases) 下载 `Standard.zip` 或 `Protable.zip`
2. 解压后编辑 `appsettings.json`，填写学号、邮箱授权码等信息（参见[配置](#配置)）
3. 双击 `SmartEletricityReminder.exe` 启动桌面 GUI
4. （可选）创建快捷方式并加入开机自启，实现无人值守

## 许可证

本项目基于 [MIT License](LICENSE) 发布。
