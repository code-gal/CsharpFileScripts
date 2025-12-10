#!/usr/bin/env dotnet

#:sdk Microsoft.NET.Sdk.Web

using Microsoft.Extensions.FileProviders;
using System.Net;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 配置 JSON 序列化器 (支持 AOT 和源生成)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// 配置 Kestrel 明确使用 IPv4
builder.WebHost.ConfigureKestrel(options =>
{
    var port = builder.Configuration.GetValue<int>("Port", 5000);
    var host = builder.Configuration.GetValue<string>("Host", "localhost");
    
    // 仅监听 IPv4
    options.Listen(IPAddress.Loopback, port); // 127.0.0.1 (本地访问)
    options.Listen(IPAddress.Any, port);      // 0.0.0.0 (网络访问)
});

var app = builder.Build();

// Web 根目录
var wwwPath = Path.Combine(Directory.GetCurrentDirectory(), "www");
if (!Directory.Exists(wwwPath))
{
    Directory.CreateDirectory(wwwPath);
    Console.WriteLine($"⚠️  已创建 www 文件夹: {wwwPath}");
}

Console.WriteLine($"📁 Web 根目录: {wwwPath}");

// 配置静态文件服务
var fileProvider = new PhysicalFileProvider(wwwPath);
var staticFileOptions = new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = "",
    ServeUnknownFileTypes = false
};

app.UseStaticFiles(staticFileOptions);

// 配置目录浏览
var directoryBrowserOptions = new DirectoryBrowserOptions
{
    FileProvider = fileProvider,
    RequestPath = "/browse"
};
app.UseDirectoryBrowser(directoryBrowserOptions);

// 路由
app.UseRouting();

// 健康检查端点
app.MapGet("/health", () => new HealthResponse(
    "健康",
    "WebDemo 正在运行",
    DateTime.Now
));

// 默认文档和 404 处理中间件
app.Use(async (context, next) =>
{
    await next();
    
    // 只处理 404 且尚未开始写入响应的情况
    if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
    {
        var path = context.Request.Path.Value?.TrimStart('/') ?? "";
        
        // 如果是根路径或目录，尝试查找默认文档
        if (string.IsNullOrEmpty(path) || path.EndsWith('/'))
        {
            var defaultFiles = new[] { "index.html", "index.htm", "default.html" };
            foreach (var defaultFile in defaultFiles)
            {
                var filePath = Path.Combine(wwwPath, path, defaultFile);
                if (File.Exists(filePath))
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.SendFileAsync(filePath);
                    return;
                }
            }
        }
        
        // 返回 JSON 格式的 404 页面
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(
            new ErrorResponse(404, "页面未找到", context.Request.Path),
            AppJsonSerializerContext.Default.ErrorResponse
        );
    }
});

// 启动信息
var port = builder.Configuration.GetValue<int>("Port", 5000);
var hostname = Dns.GetHostName();

Console.WriteLine("============================================================");
Console.WriteLine("🚀 WebDemo Web 服务器已启动!");
Console.WriteLine($"📍 本地访问: http://localhost:{port}");
Console.WriteLine($"🌐 网络访问: http://{hostname}:{port}");
Console.WriteLine($"📂 浏览文件: http://localhost:{port}/browse");
Console.WriteLine($"💚 健康检查: http://localhost:{port}/health");
Console.WriteLine("============================================================");
Console.WriteLine("按 Ctrl+C 停止服务器");

app.Run();

// JSON 序列化上下文 (用于 AOT 和性能优化)
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(ErrorResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}

// 响应模型
record HealthResponse(string Status, string Message, DateTime Timestamp);
record ErrorResponse(int Code, string Message, string Path);
