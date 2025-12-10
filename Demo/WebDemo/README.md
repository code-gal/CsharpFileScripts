# WebDemo Web Server

一个基于单文件 C# 的轻量级 Web 服务器,用于托管静态 HTML 网站。

## ✨ 特性

- 📁 **静态文件托管**: 支持 HTML, CSS, JavaScript, 图片等所有静态资源
- 🏠 **自动首页**: 自动识别 `index.html`, `index.htm`, `default.html`
- 📂 **目录浏览**: 访问 `/browse` 可浏览 www 文件夹内容
- 💚 **健康检查**: `/health` 端点提供服务器状态信息
- 🎨 **友好界面**: 美观的欢迎页面和 404 错误页面
- 🚀 **单文件程序**: 无需项目文件,一个 .cs 文件即可运行

## 📋 前置要求

- .NET 9.0 SDK 或更高版本
- Windows / Linux / macOS

## 🚀 快速开始

### 方法 1: 直接运行

```bash
dotnet run WebDemo.cs
```

服务器将在 `http://localhost:5000` 启动。

### 方法 2: 发布为可执行文件

```bash
# 发布
dotnet publish WebDemo.cs -o ./publish

# 运行
cd publish
./WebDemo
```

### 方法 3: 发布为单个可执行文件

```bash
# Windows
dotnet publish WebDemo.cs -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish

# Linux
dotnet publish WebDemo.cs -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o ./publish

# macOS
dotnet publish WebDemo.cs -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true -o ./publish
```

## 📁 文件结构

```
WebDemo/
├── WebDemo.cs          # Web 服务器主程序
├── www/                  # Web 根目录
│   ├── index.html       # 默认首页
│   ├── css/             # CSS 样式文件
│   ├── js/              # JavaScript 文件
│   └── images/          # 图片资源
└── README.md            # 本文件
```

## 🌐 访问地址

- **主页**: http://localhost:5000
- **目录浏览**: http://localhost:5000/browse
- **健康检查**: http://localhost:5000/health

## 🔧 配置

### 修改端口

编辑 `WebDemo.cs` 文件,找到以下代码:

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // 修改这里的端口号
});
```

### 添加 HTTPS 支持

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // HTTP
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});
```

### 禁用目录浏览

注释掉以下代码:

```csharp
// app.UseDirectoryBrowser(new DirectoryBrowserOptions
// {
//     FileProvider = new PhysicalFileProvider(wwwPath),
//     RequestPath = "/browse"
// });
```

## 📝 使用示例

### 1. 部署静态网站

将你的 HTML、CSS、JS 文件放入 `www` 文件夹:

```
www/
├── index.html
├── about.html
├── css/
│   └── style.css
├── js/
│   └── app.js
└── images/
    └── logo.png
```

### 2. 单页应用 (SPA)

对于 React、Vue 等单页应用,修改 404 处理逻辑,重定向到 `index.html`:

```csharp
app.Use(async (context, next) =>
{
    await next();
    
    if (context.Response.StatusCode == 404 && !Path.HasExtension(context.Request.Path.Value))
    {
        context.Request.Path = "/index.html";
        await next();
    }
});
```

### 3. API 代理

添加反向代理中间件转发 API 请求:

```csharp
#:package Yarp.ReverseProxy@2.0.0

app.MapReverseProxy();
```

## 🛠️ 开发工具

### VS Code 配置

1. 安装扩展:
   - C# Dev Kit
   - C#

2. 启用预览功能:
   - 设置 → 搜索 "Dotnet Projects Enable File Based Programs"
   - 勾选启用

3. 调试配置 (`.vscode/launch.json`):

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch WebDemo",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "dotnet",
            "args": ["run", "WebDemo.cs"],
            "cwd": "${workspaceFolder}/Userful/WebDemo",
            "stopAtEntry": false,
            "serverReadyAction": {
                "action": "openExternally",
                "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
            }
        }
    ]
}
```

## 📊 性能优化

### 启用响应压缩

```csharp
#:package Microsoft.AspNetCore.ResponseCompression

builder.Services.AddResponseCompression();
app.UseResponseCompression();
```

### 启用响应缓存

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600");
    }
});
```

## 🐛 故障排除

### 端口被占用

```
Error: Failed to bind to address http://0.0.0.0:5000
```

**解决方案**: 修改端口或终止占用进程:

```bash
# Windows
netstat -ano | findstr :5000
taskkill /PID <进程ID> /F

# Linux/macOS
lsof -i :5000
kill -9 <进程ID>
```

### www 文件夹找不到

程序会自动在以下位置查找:
1. 当前工作目录下的 `www` 文件夹
2. 程序所在目录的 `www` 文件夹

如果都不存在,会自动创建。

## 📄 许可证

MIT License - 自由使用和修改

## 🤝 贡献

欢迎提交 Issue 和 Pull Request!

## 📚 相关资源

- [.NET 文档](https://docs.microsoft.com/dotnet/)
- [ASP.NET Core 文档](https://docs.microsoft.com/aspnet/core/)
- [基于文件的 C# 程序](https://learn.microsoft.com/dotnet/csharp/fundamentals/program-structure/file-based-apps)

---

**Enjoy coding! 🎉**
