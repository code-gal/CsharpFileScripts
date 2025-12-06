#!/usr/bin/env dotnet
#:package Spectre.Console@0.49.1

using Spectre.Console;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// 🎯 智能文件整理助手 - Smart File Organizer
// 一个美观、实用的跨平台文件管理工具
// 使用 AI 辅助思维，帮助普通用户轻松整理文件

// 欢迎界面
ShowWelcome();

// 主菜单循环
while (true)
{
    var choice = ShowMainMenu();
    
    switch (choice)
    {
        case "analyze":
            await AnalyzeDirectory();
            break;
        case "organize":
            await OrganizeFiles();
            break;
        case "search":
            await SearchFiles();
            break;
        case "clean":
            await CleanupFiles();
            break;
        case "exit":
            ShowGoodbye();
            return;
    }
    
    AnsiConsole.WriteLine();
    AnsiConsole.Markup("[dim]按任意键继续...[/]");
    Console.ReadKey(true);
    Console.Clear();
}

// === 显示欢迎界面 ===
void ShowWelcome()
{
    Console.Clear();
    
    var rule = new Rule("[bold cyan]🎯 智能文件整理助手[/]");
    rule.Justification = Justify.Center;
    AnsiConsole.Write(rule);
    
    AnsiConsole.WriteLine();
    
    var panel = new Panel(
        new Markup(
            "[yellow]✨ 让 AI 帮你轻松管理文件[/]\n\n" +
            "[dim]• 智能分析文件分布\n" +
            "• 一键整理到分类文件夹\n" +
            "• 快速搜索和查找\n" +
            "• 清理重复和临时文件[/]"
        ))
    {
        Header = new PanelHeader("[bold green]功能特性[/]"),
        Border = BoxBorder.Rounded,
        BorderStyle = new Style(Color.Green)
    };
    
    AnsiConsole.Write(panel);
    AnsiConsole.WriteLine();
}

// === 显示主菜单 ===
string ShowMainMenu()
{
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[bold yellow]🎯 请选择功能：[/]")
            .PageSize(10)
            .HighlightStyle(new Style(Color.Cyan1, decoration: Decoration.Bold))
            .AddChoices(new[] {
                "📊 analyze - 分析当前目录",
                "📁 organize - 智能整理文件",
                "🔍 search - 搜索文件",
                "🧹 clean - 清理临时文件",
                "🚪 exit - 退出程序"
            }));
    
    return choice.Split(' ')[1];
}

// === 分析目录 ===
async Task AnalyzeDirectory()
{
    var currentDir = Directory.GetCurrentDirectory();
    
    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("cyan bold"))
        .StartAsync("[yellow]正在分析目录...[/]", async ctx =>
        {
            await Task.Delay(800);
        });
    
    var files = Directory.GetFiles(currentDir, "*", SearchOption.TopDirectoryOnly);
    var dirs = Directory.GetDirectories(currentDir);
    
    // 显示基本信息
    var grid = new Grid();
    grid.AddColumn(new GridColumn().NoWrap().PadRight(4));
    grid.AddColumn();
    
    grid.AddRow("[cyan]📂 当前目录:[/]", $"[dim]{currentDir}[/]");
    grid.AddRow("[cyan]📄 文件数量:[/]", $"[green]{files.Length}[/] 个");
    grid.AddRow("[cyan]📁 文件夹数:[/]", $"[green]{dirs.Length}[/] 个");
    
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Panel(grid)
    {
        Header = new PanelHeader("[bold cyan]📊 目录概览[/]"),
        Border = BoxBorder.Rounded
    });
    
    if (files.Length == 0)
    {
        AnsiConsole.MarkupLine("\n[yellow]⚠️  当前目录没有文件[/]");
        return;
    }
    
    // 按扩展名分组
    var filesByExtension = files
        .GroupBy(f => Path.GetExtension(f).ToLower())
        .OrderByDescending(g => g.Count())
        .ToList();
    
    AnsiConsole.WriteLine();
    
    // 创建文件类型分布图
    var chart = new BarChart()
        .Width(60)
        .Label("[bold underline cyan]📈 文件类型分布[/]")
        .CenterLabel();
    
    foreach (var group in filesByExtension.Take(10))
    {
        var ext = string.IsNullOrEmpty(group.Key) ? "无扩展名" : group.Key;
        var color = GetColorForExtension(ext);
        chart.AddItem(ext, group.Count(), color);
    }
    
    AnsiConsole.Write(chart);
    
    // 显示大文件
    AnsiConsole.WriteLine();
    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Grey);
    
    table.AddColumn(new TableColumn("[cyan]📄 文件名[/]").LeftAligned());
    table.AddColumn(new TableColumn("[cyan]📏 大小[/]").RightAligned());
    table.AddColumn(new TableColumn("[cyan]📅 修改时间[/]").Centered());
    
    var largeFiles = files
        .Select(f => new FileInfo(f))
        .OrderByDescending(f => f.Length)
        .Take(5);
    
    foreach (var file in largeFiles)
    {
        var size = FormatFileSize(file.Length);
        var date = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
        var name = file.Name.Length > 40 
            ? file.Name.Substring(0, 37) + "..." 
            : file.Name;
        
        table.AddRow(
            $"[white]{name}[/]",
            $"[yellow]{size}[/]",
            $"[dim]{date}[/]"
        );
    }
    
    AnsiConsole.Write(new Panel(table)
    {
        Header = new PanelHeader("[bold yellow]📦 最大的 5 个文件[/]")
    });
}

// === 整理文件 ===
async Task OrganizeFiles()
{
    var currentDir = Directory.GetCurrentDirectory();
    var files = Directory.GetFiles(currentDir, "*", SearchOption.TopDirectoryOnly);
    
    if (files.Length == 0)
    {
        AnsiConsole.MarkupLine("[yellow]⚠️  当前目录没有文件需要整理[/]");
        return;
    }
    
    AnsiConsole.MarkupLine($"\n[cyan]找到 [bold]{files.Length}[/] 个文件待整理[/]\n");
    
    var categories = new Dictionary<string, List<string>>
    {
        { "📄 文档", new List<string> { ".txt", ".doc", ".docx", ".pdf", ".md", ".rtf" } },
        { "🖼️  图片", new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".ico" } },
        { "🎵 音频", new List<string> { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a" } },
        { "🎬 视频", new List<string> { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv" } },
        { "💾 压缩", new List<string> { ".zip", ".rar", ".7z", ".tar", ".gz" } },
        { "💻 代码", new List<string> { ".cs", ".js", ".py", ".java", ".cpp", ".html", ".css", ".json", ".xml" } },
        { "📊 表格", new List<string> { ".xls", ".xlsx", ".csv" } }
    };
    
    var filePlan = new Dictionary<string, List<string>>();
    var otherFiles = new List<string>();
    
    foreach (var file in files)
    {
        var ext = Path.GetExtension(file).ToLower();
        var found = false;
        
        foreach (var category in categories)
        {
            if (category.Value.Contains(ext))
            {
                if (!filePlan.ContainsKey(category.Key))
                    filePlan[category.Key] = new List<string>();
                filePlan[category.Key].Add(file);
                found = true;
                break;
            }
        }
        
        if (!found)
            otherFiles.Add(file);
    }
    
    // 显示整理方案
    var tree = new Tree("[bold cyan]📋 整理方案预览[/]");
    
    foreach (var plan in filePlan.OrderByDescending(p => p.Value.Count))
    {
        var node = tree.AddNode($"[yellow]{plan.Key}[/] [dim]({plan.Value.Count} 个文件)[/]");
        foreach (var file in plan.Value.Take(3))
        {
            node.AddNode($"[dim]{Path.GetFileName(file)}[/]");
        }
        if (plan.Value.Count > 3)
        {
            node.AddNode($"[dim]... 还有 {plan.Value.Count - 3} 个文件[/]");
        }
    }
    
    if (otherFiles.Count > 0)
    {
        tree.AddNode($"[grey]📦 其他文件 ({otherFiles.Count} 个)[/]");
    }
    
    AnsiConsole.Write(tree);
    AnsiConsole.WriteLine();
    
    if (!AnsiConsole.Confirm("[bold yellow]是否开始整理？[/]"))
    {
        AnsiConsole.MarkupLine("[dim]已取消整理[/]");
        return;
    }
    
    // 执行整理
    await AnsiConsole.Progress()
        .Columns(new ProgressColumn[]
        {
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new PercentageColumn(),
            new SpinnerColumn(),
        })
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("[cyan]整理文件中...[/]", maxValue: filePlan.Sum(p => p.Value.Count));
            
            foreach (var plan in filePlan)
            {
                var folderName = plan.Key.Split(' ')[1]; // 去掉 emoji
                var targetDir = Path.Combine(currentDir, folderName);
                
                Directory.CreateDirectory(targetDir);
                
                foreach (var file in plan.Value)
                {
                    try
                    {
                        var fileName = Path.GetFileName(file);
                        var targetPath = Path.Combine(targetDir, fileName);
                        
                        // 如果目标文件已存在，添加序号
                        if (File.Exists(targetPath))
                        {
                            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                            var ext = Path.GetExtension(fileName);
                            var counter = 1;
                            
                            while (File.Exists(targetPath))
                            {
                                targetPath = Path.Combine(targetDir, $"{nameWithoutExt}_{counter}{ext}");
                                counter++;
                            }
                        }
                        
                        File.Move(file, targetPath);
                        task.Increment(1);
                        await Task.Delay(50); // 视觉效果
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
                    }
                }
            }
        });
    
    AnsiConsole.MarkupLine("\n[bold green]✅ 整理完成！[/]");
}

// === 搜索文件 ===
async Task SearchFiles()
{
    var keyword = AnsiConsole.Ask<string>("\n[cyan]🔍 请输入搜索关键词：[/]");
    
    var currentDir = Directory.GetCurrentDirectory();
    
    List<string> results = new List<string>();
    
    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync("[yellow]搜索中...[/]", async ctx =>
        {
            await Task.Run(() =>
            {
                results = Directory.GetFiles(currentDir, "*", SearchOption.AllDirectories)
                    .Where(f => Path.GetFileName(f).Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            });
        });
    
    if (results.Count == 0)
    {
        AnsiConsole.MarkupLine($"\n[yellow]😔 没有找到包含 '{keyword}' 的文件[/]");
        return;
    }
    
    AnsiConsole.MarkupLine($"\n[green]✨ 找到 {results.Count} 个匹配的文件：[/]\n");
    
    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Cyan1);
    
    table.AddColumn("[cyan]📄 文件名[/]");
    table.AddColumn("[cyan]📂 路径[/]");
    table.AddColumn("[cyan]📏 大小[/]");
    
    foreach (var file in results.Take(20))
    {
        var info = new FileInfo(file);
        var relativePath = Path.GetRelativePath(currentDir, Path.GetDirectoryName(file)!);
        
        table.AddRow(
            $"[white]{info.Name}[/]",
            $"[dim]{relativePath}[/]",
            $"[yellow]{FormatFileSize(info.Length)}[/]"
        );
    }
    
    AnsiConsole.Write(table);
    
    if (results.Count > 20)
    {
        AnsiConsole.MarkupLine($"\n[dim]... 还有 {results.Count - 20} 个结果未显示[/]");
    }
}

// === 清理临时文件 ===
async Task CleanupFiles()
{
    var currentDir = Directory.GetCurrentDirectory();
    
    var tempExtensions = new[] { ".tmp", ".temp", ".bak", ".old", ".cache", "~" };
    
    List<string> tempFiles = new List<string>();
    
    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync("[yellow]扫描临时文件...[/]", async ctx =>
        {
            await Task.Run(() =>
            {
                tempFiles = Directory.GetFiles(currentDir, "*", SearchOption.AllDirectories)
                    .Where(f => tempExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            });
        });
    
    if (tempFiles.Count == 0)
    {
        AnsiConsole.MarkupLine("\n[green]✨ 太棒了！没有发现临时文件[/]");
        return;
    }
    
    var totalSize = tempFiles.Sum(f => new FileInfo(f).Length);
    
    AnsiConsole.MarkupLine($"\n[yellow]⚠️  发现 {tempFiles.Count} 个临时文件，总大小：{FormatFileSize(totalSize)}[/]\n");
    
    foreach (var file in tempFiles.Take(10))
    {
        AnsiConsole.MarkupLine($"[dim]• {Path.GetFileName(file)}[/]");
    }
    
    if (tempFiles.Count > 10)
    {
        AnsiConsole.MarkupLine($"[dim]... 还有 {tempFiles.Count - 10} 个文件[/]");
    }
    
    AnsiConsole.WriteLine();
    
    if (!AnsiConsole.Confirm("[bold red]是否删除这些临时文件？[/]"))
    {
        AnsiConsole.MarkupLine("[dim]已取消清理[/]");
        return;
    }
    
    var deleted = 0;
    foreach (var file in tempFiles)
    {
        try
        {
            File.Delete(file);
            deleted++;
        }
        catch { }
    }
    
    AnsiConsole.MarkupLine($"\n[bold green]✅ 成功删除 {deleted} 个临时文件，释放 {FormatFileSize(totalSize)} 空间！[/]");
}

// === 显示再见 ===
void ShowGoodbye()
{
    Console.Clear();
    
    var panel = new Panel(
        new FigletText("Goodbye!")
            .Centered()
            .Color(Color.Cyan1))
    {
        Border = BoxBorder.Rounded,
        BorderStyle = new Style(Color.Cyan1)
    };
    
    AnsiConsole.Write(panel);
    AnsiConsole.MarkupLine("\n[cyan]感谢使用智能文件整理助手！[/]");
    AnsiConsole.MarkupLine("[dim]让 AI 帮你的文件井井有条 ✨[/]\n");
}

// === 辅助函数 ===
string FormatFileSize(long bytes)
{
    string[] sizes = { "B", "KB", "MB", "GB", "TB" };
    double len = bytes;
    int order = 0;
    
    while (len >= 1024 && order < sizes.Length - 1)
    {
        order++;
        len /= 1024;
    }
    
    return $"{len:0.##} {sizes[order]}";
}

Color GetColorForExtension(string ext)
{
    return ext switch
    {
        ".txt" or ".md" or ".doc" or ".docx" or ".pdf" => Color.Blue,
        ".jpg" or ".jpeg" or ".png" or ".gif" => Color.Magenta1,
        ".mp3" or ".wav" or ".flac" => Color.Green,
        ".mp4" or ".avi" or ".mkv" => Color.Red,
        ".zip" or ".rar" or ".7z" => Color.Yellow,
        ".cs" or ".js" or ".py" or ".java" => Color.Cyan1,
        _ => Color.Grey
    };
}
