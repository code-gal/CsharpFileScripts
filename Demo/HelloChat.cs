#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk.Web
#:package QRCoder@1.6.0

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.SignalR;
using QRCoder;

// --- 1. 初始化 Web 应用构建器 ---
var builder = WebApplication.CreateBuilder(args);

// 添加 SignalR 服务 (用于实时通信)
builder.Services.AddSignalR();

var app = builder.Build();

// --- 2. 配置路由和功能 ---

// 映射 SignalR Hub
app.MapHub<CollaborationHub>("/hub");

// 首页路由：返回包含前端代码的 HTML
app.MapGet("/", async (HttpContext context) =>
{
    // 获取本机局域网 IP
    var localIp = GetLocalIpAddress();
    var port = app.Urls.FirstOrDefault()?.Split(':').Last() ?? "5000"; // 默认端口
    var url = $"http://{localIp}:{port}";

    // 生成二维码 (SVG格式，跨平台兼容性好)
    var qrGenerator = new QRCodeGenerator();
    var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
    var qrCode = new SvgQRCode(qrCodeData);
    var qrSvg = qrCode.GetGraphic(20, "#ffffff", "#00000000"); // 白色前景，透明背景

    // 返回完整的 HTML 页面
    var html = GetHtmlContent(url, qrSvg);
    await context.Response.WriteAsync(html);
});

// --- 4. 辅助类与方法 ---

// 获取本机局域网 IP 地址
string GetLocalIpAddress()
{
    var host = Dns.GetHostEntry(Dns.GetHostName());
    foreach (var ip in host.AddressList)
    {
        if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
        {
            return ip.ToString();
        }
    }
    return "localhost";
}

// 前端 HTML/CSS/JS 代码模板
// 使用 $$""" ... """ 原始字符串字面量，避免与 JS/CSS 的花括号冲突
// C# 插值需要使用双花括号 {{variable}}
string GetHtmlContent(string serverUrl, string qrSvg) => $$"""
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no">
    <title>.NET 10 单文件协作演示</title>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js"></script>
    <script src="https://cdn.tailwindcss.com"></script>
    <style>
        body { background: #0f172a; color: #e2e8f0; font-family: 'Segoe UI', sans-serif; overflow: hidden; touch-action: none; }
        .glass { background: rgba(30, 41, 59, 0.7); backdrop-filter: blur(10px); border: 1px solid rgba(255, 255, 255, 0.1); border-radius: 16px; }
        #canvas-container { cursor: crosshair; }
        /* 滚动条样式 */
        ::-webkit-scrollbar { width: 6px; }
        ::-webkit-scrollbar-track { background: transparent; }
        ::-webkit-scrollbar-thumb { background: #475569; border-radius: 3px; }
    </style>
</head>
<body class="h-screen w-screen flex flex-col md:flex-row p-4 gap-4">
    
    <!-- 左侧：控制面板与聊天 -->
    <div class="glass w-full md:w-1/3 flex flex-col p-4 h-1/3 md:h-full shadow-2xl z-10">
        <div class="mb-4 flex items-center justify-between">
            <div>
                <h1 class="text-xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-blue-400 to-purple-500">.NET 10 File App</h1>
                <p class="text-xs text-slate-400">单文件 C# 驱动</p>
            </div>
            <div class="w-12 h-12 bg-white p-1 rounded-lg shadow-lg">
                {{qrSvg}}
            </div>
        </div>
        
        <div class="flex-1 overflow-y-auto mb-4 space-y-2" id="messagesList">
            <div class="text-xs text-slate-500 text-center">--- 聊天记录 ---</div>
        </div>

        <div class="flex gap-2">
            <input type="text" id="userInput" placeholder="昵称" class="w-1/4 bg-slate-800 border border-slate-600 rounded px-2 py-1 text-sm focus:outline-none focus:border-blue-500">
            <input type="text" id="messageInput" placeholder="输入消息..." class="flex-1 bg-slate-800 border border-slate-600 rounded px-2 py-1 text-sm focus:outline-none focus:border-blue-500">
            <button id="sendButton" class="bg-blue-600 hover:bg-blue-700 text-white px-3 py-1 rounded text-sm transition">发送</button>
        </div>
        <div class="mt-2 text-xs text-slate-500 text-center">
            手机扫码: <span class="text-blue-400">{{serverUrl}}</span>
        </div>
    </div>

    <!-- 右侧：绘图白板 -->
    <div class="glass flex-1 relative overflow-hidden shadow-2xl" id="canvas-container">
        <div class="absolute top-4 left-4 text-slate-400 text-sm pointer-events-none select-none">
            <span class="bg-slate-800/50 px-2 py-1 rounded">实时白板区域</span>
        </div>
        <canvas id="whiteboard" class="w-full h-full block"></canvas>
        
        <!-- 颜色选择器 -->
        <div class="absolute bottom-4 left-1/2 transform -translate-x-1/2 flex gap-2 bg-slate-800/80 p-2 rounded-full">
            <button class="w-6 h-6 rounded-full bg-white border-2 border-transparent hover:scale-110 transition" onclick="setColor('#ffffff')"></button>
            <button class="w-6 h-6 rounded-full bg-red-500 border-2 border-transparent hover:scale-110 transition" onclick="setColor('#ef4444')"></button>
            <button class="w-6 h-6 rounded-full bg-green-500 border-2 border-transparent hover:scale-110 transition" onclick="setColor('#22c55e')"></button>
            <button class="w-6 h-6 rounded-full bg-blue-500 border-2 border-transparent hover:scale-110 transition" onclick="setColor('#3b82f6')"></button>
            <button class="w-6 h-6 rounded-full bg-yellow-500 border-2 border-transparent hover:scale-110 transition" onclick="setColor('#eab308')"></button>
        </div>
    </div>

    <script>
        // --- SignalR 连接逻辑 ---
        const connection = new signalR.HubConnectionBuilder().withUrl("/hub").build();

        // 接收消息
        connection.on("ReceiveMessage", function (user, message) {
            const div = document.createElement("div");
            div.className = "bg-slate-800/50 p-2 rounded text-sm animate-fade-in";
            div.innerHTML = `<span class="font-bold text-blue-400">${user}:</span> ${message}`;
            const list = document.getElementById("messagesList");
            list.appendChild(div);
            list.scrollTop = list.scrollHeight;
        });

        // 接收绘图
        connection.on("ReceiveDraw", function (x, y, color, isDrawing) {
            drawRemote(x, y, color, isDrawing);
        });

        connection.start().catch(err => console.error(err.toString()));

        // --- 聊天逻辑 ---
        document.getElementById("sendButton").addEventListener("click", function (event) {
            const user = document.getElementById("userInput").value || "匿名";
            const message = document.getElementById("messageInput").value;
            if(message) {
                connection.invoke("SendMessage", user, message).catch(err => console.error(err.toString()));
                document.getElementById("messageInput").value = '';
            }
            event.preventDefault();
        });

        // --- 白板绘图逻辑 ---
        const canvas = document.getElementById('whiteboard');
        const ctx = canvas.getContext('2d');
        let drawing = false;
        let currentColor = '#ffffff';
        let lastX = 0;
        let lastY = 0;

        function resizeCanvas() {
            canvas.width = canvas.parentElement.clientWidth;
            canvas.height = canvas.parentElement.clientHeight;
        }
        window.addEventListener('resize', resizeCanvas);
        resizeCanvas();

        function setColor(color) { currentColor = color; }

        function getPos(e) {
            const rect = canvas.getBoundingClientRect();
            const clientX = e.touches ? e.touches[0].clientX : e.clientX;
            const clientY = e.touches ? e.touches[0].clientY : e.clientY;
            return {
                x: clientX - rect.left,
                y: clientY - rect.top
            };
        }

        function startDraw(e) {
            drawing = true;
            const pos = getPos(e);
            lastX = pos.x;
            lastY = pos.y;
            // 通知服务器开始
            connection.invoke("Draw", lastX, lastY, currentColor, false).catch(err => console.error(err));
        }

        function moveDraw(e) {
            if (!drawing) return;
            const pos = getPos(e);
            
            ctx.beginPath();
            ctx.moveTo(lastX, lastY);
            ctx.lineTo(pos.x, pos.y);
            ctx.strokeStyle = currentColor;
            ctx.lineWidth = 3;
            ctx.lineCap = 'round';
            ctx.stroke();

            // 发送绘图数据
            connection.invoke("Draw", pos.x, pos.y, currentColor, true).catch(err => console.error(err));

            lastX = pos.x;
            lastY = pos.y;
            e.preventDefault(); // 防止触摸滚动
        }

        function endDraw() {
            drawing = false;
        }

        // 远程绘图处理
        let remoteLastX = 0;
        let remoteLastY = 0;
        function drawRemote(x, y, color, isDrawing) {
            if (!isDrawing) {
                remoteLastX = x;
                remoteLastY = y;
                return;
            }
            ctx.beginPath();
            ctx.moveTo(remoteLastX, remoteLastY);
            ctx.lineTo(x, y);
            ctx.strokeStyle = color;
            ctx.lineWidth = 3;
            ctx.lineCap = 'round';
            ctx.stroke();
            remoteLastX = x;
            remoteLastY = y;
        }

        canvas.addEventListener('mousedown', startDraw);
        canvas.addEventListener('mousemove', moveDraw);
        canvas.addEventListener('mouseup', endDraw);
        canvas.addEventListener('mouseout', endDraw);

        canvas.addEventListener('touchstart', startDraw);
        canvas.addEventListener('touchmove', moveDraw);
        canvas.addEventListener('touchend', endDraw);
    </script>
</body>
</html>
""";

// --- 3. 启动应用 ---
// 自动监听所有网络接口，以便局域网访问
app.Urls.Add("http://0.0.0.0:5000");
Console.WriteLine($"应用已启动! 请在浏览器访问: http://localhost:5000");
app.Run();

// SignalR Hub: 处理实时消息和绘图数据
class CollaborationHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task Draw(int x, int y, string color, bool isDrawing)
    {
        // 广播绘图坐标给其他人 (除了发送者自己，减少延迟感)
        await Clients.Others.SendAsync("ReceiveDraw", x, y, color, isDrawing);
    }
}
