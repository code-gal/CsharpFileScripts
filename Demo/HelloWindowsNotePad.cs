#!/usr/bin/env dotnet
#:property TargetFramework=net10.0-windows
#:property UseWindowsForms=true
#:property PublishTrimmed=false
#:package Spectre.Console@0.49.1

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Spectre.Console;
using SpectreColor = Spectre.Console.Color;

// ===========================================
// HelloRobot.cs - 桌面自动化 RPA 机器人演示
// ===========================================
// 功能：
// 1. 在桌面创建工作文件夹和测试文件
// 2. 启动记事本应用
// 3. 模拟键盘输入，将数据"幽灵打字"到记事本
// 4. 控制鼠标绘制矩形轨迹
// 5. 实时显示自动化进度
// ===========================================

// Win32 常量定义
const int SM_CXSCREEN = 0;
const int SM_CYSCREEN = 1;

Console.OutputEncoding = Encoding.UTF8;

AnsiConsole.Write(
    new FigletText("RPA Robot")
        .LeftJustified()
        .Color(SpectreColor.Cyan1));

AnsiConsole.MarkupLine("[yellow]⚠️  警告: 程序运行时请勿移动鼠标或点击其他窗口[/]\n");

await AnsiConsole.Status()
    .StartAsync("[green]正在初始化自动化任务...[/]", async ctx =>
    {
        // ========== 步骤 1: 准备工作区 ==========
        ctx.Status("[cyan1]📁 创建工作区文件夹...[/]");
        await Task.Delay(800);
        
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var workspacePath = Path.Combine(desktopPath, "AutoWorkspace");
        
        if (Directory.Exists(workspacePath))
        {
            Directory.Delete(workspacePath, true);
        }
        Directory.CreateDirectory(workspacePath);
        
        AnsiConsole.MarkupLine("[green]✓[/] 已创建: [blue]{0}[/]", workspacePath);

        // 生成任务内容并创建空文件
        ctx.Status("[cyan1]📝 创建空白任务文件...[/]");
        await Task.Delay(600);
        
        var taskFilePath = Path.Combine(workspacePath, "mission.txt");
        var taskContent = GenerateMissionContent();
        
        // 创建空文件
        await File.WriteAllTextAsync(taskFilePath, "");
        
        AnsiConsole.MarkupLine("[green]✓[/] 已创建空白文件: [blue]{0}[/]", taskFilePath);

        // ========== 步骤 2: 用记事本打开该文件 ==========
        ctx.Status("[cyan1]🚀 用记事本打开文件...[/]");
        await Task.Delay(1000);
        
        var notepadProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "notepad.exe",
            Arguments = $"\"{taskFilePath}\"",
            UseShellExecute = true
        });

        if (notepadProcess == null)
        {
            AnsiConsole.MarkupLine("[red]✗ 无法启动记事本[/]");
            return;
        }

        // 等待记事本窗口完全加载
        await Task.Delay(2000);
        
        AnsiConsole.MarkupLine("[green]✓[/] 记事本已打开文件 (PID: [yellow]{0}[/])", notepadProcess.Id);

        // ========== 步骤 3: 幽灵打字机 ==========
        ctx.Status("[cyan1]⌨️  正在注入数据 (幽灵打字模式)...[/]");
        AnsiConsole.MarkupLine("[yellow]👀 请观察记事本窗口...[/]");
        
        await Task.Delay(1500); // 给用户时间切换视角
        await TypewriterEffect(taskContent);
        
        AnsiConsole.MarkupLine("[green]✓[/] 数据注入完成");

        // ========== 步骤 4: 保存文件 ==========
        ctx.Status("[cyan1]💾 自动保存文件...[/]");
        await Task.Delay(500);
        
        // 模拟 Ctrl+S 保存（文件已存在，直接保存）
        SendKeys.SendWait("^s"); // Ctrl+S
        await Task.Delay(1000);
        
        AnsiConsole.MarkupLine("[green]✓[/] 文件已保存");

        // ========== 步骤 5: 鼠标控制演示 ==========
        ctx.Status("[cyan1]🖱️  演示鼠标控制 (绘制矩形)...[/]");
        await Task.Delay(1000);
        
        DrawRectangleWithMouse();
        
        AnsiConsole.MarkupLine("[green]✓[/] 鼠标轨迹演示完成");
    });

AnsiConsole.MarkupLine("\n[green bold]🎉 所有自动化任务执行完毕！[/]");
AnsiConsole.MarkupLine("[dim]检查您的桌面 'AutoWorkspace' 文件夹查看生成的文件[/]");

// ========================================
// 辅助函数
// ========================================

string GenerateMissionContent()
{
    var random = new Random();
    var agents = new[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo" };
    var targets = new[] { "数据中心", "卫星基站", "研究实验室", "情报站点" };
    
    var sb = new StringBuilder();
    sb.AppendLine("=== 机密任务简报 ===");
    sb.AppendLine($"日期: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine($"代号: OPERATION-{random.Next(1000, 9999)}");
    sb.AppendLine();
    sb.AppendLine($"指派特工: {agents[random.Next(agents.Length)]}");
    sb.AppendLine($"目标位置: {targets[random.Next(targets.Length)]}");
    sb.AppendLine();
    sb.AppendLine("任务内容:");
    sb.AppendLine("1. 渗透目标设施");
    sb.AppendLine("2. 获取核心数据");
    sb.AppendLine("3. 安全撤离并销毁痕迹");
    sb.AppendLine();
    sb.AppendLine("--- 此消息由 RPA Robot 自动生成 ---");
    
    return sb.ToString();
}

async Task TypewriterEffect(string text)
{
    foreach (var c in text)
    {
        if (c == '\r') continue; // 跳过回车符
        
        if (c == '\n')
        {
            // 换行需要发送 Enter 键
            SendKeys.SendWait("{ENTER}");
            await Task.Delay(50);
        }
        else if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || c == ' ')
        {
            // 发送普通字符
            var key = c.ToString();
            if (c == '+' || c == '^' || c == '%' || c == '~' || c == '(' || c == ')' || c == '{' || c == '}' || c == '[' || c == ']')
            {
                // 特殊字符需要转义
                key = "{" + c + "}";
            }
            SendKeys.SendWait(key);
            await Task.Delay(Random.Shared.Next(30, 80)); // 模拟人类打字速度
        }
        else
        {
            // 其他字符直接发送
            SendKeys.SendWait(c.ToString());
            await Task.Delay(50);
        }
    }
}

void DrawRectangleWithMouse()
{
    // 获取屏幕中心位置
    var screenWidth = GetSystemMetrics(SM_CXSCREEN);
    var screenHeight = GetSystemMetrics(SM_CYSCREEN);
    
    var centerX = screenWidth / 2;
    var centerY = screenHeight / 2;
    var rectSize = 200;
    
    // 定义矩形四个角
    var points = new[]
    {
        (centerX - rectSize, centerY - rectSize), // 左上
        (centerX + rectSize, centerY - rectSize), // 右上
        (centerX + rectSize, centerY + rectSize), // 右下
        (centerX - rectSize, centerY + rectSize), // 左下
        (centerX - rectSize, centerY - rectSize)  // 回到左上（闭合）
    };
    
    // 移动鼠标绘制矩形
    foreach (var (x, y) in points)
    {
        SetCursorPos(x, y);
        Thread.Sleep(300); // 停顿以便观察
    }
    
    // 回到中心
    SetCursorPos(centerX, centerY);
}

// ========================================
// Win32 API 声明
// ========================================

[DllImport("user32.dll")]
static extern bool SetCursorPos(int X, int Y);

[DllImport("user32.dll")]
static extern int GetSystemMetrics(int nIndex);
