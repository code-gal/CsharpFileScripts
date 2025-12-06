# SmartClipboard v5 - 使用说明

## ✅ 已修复的问题

1. **STA 线程错误** - 剪贴板现在在正确的线程上访问
2. **控制台交互问题** - 从托盘菜单调用时会自动恢复控制台窗口
3. **托盘图标显示** - 程序正常运行在系统托盘

## 🚀 快速开始

### 运行程序

```bash
# 开发模式
cd SmartClipboard
dotnet run SmartClipboard_v5.cs

# 编译后运行
dotnet build SmartClipboard_v5.cs -o bin
.\bin\SmartClipboard_v5.exe
```

### 首次使用

1. 运行后程序会最小化到系统托盘
2. 在任务栏右下角找到托盘图标
3. 右键图标 → 设置 → 配置 AI/Matrix 等功能

## 📋 功能说明

### 系统托盘菜单

- **📊 状态显示** - 显示已捕获的剪贴板数量
- **💻 打开控制台** - 显示控制台窗口
- **📜 查看历史** - 查看最近 50 条记录
- **⚙️ 设置** - 交互式配置向导
- **⏸️ 暂停/继续监听** - 临时停止剪贴板监控
- **📝 查看日志** - 打开日志文件
- **🚀 开机自启** - 切换开机自启动
- **❌ 退出** - 关闭程序

### 命令行模式

```bash
# 查看帮助
dotnet run SmartClipboard_v5.cs -- help

# 配置管理
dotnet run SmartClipboard_v5.cs -- config

# 查看历史
dotnet run SmartClipboard_v5.cs -- history

# 搜索内容
dotnet run SmartClipboard_v5.cs -- search "关键词"

# 清空历史
dotnet run SmartClipboard_v5.cs -- clear

# 开机自启
dotnet run SmartClipboard_v5.cs -- autostart
```

## 🔧 配置说明

### AI 智能分析

配置后可以自动分类和生成摘要：

- **OpenAI**: gpt-4o-mini（需要 API Key）
- **DeepSeek**: deepseek-chat（需要 API Key）
- **Ollama**: 本地运行（免费，需先安装 Ollama）
- **自定义**: 兼容 OpenAI API 的其他服务

### Matrix 同步

将剪贴板内容同步到 Matrix 房间：

1. 在 Element 中创建房间
2. 获取 Access Token（设置 → 帮助与关于 → 高级）
3. 获取房间 ID（房间设置 → 高级）
4. 在配置中填入这些信息

### 敏感信息过滤

自动过滤包含以下模式的内容：
- 密码（password:、password=）
- 私钥（BEGIN RSA/DSA/EC PRIVATE KEY）
- API Keys（sk-、ghp-）

## 📁 数据位置

- **数据库**: `%APPDATA%\SmartClipboard\data.db`
- **日志**: `%APPDATA%\SmartClipboard\app.log`

所有配置和历史记录都存储在 SQLite 数据库中。

## ⚡ 性能

- **CPU 占用**: 接近 0%（事件驱动）
- **内存占用**: ~30-50MB
- **磁盘占用**: 取决于历史记录数量

## 🐛 故障排查

### 剪贴板不监控

1. 检查是否暂停（托盘菜单查看）
2. 查看日志文件了解错误信息
3. 确保没有其他程序占用剪贴板

### 托盘菜单无响应

1. 从托盘菜单选择"打开控制台"
2. 确保控制台窗口完全显示后再操作
3. 如果失败，重启程序

### 配置无法保存

1. 检查数据库文件权限
2. 查看日志中的错误信息
3. 尝试删除数据库文件重新开始

## 📝 开发说明

### 技术栈

- C# 14（基于文件的应用）
- .NET 10
- Spectre.Console（TUI）
- SQLite（数据存储）
- WinForms（系统托盘）

### 关键设计

1. **STA 线程**: 主程序在 STA 线程中运行，确保剪贴板和 WinForms 正常工作
2. **事件驱动**: 使用 Windows API `AddClipboardFormatListener` 零 CPU 监控
3. **单实例**: 使用 Mutex 防止重复运行
4. **异步处理**: AI 分析和 Matrix 同步不阻塞主线程

## 📄 许可证

MIT License

---

**使用愉快！** 🎉
