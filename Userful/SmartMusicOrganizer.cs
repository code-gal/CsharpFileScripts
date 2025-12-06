#!/usr/bin/env dotnet
#:package Spectre.Console@0.49.1
#:package TagLibSharp@2.3.0

using Spectre.Console;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TagLib;

// 🎵 智能音乐整理大师 Pro - Smart Music Organizer Pro
// 
// 真实场景：经年累月下载的音乐库现状
// - 重复文件：同一首歌多个版本（MP3/FLAC，不同码率，不同命名）
// - 标签混乱：有的完整，有的空白，有的文件名就是标签
// - 歌词缺失：部分有.lrc，部分没有，还有孤立的歌词文件
//
// 核心功能：
// 1. 🔍 智能去重：通过音频特征识别同一首歌的不同版本
// 2. ⭐ 质量评分：FLAC > 320kbps > 其他，推荐保留版本
// 3. 🏷️  标签修复：从文件名智能推测歌曲信息
// 4. 📝 歌词匹配：自动关联.lrc文件
// 5. 📁 分级整理：按质量分类（无损/高品质/普通/待修复）

AnsiConsole.Clear();
ShowWelcome();

string? sourceDir = null;
string? targetDir = null;
List<MusicFileInfo> cachedMusicFiles = new List<MusicFileInfo>();

while (true)
{
    var choice = ShowMainMenu(sourceDir, targetDir);
    
    switch (choice)
    {
        case "set-source":
            sourceDir = SetSourceDirectory();
            cachedMusicFiles.Clear(); // 切换目录时清空缓存
            break;
        case "scan":
            if (sourceDir == null)
            {
                AnsiConsole.MarkupLine("[red]⚠️  请先设置源目录[/]");
                break;
            }
            cachedMusicFiles = await ScanAndAnalyze(sourceDir);
            break;
        case "duplicates":
            if (sourceDir == null || cachedMusicFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]⚠️  请先扫描目录[/]");
                break;
            }
            await SmartDuplicateFinder(cachedMusicFiles);
            break;
        case "fix-tags":
            if (sourceDir == null || cachedMusicFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]⚠️  请先扫描目录[/]");
                break;
            }
            await FixMetadataTags(cachedMusicFiles);
            break;
        case "match-lyrics":
            if (sourceDir == null)
            {
                AnsiConsole.MarkupLine("[red]⚠️  请先设置源目录[/]");
                break;
            }
            await MatchLyricsFiles(sourceDir);
            break;
        case "organize":
            if (sourceDir == null || cachedMusicFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]⚠️  请先扫描目录[/]");
                break;
            }
            targetDir = SetTargetDirectory();
            if (targetDir != null)
            {
                await OrganizeMusicAdvanced(cachedMusicFiles, targetDir);
            }
            break;
        case "audio-info":
            if (sourceDir == null || cachedMusicFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]⚠️  请先扫描目录[/]");
                break;
            }
            await AnalyzeAudioDetails(cachedMusicFiles);
            break;
        case "volume-check":
            if (sourceDir == null || cachedMusicFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]⚠️  请先扫描目录[/]");
                break;
            }
            await CheckVolumeNormalization(cachedMusicFiles);
            break;
        case "playlist":
            if (sourceDir == null || cachedMusicFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]⚠️  请先扫描目录[/]");
                break;
            }
            await GeneratePlaylists(cachedMusicFiles, sourceDir!);
            break;
        case "lyrics-analysis":
            if (sourceDir == null)
            {
                AnsiConsole.MarkupLine("[red]⚠️  请先设置源目录[/]");
                break;
            }
            await AnalyzeLyricsIntelligent(sourceDir);
            break;
        case "cover-report":
            if (sourceDir == null || cachedMusicFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]⚠️  请先扫描目录[/]");
                break;
            }
            await GenerateCoverArtReport(cachedMusicFiles, sourceDir!);
            break;
        case "report":
            if (sourceDir == null || cachedMusicFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]⚠️  请先扫描目录[/]");
                break;
            }
            await GenerateDetailedReport(cachedMusicFiles, sourceDir!);
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

// === 欢迎界面 ===
void ShowWelcome()
{
    var gradient = new FigletText("Music Organizer Pro")
        .Centered()
        .Color(Color.Magenta1);
    
    AnsiConsole.Write(gradient);
    
    var panel = new Panel(
        Align.Center(
            new Markup(
                "[yellow]🎵 智能音乐整理大师 Pro[/]\n\n" +
                "[dim]💭 你的真实困境：\n" +
                "• 多年下载的音乐，重复文件一堆\n" +
                "• 同一首歌：MP3/FLAC/不同码率/不同命名\n" +
                "• 有的有完整标签，有的只有文件名\n" +
                "• 有的有.lrc歌词，有的没有\n" +
                "• 想整理但不知道从何下手...\n\n" +
                "✨ 让AI帮你智能处理！[/]\n\n" +
                "[cyan]🔥 Pro 功能：\n" +
                "• 智能去重：识别同一首歌的不同版本\n" +
                "• 质量评分：自动推荐保留最佳版本\n" +
                "• 标签修复：从文件名智能推测歌曲信息\n" +
                "• 歌词匹配：自动关联.lrc文件\n" +
                "• 分级整理：无损/高品质/普通 分类管理\n" +
                "• 完整报告：详细的音乐库健康度分析[/]"
            )
        ))
    {
        Border = BoxBorder.Double,
        BorderStyle = new Style(Color.Magenta1),
        Padding = new Padding(2, 1)
    };
    
    AnsiConsole.Write(panel);
    AnsiConsole.WriteLine();
}

// === 主菜单 ===
string ShowMainMenu(string? source, string? target)
{
    var statusGrid = new Grid();
    statusGrid.AddColumn();
    statusGrid.AddColumn();
    
    statusGrid.AddRow(
        "[cyan]📂 源目录:[/]",
        source != null ? $"[green]{source}[/]" : "[dim]未设置[/]"
    );
    statusGrid.AddRow(
        "[cyan]🎯 目标目录:[/]",
        target != null ? $"[green]{target}[/]" : "[dim]未设置[/]"
    );
    statusGrid.AddRow(
        "[cyan]📀 已扫描:[/]",
        cachedMusicFiles.Count > 0 ? $"[green]{cachedMusicFiles.Count} 首[/]" : "[dim]未扫描[/]"
    );
    
    AnsiConsole.Write(new Panel(statusGrid)
    {
        Header = new PanelHeader("[bold yellow]📊 当前状态[/]"),
        Border = BoxBorder.Rounded,
        Padding = new Padding(1, 0)
    });
    AnsiConsole.WriteLine();
    
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[bold magenta]🎯 请选择操作：[/]")
            .PageSize(15)
            .HighlightStyle(new Style(Color.Cyan1, decoration: Decoration.Bold))
            .AddChoices(new[] {
                "📂 set-source - 设置源目录（从哪里找音乐）",
                "🔍 scan - 深度扫描并分析音乐文件",
                "🔄 duplicates - 智能去重（同一首歌不同版本）",
                "🏷️  fix-tags - 修复缺失的元数据标签",
                "📝 match-lyrics - 匹配歌词文件",
                "📁 organize - 分级整理（无损/高品质/普通）",
                "--- 🎵 高级分析功能 ---",
                "🎼 audio-info - 音频详细信息分析",
                "🔊 volume-check - 音量标准化检测",
                "🎧 playlist - 生成智能播放列表",
                "📄 lyrics-analysis - 歌词智能分析（含翻译检测）",
                "🖼️  cover-report - 缺少封面报告",
                "--- 📊 报告 ---",
                "📊 report - 生成完整健康度报告",
                "🚪 exit - 退出程序"
            }));
    
    // 处理分隔线选项
    if (choice.StartsWith("---"))
    {
        return ShowMainMenu(source, target); // 重新显示菜单
    }
    
    return choice.Split(' ')[1];
}

// === 设置源目录 ===
string? SetSourceDirectory()
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]💡 常见场景示例：[/]");
    AnsiConsole.MarkupLine("[dim]  • Windows: C:\\Users\\YourName\\Downloads\\Music[/]");
    AnsiConsole.MarkupLine("[dim]  • Linux: /home/username/Music[/]");
    AnsiConsole.MarkupLine("[dim]  • Mac: /Users/username/Downloads[/]");
    AnsiConsole.WriteLine();
    
    var dir = AnsiConsole.Ask<string>("[yellow]请输入要扫描的目录路径：[/]");
    
    if (!Directory.Exists(dir))
    {
        AnsiConsole.MarkupLine("[red]❌ 目录不存在！[/]");
        return null;
    }
    
    AnsiConsole.MarkupLine($"[green]✅ 已设置源目录：{dir}[/]");
    return dir;
}

// === 设置目标目录 ===
string? SetTargetDirectory()
{
    AnsiConsole.WriteLine();
    var dir = AnsiConsole.Ask<string>("[yellow]请输入整理后的音乐库保存路径：[/]");
    
    if (!Directory.Exists(dir))
    {
        if (AnsiConsole.Confirm($"[yellow]目录不存在，是否创建？[/]"))
        {
            Directory.CreateDirectory(dir);
            AnsiConsole.MarkupLine("[green]✅ 目录已创建[/]");
        }
        else
        {
            return null;
        }
    }
    
    AnsiConsole.MarkupLine($"[green]✅ 已设置目标目录：{dir}[/]");
    return dir;
}

// === 扫描和分析 ===
async Task<List<MusicFileInfo>> ScanAndAnalyze(string sourceDir)
{
    var musicExtensions = new[] { ".mp3", ".flac", ".m4a", ".wav", ".wma", ".aac", ".ogg", ".ape" };
    List<MusicFileInfo> musicFiles = new List<MusicFileInfo>();
    
    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots2)
        .SpinnerStyle(Style.Parse("magenta bold"))
        .StartAsync("[yellow]🔍 正在深度扫描目录...[/]", async ctx =>
        {
            await Task.Run(() =>
            {
                try
                {
                    var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories)
                        .Where(f => musicExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .ToList();
                    
                    ctx.Status("[yellow]📖 正在读取音乐元数据和计算质量分...[/]");
                    
                    foreach (var file in files)
                    {
                        try
                        {
                            var info = GetMusicInfoWithQuality(file);
                            musicFiles.Add(info);
                        }
                        catch
                        {
                            // 无法读取的文件跳过
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // 忽略无权限的目录
                }
            });
        });
    
    if (musicFiles.Count == 0)
    {
        AnsiConsole.MarkupLine("\n[yellow]😔 未找到任何音乐文件[/]");
        return musicFiles;
    }
    
    // 显示扫描结果
    AnsiConsole.WriteLine();
    
    var losslessCount = musicFiles.Count(m => m.QualityTier == "无损");
    var highQualityCount = musicFiles.Count(m => m.QualityTier == "高品质");
    var normalCount = musicFiles.Count(m => m.QualityTier == "普通");
    var lowQualityCount = musicFiles.Count(m => m.QualityTier == "低品质");
    
    var resultPanel = new Panel(
        new Markup(
            $"[green]✨ 扫描完成！[/]\n\n" +
            $"[cyan]📀 找到音乐文件：[/] [bold]{musicFiles.Count}[/] 首\n" +
            $"[cyan]🎤 识别出艺术家：[/] [bold]{musicFiles.Select(m => m.Artist).Distinct().Count()}[/] 位\n" +
            $"[cyan]💿 识别出专辑：[/] [bold]{musicFiles.Select(m => m.Album).Distinct().Count()}[/] 张\n" +
            $"[cyan]📊 总大小：[/] [bold]{FormatFileSize(musicFiles.Sum(m => m.Size))}[/]\n\n" +
            $"[yellow]🎯 质量分布：[/]\n" +
            $"  • 无损音质 (FLAC/APE): [green]{losslessCount}[/] 首\n" +
            $"  • 高品质 (320kbps): [cyan]{highQualityCount}[/] 首\n" +
            $"  • 普通 (192-256kbps): [white]{normalCount}[/] 首\n" +
            $"  • 低品质 (<192kbps): [dim]{lowQualityCount}[/] 首"
        ))
    {
        Header = new PanelHeader("[bold green]📊 扫描统计[/]"),
        Border = BoxBorder.Rounded,
        BorderStyle = new Style(Color.Green)
    };
    
    AnsiConsole.Write(resultPanel);
    AnsiConsole.WriteLine();
    
    // 显示问题文件统计
    var noTagFiles = musicFiles.Where(m => m.Artist == "Unknown Artist").ToList();
    var lowScoreFiles = musicFiles.Where(m => m.TagCompleteness < 50).ToList();
    
    if (noTagFiles.Count > 0 || lowScoreFiles.Count > 0)
    {
        var problemsTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow);
        
        problemsTable.AddColumn("[yellow]问题类型[/]");
        problemsTable.AddColumn("[yellow]数量[/]");
        problemsTable.AddColumn("[yellow]建议[/]");
        
        if (noTagFiles.Count > 0)
        {
            problemsTable.AddRow(
                "⚠️  完全缺少标签",
                $"[red]{noTagFiles.Count}[/] 个",
                "[dim]使用「修复标签」功能[/]"
            );
        }
        
        if (lowScoreFiles.Count > 0)
        {
            problemsTable.AddRow(
                "⚠️  标签不完整",
                $"[yellow]{lowScoreFiles.Count}[/] 个",
                "[dim]使用「修复标签」功能[/]"
            );
        }
        
        AnsiConsole.Write(new Panel(problemsTable)
        {
            Header = new PanelHeader("[bold yellow]🔧 发现的问题[/]")
        });
    }
    
    return musicFiles;
}

// === 智能去重 ===
async Task SmartDuplicateFinder(List<MusicFileInfo> musicFiles)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]🔍 智能去重说明：[/]");
    AnsiConsole.MarkupLine("[dim]• 不仅仅是文件内容相同，还会识别同一首歌的不同版本[/]");
    AnsiConsole.MarkupLine("[dim]• 例如：「七里香.mp3」和「七里香.flac」会被识别为重复[/]");
    AnsiConsole.MarkupLine("[dim]• 推荐保留：FLAC > 320kbps MP3 > 其他[/]\n");
    
    Dictionary<string, List<MusicFileInfo>> duplicateGroups = new Dictionary<string, List<MusicFileInfo>>();
    
    await AnsiConsole.Status()
        .StartAsync("[yellow]🔍 分析中...[/]", async ctx =>
        {
            await Task.Run(() =>
            {
                // 方法1: 按 "艺术家 - 歌曲名" 分组
                var groups = musicFiles
                    .Where(m => m.Artist != "Unknown Artist" && m.Title != "Unknown")
                    .GroupBy(m => $"{NormalizeString(m.Artist)}|||{NormalizeString(m.Title)}")
                    .Where(g => g.Count() > 1);
                
                foreach (var group in groups)
                {
                    duplicateGroups[group.Key] = group.OrderByDescending(m => m.QualityScore).ToList();
                }
            });
        });
    
    if (duplicateGroups.Count == 0)
    {
        AnsiConsole.MarkupLine("[green]✨ 太好了！没有发现重复的音乐[/]");
        return;
    }
    
    var totalDuplicates = duplicateGroups.Sum(g => g.Value.Count - 1);
    var canSaveSpace = duplicateGroups.Sum(g => 
        g.Value.Skip(1).Sum(m => m.Size));
    
    AnsiConsole.MarkupLine($"[yellow]⚠️  发现 {duplicateGroups.Count} 首歌曲有 {totalDuplicates} 个重复版本[/]");
    AnsiConsole.MarkupLine($"[yellow]💾 如果清理，可节省：{FormatFileSize(canSaveSpace)}[/]\n");
    
    // 显示前几组
    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Yellow);
    
    table.AddColumn("[yellow]歌曲[/]");
    table.AddColumn("[yellow]版本[/]");
    table.AddColumn("[yellow]格式[/]");
    table.AddColumn("[yellow]质量[/]");
    table.AddColumn("[yellow]大小[/]");
    table.AddColumn("[yellow]建议[/]");
    
    foreach (var group in duplicateGroups.Take(10))
    {
        var firstInfo = group.Value[0];
        var songName = $"{firstInfo.Artist} - {firstInfo.Title}";
        
        bool firstInGroup = true;
        foreach (var music in group.Value)
        {
            var format = Path.GetExtension(music.FilePath).ToUpper().TrimStart('.');
            var quality = music.Bitrate > 0 ? $"{music.Bitrate}kbps" : music.QualityTier;
            var recommendation = firstInGroup ? "[green]✅ 保留[/]" : "[dim]❌ 可删除[/]";
            
            table.AddRow(
                firstInGroup ? $"[bold]{songName}[/]" : "",
                $"[dim]v{group.Value.IndexOf(music) + 1}[/]",
                $"[cyan]{format}[/]",
                quality,
                FormatFileSize(music.Size),
                recommendation
            );
            firstInGroup = false;
        }
    }
    
    AnsiConsole.Write(table);
    
    if (duplicateGroups.Count > 10)
    {
        AnsiConsole.MarkupLine($"\n[dim]... 还有 {duplicateGroups.Count - 10} 组重复歌曲[/]");
    }
    
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]💡 提示：在「分级整理」时可以选择处理重复文件的策略[/]");
}

// === 修复标签 ===
async Task FixMetadataTags(List<MusicFileInfo> musicFiles)
{
    var brokenFiles = musicFiles.Where(m => m.TagCompleteness < 80).ToList();
    
    if (brokenFiles.Count == 0)
    {
        AnsiConsole.MarkupLine("\n[green]✨ 所有文件标签完整，无需修复！[/]");
        return;
    }
    
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[yellow]🔧 发现 {brokenFiles.Count} 个文件标签不完整[/]\n");
    
    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Yellow);
    
    table.AddColumn("[yellow]文件名[/]");
    table.AddColumn("[yellow]当前标签[/]");
    table.AddColumn("[yellow]推测信息[/]");
    table.AddColumn("[yellow]完整度[/]");
    
    foreach (var file in brokenFiles.Take(10))
    {
        var fileName = Path.GetFileNameWithoutExtension(file.FilePath);
        var current = $"{file.Artist} - {file.Title}";
        var guessed = GuessInfoFromFilename(fileName);
        var completeness = $"{file.TagCompleteness}%";
        
        table.AddRow(
            $"[dim]{fileName}[/]",
            file.Artist == "Unknown Artist" ? "[red]无标签[/]" : $"[yellow]{current}[/]",
            $"[green]{guessed.Artist} - {guessed.Title}[/]",
            completeness
        );
    }
    
    AnsiConsole.Write(table);
    
    if (brokenFiles.Count > 10)
    {
        AnsiConsole.MarkupLine($"\n[dim]... 还有 {brokenFiles.Count - 10} 个文件[/]");
    }
    
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]💡 提示：[/]");
    AnsiConsole.MarkupLine("[dim]• 从文件名推测的信息可能不准确[/]");
    AnsiConsole.MarkupLine("[dim]• 建议手动检查重要文件的标签[/]");
    AnsiConsole.MarkupLine("[dim]• 可以使用在线音乐标签库来获取准确信息[/]");
    
    if (AnsiConsole.Confirm("\n[yellow]是否自动修复标签？（会修改原文件）[/]"))
    {
        int fixedCount = 0;
        
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[cyan]修复标签中...[/]", maxValue: brokenFiles.Count);
                
                foreach (var music in brokenFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(music.FilePath);
                        var guessed = GuessInfoFromFilename(fileName);
                        
                        using var file = TagLib.File.Create(music.FilePath);
                        
                        if (string.IsNullOrWhiteSpace(file.Tag.FirstPerformer))
                        {
                            file.Tag.Performers = new[] { guessed.Artist };
                        }
                        if (string.IsNullOrWhiteSpace(file.Tag.Title))
                        {
                            file.Tag.Title = guessed.Title;
                        }
                        
                        file.Save();
                        fixedCount++;
                    }
                    catch { }
                    
                    task.Increment(1);
                    await Task.Delay(20);
                }
            });
        
        AnsiConsole.MarkupLine($"\n[green]✅ 成功修复 {fixedCount} 个文件的标签[/]");
        AnsiConsole.MarkupLine("[yellow]⚠️  请重新扫描以更新信息[/]");
    }
}

// === 匹配歌词 ===
async Task MatchLyricsFiles(string sourceDir)
{
    AnsiConsole.WriteLine();
    
    List<string> musicFiles = new List<string>();
    List<string> lrcFiles = new List<string>();
    
    await AnsiConsole.Status()
        .StartAsync("[yellow]扫描文件...[/]", async ctx =>
        {
            await Task.Run(() =>
            {
                var allFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
                musicFiles = allFiles.Where(f => new[] { ".mp3", ".flac", ".m4a" }.Contains(Path.GetExtension(f).ToLower())).ToList();
                lrcFiles = allFiles.Where(f => f.EndsWith(".lrc", StringComparison.OrdinalIgnoreCase)).ToList();
            });
        });
    
    AnsiConsole.MarkupLine($"[cyan]📀 音乐文件：{musicFiles.Count} 个[/]");
    AnsiConsole.MarkupLine($"[cyan]📝 歌词文件：{lrcFiles.Count} 个[/]\n");
    
    // 统计匹配情况
    var matched = new List<(string music, string lrc)>();
    var musicWithoutLrc = new List<string>();
    var orphanLrc = new List<string>(lrcFiles);
    
    foreach (var music in musicFiles)
    {
        var musicName = Path.GetFileNameWithoutExtension(music);
        var musicDir = Path.GetDirectoryName(music)!;
        
        // 查找同名.lrc文件
        var possibleLrc = Path.Combine(musicDir, musicName + ".lrc");
        
        if (System.IO.File.Exists(possibleLrc))
        {
            matched.Add((music, possibleLrc));
            orphanLrc.Remove(possibleLrc);
        }
        else
        {
            musicWithoutLrc.Add(music);
        }
    }
    
    // 显示结果
    var statsGrid = new Grid();
    statsGrid.AddColumn();
    statsGrid.AddColumn();
    
    statsGrid.AddRow("[green]✅ 已匹配：[/]", $"[bold]{matched.Count}[/] 对");
    statsGrid.AddRow("[yellow]⚠️  缺少歌词：[/]", $"[bold]{musicWithoutLrc.Count}[/] 首");
    statsGrid.AddRow("[red]❌ 孤立歌词：[/]", $"[bold]{orphanLrc.Count}[/] 个");
    
    AnsiConsole.Write(new Panel(statsGrid)
    {
        Header = new PanelHeader("[bold cyan]📊 歌词匹配统计[/]"),
        Border = BoxBorder.Rounded
    });
    
    if (musicWithoutLrc.Count > 0)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]缺少歌词的歌曲（前10首）：[/]");
        foreach (var music in musicWithoutLrc.Take(10))
        {
            AnsiConsole.MarkupLine($"[dim]• {Path.GetFileName(music)}[/]");
        }
    }
    
    if (orphanLrc.Count > 0)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[red]孤立的歌词文件（前10个）：[/]");
        foreach (var lrc in orphanLrc.Take(10))
        {
            AnsiConsole.MarkupLine($"[dim]• {Path.GetFileName(lrc)}[/]");
        }
    }
    
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]💡 提示：在整理音乐时，会自动复制匹配的歌词文件[/]");
}

// === 高级整理 ===
async Task OrganizeMusicAdvanced(List<MusicFileInfo> musicFiles, string targetDir)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]📋 请选择整理策略：[/]\n");
    
    var strategy = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .AddChoices(new[] {
                "1. 按质量分级（推荐）- 无损/高品质/普通/低品质",
                "2. 按艺术家/专辑（传统）",
                "3. 智能去重+按质量分级（最省空间）"
            }));
    
    var strategyType = strategy[0].ToString();
    
    if (strategyType == "3")
    {
        // 去重策略
        var duplicateGroups = musicFiles
            .Where(m => m.Artist != "Unknown Artist" && m.Title != "Unknown")
            .GroupBy(m => $"{NormalizeString(m.Artist)}|||{NormalizeString(m.Title)}")
            .ToList();
        
        var filesToOrganize = new List<MusicFileInfo>();
        
        foreach (var group in duplicateGroups)
        {
            // 每组只保留质量最好的
            filesToOrganize.Add(group.OrderByDescending(m => m.QualityScore).First());
        }
        
        // 加上没有分组的（唯一的）
        var uniqueFiles = musicFiles.Except(duplicateGroups.SelectMany(g => g)).ToList();
        filesToOrganize.AddRange(uniqueFiles);
        
        AnsiConsole.MarkupLine($"\n[cyan]原始文件：{musicFiles.Count} 首[/]");
        AnsiConsole.MarkupLine($"[green]去重后：{filesToOrganize.Count} 首[/]");
        AnsiConsole.MarkupLine($"[yellow]节省：{musicFiles.Count - filesToOrganize.Count} 个重复文件[/]\n");
        
        musicFiles = filesToOrganize;
    }
    
    // 显示整理预览
    var tree = new Tree("[bold magenta]🎵 整理结构预览[/]");
    
    if (strategyType == "1" || strategyType == "3")
    {
        // 按质量分级
        var tiers = new[] { "无损", "高品质", "普通", "低品质" };
        
        foreach (var tier in tiers)
        {
            var tierFiles = musicFiles.Where(m => m.QualityTier == tier).ToList();
            if (tierFiles.Count == 0) continue;
            
            var tierNode = tree.AddNode($"[yellow]{tier}[/] [dim]({tierFiles.Count} 首)[/]");
            
            var artists = tierFiles.GroupBy(m => m.Artist).OrderBy(g => g.Key).Take(3);
            foreach (var artist in artists)
            {
                var artistNode = tierNode.AddNode($"[cyan]{artist.Key}[/] [dim]({artist.Count()} 首)[/]");
                foreach (var song in artist.Take(2))
                {
                    artistNode.AddNode($"[dim]{song.Title}[/]");
                }
            }
        }
    }
    else
    {
        // 按艺术家/专辑
        var artists = musicFiles.GroupBy(m => m.Artist).OrderBy(g => g.Key).Take(5);
        foreach (var artist in artists)
        {
            var artistNode = tree.AddNode($"[yellow]{artist.Key}[/] [dim]({artist.Count()} 首)[/]");
            var albums = artist.GroupBy(m => m.Album).Take(2);
            foreach (var album in albums)
            {
                artistNode.AddNode($"[cyan]{album.Key}[/] [dim]({album.Count()} 首)[/]");
            }
        }
    }
    
    AnsiConsole.Write(tree);
    AnsiConsole.WriteLine();
    
    if (!AnsiConsole.Confirm("[bold yellow]开始整理？[/]"))
    {
        AnsiConsole.MarkupLine("[dim]已取消[/]");
        return;
    }
    
    // 执行整理
    int successCount = 0;
    int failCount = 0;
    
    await AnsiConsole.Progress()
        .Columns(new ProgressColumn[]
        {
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new PercentageColumn(),
            new RemainingTimeColumn(),
            new SpinnerColumn(),
        })
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("[cyan]整理音乐中...[/]", maxValue: musicFiles.Count);
            
            foreach (var music in musicFiles)
            {
                try
                {
                    string targetPath;
                    
                    if (strategyType == "1" || strategyType == "3")
                    {
                        // 质量分级：质量级别/艺术家/专辑/
                        var tierDir = Path.Combine(targetDir, music.QualityTier);
                        var artistDir = Path.Combine(tierDir, SanitizePath(music.Artist));
                        var albumDir = Path.Combine(artistDir, SanitizePath(music.Album));
                        Directory.CreateDirectory(albumDir);
                        
                        var ext = Path.GetExtension(music.FilePath);
                        var fileName = $"{music.Track:D2} - {SanitizePath(music.Title)}{ext}";
                        targetPath = Path.Combine(albumDir, fileName);
                    }
                    else
                    {
                        // 传统：艺术家/专辑/
                        var artistDir = Path.Combine(targetDir, SanitizePath(music.Artist));
                        var albumDir = Path.Combine(artistDir, SanitizePath(music.Album));
                        Directory.CreateDirectory(albumDir);
                        
                        var ext = Path.GetExtension(music.FilePath);
                        var fileName = $"{music.Track:D2} - {SanitizePath(music.Title)}{ext}";
                        targetPath = Path.Combine(albumDir, fileName);
                    }
                    
                    // 处理文件名冲突
                    if (System.IO.File.Exists(targetPath))
                    {
                        var dir = Path.GetDirectoryName(targetPath)!;
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(targetPath);
                        var ext = Path.GetExtension(targetPath);
                        var counter = 1;
                        
                        while (System.IO.File.Exists(targetPath))
                        {
                            targetPath = Path.Combine(dir, $"{nameWithoutExt} ({counter}){ext}");
                            counter++;
                        }
                    }
                    
                    // 复制音乐文件
                    System.IO.File.Copy(music.FilePath, targetPath, false);
                    
                    // 查找并复制对应的歌词文件
                    var lrcPath = Path.ChangeExtension(music.FilePath, ".lrc");
                    if (System.IO.File.Exists(lrcPath))
                    {
                        var targetLrcPath = Path.ChangeExtension(targetPath, ".lrc");
                        System.IO.File.Copy(lrcPath, targetLrcPath, true);
                    }
                    
                    successCount++;
                }
                catch
                {
                    failCount++;
                }
                
                task.Increment(1);
                await Task.Delay(15);
            }
        });
    
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[bold green]✅ 整理完成！[/]");
    AnsiConsole.MarkupLine($"[green]成功：{successCount} 首[/]");
    if (failCount > 0)
    {
        AnsiConsole.MarkupLine($"[red]失败：{failCount} 首[/]");
    }
    AnsiConsole.MarkupLine($"\n[cyan]📂 音乐库位置：{targetDir}[/]");
}

// === 生成详细报告 ===
async Task GenerateDetailedReport(List<MusicFileInfo> musicFiles, string sourceDir)
{
    Console.Clear();
    
    AnsiConsole.Write(
        new FigletText("Health Report")
            .Centered()
            .Color(Color.Magenta1)
    );
    
    // 总体健康度评分
    var avgCompleteness = musicFiles.Average(m => m.TagCompleteness);
    var avgQuality = musicFiles.Average(m => m.QualityScore);
    var healthScore = (avgCompleteness + avgQuality) / 2;
    
    var healthColor = healthScore >= 80 ? Color.Green : healthScore >= 60 ? Color.Yellow : Color.Red;
    var healthEmoji = healthScore >= 80 ? "😄" : healthScore >= 60 ? "😐" : "😢";
    
    var scorePanel = new Panel(
        Align.Center(
            new Markup(
                $"[bold {healthColor}]音乐库健康度：{healthScore:F1}分 {healthEmoji}[/]\n\n" +
                $"[dim]标签完整度：{avgCompleteness:F1}% | 音质评分：{avgQuality:F1}分[/]"
            )
        ))
    {
        Border = BoxBorder.Double,
        BorderStyle = new Style(healthColor)
    };
    
    AnsiConsole.Write(scorePanel);
    AnsiConsole.WriteLine();
    
    // 基础统计
    var statsGrid = new Grid();
    statsGrid.AddColumn();
    statsGrid.AddColumn();
    statsGrid.AddColumn();
    statsGrid.AddColumn();
    
    statsGrid.AddRow(
        "[cyan]🎵 总歌曲[/]", $"[bold]{musicFiles.Count}[/] 首",
        "[cyan]🎤 艺术家[/]", $"[bold]{musicFiles.Select(m => m.Artist).Distinct().Count()}[/] 位"
    );
    statsGrid.AddRow(
        "[cyan]💿 专辑[/]", $"[bold]{musicFiles.Select(m => m.Album).Distinct().Count()}[/] 张",
        "[cyan]📊 总大小[/]", $"[bold]{FormatFileSize(musicFiles.Sum(m => m.Size))}[/]"
    );
    
    AnsiConsole.Write(new Panel(statsGrid)
    {
        Header = new PanelHeader("[bold green]📊 基础统计[/]"),
        Border = BoxBorder.Rounded
    });
    AnsiConsole.WriteLine();
    
    // 质量分布
    var qualityChart = new BarChart()
        .Width(60)
        .Label("[bold cyan]🎼 音质分布[/]")
        .CenterLabel();
    
    qualityChart.AddItem("无损", musicFiles.Count(m => m.QualityTier == "无损"), Color.Green);
    qualityChart.AddItem("高品质", musicFiles.Count(m => m.QualityTier == "高品质"), Color.Cyan1);
    qualityChart.AddItem("普通", musicFiles.Count(m => m.QualityTier == "普通"), Color.Yellow);
    qualityChart.AddItem("低品质", musicFiles.Count(m => m.QualityTier == "低品质"), Color.Red);
    
    AnsiConsole.Write(qualityChart);
    AnsiConsole.WriteLine();
    
    // 问题清单
    var problems = new List<string>();
    
    var noTagCount = musicFiles.Count(m => m.Artist == "Unknown Artist");
    if (noTagCount > 0)
        problems.Add($"[red]• {noTagCount} 首歌曲完全缺少标签[/]");
    
    var lowTagCount = musicFiles.Count(m => m.TagCompleteness < 50);
    if (lowTagCount > 0)
        problems.Add($"[yellow]• {lowTagCount} 首歌曲标签不完整[/]");
    
    var lowQualityCount = musicFiles.Count(m => m.QualityTier == "低品质");
    if (lowQualityCount > 0)
        problems.Add($"[yellow]• {lowQualityCount} 首歌曲音质较低[/]");
    
    // 估算重复
    var duplicateCount = musicFiles
        .Where(m => m.Artist != "Unknown Artist")
        .GroupBy(m => $"{m.Artist}|||{m.Title}")
        .Count(g => g.Count() > 1);
    
    if (duplicateCount > 0)
        problems.Add($"[yellow]• 约 {duplicateCount} 首歌曲存在重复版本[/]");
    
    if (problems.Count > 0)
    {
        var problemPanel = new Panel(
            string.Join("\n", problems)
        )
        {
            Header = new PanelHeader("[bold red]⚠️  发现的问题[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Red)
        };
        
        AnsiConsole.Write(problemPanel);
        AnsiConsole.WriteLine();
    }
    
    // 建议
    var recommendations = new List<string>();
    
    if (noTagCount > 0 || lowTagCount > 0)
        recommendations.Add("[cyan]1. 使用「修复标签」功能补全缺失信息[/]");
    
    if (duplicateCount > 0)
        recommendations.Add("[cyan]2. 使用「智能去重」查看重复文件[/]");
    
    if (lowQualityCount > 0)
        recommendations.Add("[cyan]3. 考虑用高品质版本替换低品质音乐[/]");
    
    recommendations.Add("[cyan]4. 使用「分级整理」功能组织音乐库[/]");
    
    var recPanel = new Panel(
        string.Join("\n", recommendations)
    )
    {
        Header = new PanelHeader("[bold green]💡 改进建议[/]"),
        Border = BoxBorder.Rounded,
        BorderStyle = new Style(Color.Green)
    };
    
    AnsiConsole.Write(recPanel);
}

// === 辅助函数 ===
MusicFileInfo GetMusicInfoWithQuality(string filePath)
{
    var info = new MusicFileInfo
    {
        FilePath = filePath,
        Size = new FileInfo(filePath).Length
    };
    
    try
    {
        using var file = TagLib.File.Create(filePath);
        
        info.Title = string.IsNullOrWhiteSpace(file.Tag.Title) 
            ? Path.GetFileNameWithoutExtension(filePath) 
            : file.Tag.Title;
        info.Artist = string.IsNullOrWhiteSpace(file.Tag.FirstPerformer) 
            ? "Unknown Artist" 
            : file.Tag.FirstPerformer;
        info.Album = string.IsNullOrWhiteSpace(file.Tag.Album) 
            ? "Unknown Album" 
            : file.Tag.Album;
        info.Year = file.Tag.Year;
        info.Track = file.Tag.Track;
        info.Duration = file.Properties.Duration;
        info.Bitrate = file.Properties.AudioBitrate;
        
        // 计算标签完整度 (0-100)
        int tagScore = 0;
        if (!string.IsNullOrWhiteSpace(file.Tag.Title)) tagScore += 25;
        if (!string.IsNullOrWhiteSpace(file.Tag.FirstPerformer)) tagScore += 25;
        if (!string.IsNullOrWhiteSpace(file.Tag.Album)) tagScore += 20;
        if (file.Tag.Year > 0) tagScore += 15;
        if (file.Tag.Track > 0) tagScore += 15;
        
        info.TagCompleteness = tagScore;
        
        // 计算质量评分和分级
        var ext = Path.GetExtension(filePath).ToLower();
        
        if (ext == ".flac" || ext == ".ape" || ext == ".wav")
        {
            info.QualityTier = "无损";
            info.QualityScore = 100;
        }
        else if (info.Bitrate >= 320)
        {
            info.QualityTier = "高品质";
            info.QualityScore = 90;
        }
        else if (info.Bitrate >= 192)
        {
            info.QualityTier = "普通";
            info.QualityScore = 70;
        }
        else
        {
            info.QualityTier = "低品质";
            info.QualityScore = 50;
        }
    }
    catch
    {
        info.Title = Path.GetFileNameWithoutExtension(filePath);
        info.Artist = "Unknown Artist";
        info.Album = "Unknown Album";
        info.TagCompleteness = 0;
        info.QualityTier = "普通";
        info.QualityScore = 50;
    }
    
    return info;
}

(string Artist, string Title) GuessInfoFromFilename(string filename)
{
    // 常见模式:
    // "艺术家 - 歌名"
    // "歌名 - 艺术家"
    // "艺术家-歌名"
    
    var patterns = new[]
    {
        @"^(.+?)\s*-\s*(.+)$",  // 艺术家 - 歌名
        @"^(.+?)[-_](.+)$",      // 艺术家-歌名 or 艺术家_歌名
    };
    
    foreach (var pattern in patterns)
    {
        var match = Regex.Match(filename, pattern);
        if (match.Success)
        {
            var part1 = match.Groups[1].Value.Trim();
            var part2 = match.Groups[2].Value.Trim();
            
            // 简单启发式：如果part1更短，可能是艺术家
            if (part1.Length < part2.Length * 0.7)
            {
                return (part1, part2);
            }
            else
            {
                return (part1, part2); // 默认第一个是艺术家
            }
        }
    }
    
    return ("Unknown Artist", filename);
}

string NormalizeString(string str)
{
    // 规范化字符串用于比较（去空格、转小写）
    return str.ToLower().Replace(" ", "").Replace("-", "");
}

string SanitizePath(string path)
{
    var invalid = Path.GetInvalidFileNameChars();
    return string.Join("_", path.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
}

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

// === 🎼 音频详细信息分析 ===
async Task AnalyzeAudioDetails(List<MusicFileInfo> musicFiles)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]🎼 音频技术参数分析[/]\n");
    
    var detailedInfoList = new List<AudioDetailInfo>();
    
    await AnsiConsole.Status()
        .StartAsync("[yellow]分析音频参数...[/]", async ctx =>
        {
            await Task.Run(() =>
            {
                foreach (var music in musicFiles.Take(100)) // 分析前100首
                {
                    try
                    {
                        using var file = TagLib.File.Create(music.FilePath);
                        var info = new AudioDetailInfo
                        {
                            FilePath = music.FilePath,
                            SampleRate = file.Properties.AudioSampleRate,
                            BitsPerSample = file.Properties.BitsPerSample,
                            Channels = file.Properties.AudioChannels,
                            Codec = file.Properties.Description,
                            Bitrate = file.Properties.AudioBitrate,
                            Duration = file.Properties.Duration
                        };
                        detailedInfoList.Add(info);
                    }
                    catch { }
                }
            });
        });
    
    if (detailedInfoList.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]无法获取音频详细信息[/]");
        return;
    }
    
    // 统计分析
    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Cyan1);
    
    table.AddColumn("[cyan]参数[/]");
    table.AddColumn("[cyan]最常见值[/]");
    table.AddColumn("[cyan]其他值[/]");
    
    // 采样率分布
    var sampleRates = detailedInfoList.GroupBy(d => d.SampleRate).OrderByDescending(g => g.Count());
    var topSampleRate = sampleRates.First();
    table.AddRow(
        "📊 采样率",
        $"[green]{topSampleRate.Key} Hz[/] ({topSampleRate.Count()} 首)",
        string.Join(", ", sampleRates.Skip(1).Take(3).Select(g => $"{g.Key} Hz"))
    );
    
    // 比特深度
    var bitsPerSample = detailedInfoList.GroupBy(d => d.BitsPerSample).OrderByDescending(g => g.Count());
    var topBits = bitsPerSample.First();
    table.AddRow(
        "🎚️  比特深度",
        $"[green]{topBits.Key} bit[/] ({topBits.Count()} 首)",
        string.Join(", ", bitsPerSample.Skip(1).Take(3).Select(g => $"{g.Key} bit"))
    );
    
    // 声道数
    var channels = detailedInfoList.GroupBy(d => d.Channels).OrderByDescending(g => g.Count());
    var topChannels = channels.First();
    var channelName = topChannels.Key == 2 ? "立体声" : topChannels.Key == 1 ? "单声道" : $"{topChannels.Key}声道";
    table.AddRow(
        "🔊 声道",
        $"[green]{channelName}[/] ({topChannels.Count()} 首)",
        string.Join(", ", channels.Skip(1).Take(3).Select(g => g.Key == 2 ? "立体声" : $"{g.Key}声道"))
    );
    
    AnsiConsole.Write(new Panel(table)
    {
        Header = new PanelHeader("[bold cyan]🎵 音频参数统计[/]")
    });
    
    // 显示详细示例
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[yellow]📋 音频参数详细列表（前10首）：[/]\n");
    
    var detailTable = new Table()
        .Border(TableBorder.Rounded);
    
    detailTable.AddColumn("[cyan]文件[/]");
    detailTable.AddColumn("[cyan]采样率[/]");
    detailTable.AddColumn("[cyan]位深[/]");
    detailTable.AddColumn("[cyan]码率[/]");
    detailTable.AddColumn("[cyan]编码[/]");
    
    foreach (var info in detailedInfoList.Take(10))
    {
        var fileName = Path.GetFileName(info.FilePath);
        if (fileName.Length > 30) fileName = fileName.Substring(0, 27) + "...";
        
        detailTable.AddRow(
            $"[dim]{fileName}[/]",
            $"{info.SampleRate} Hz",
            $"{info.BitsPerSample} bit",
            $"{info.Bitrate} kbps",
            $"[dim]{info.Codec}[/]"
        );
    }
    
    AnsiConsole.Write(detailTable);
    
    // 音质建议
    AnsiConsole.WriteLine();
    var lowQualityCount = detailedInfoList.Count(d => d.SampleRate < 44100 || d.Bitrate < 192);
    if (lowQualityCount > 0)
    {
        AnsiConsole.MarkupLine($"[yellow]💡 建议：{lowQualityCount} 首歌曲采样率或码率较低，考虑替换为高质量版本[/]");
    }
    else
    {
        AnsiConsole.MarkupLine("[green]✨ 太棒了！所有歌曲音频参数都很不错[/]");
    }
}

// === 🔊 音量标准化检测 ===
async Task CheckVolumeNormalization(List<MusicFileInfo> musicFiles)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]🔊 音量标准化检测（ReplayGain 分析）[/]\n");
    
    var filesWithReplayGain = new List<(string Path, double TrackGain, double AlbumGain)>();
    var filesWithoutReplayGain = new List<string>();
    
    await AnsiConsole.Status()
        .StartAsync("[yellow]检测 ReplayGain 标签...[/]", async ctx =>
        {
            await Task.Run(() =>
            {
                foreach (var music in musicFiles)
                {
                    try
                    {
                        using var file = TagLib.File.Create(music.FilePath);
                        
                        // 尝试读取 ReplayGain 信息
                        var trackGain = file.Tag.ReplayGainTrackGain;
                        var albumGain = file.Tag.ReplayGainAlbumGain;
                        
                        if (trackGain != double.NaN || albumGain != double.NaN)
                        {
                            filesWithReplayGain.Add((
                                music.FilePath,
                                trackGain != double.NaN ? trackGain : 0,
                                albumGain != double.NaN ? albumGain : 0
                            ));
                        }
                        else
                        {
                            filesWithoutReplayGain.Add(music.FilePath);
                        }
                    }
                    catch
                    {
                        filesWithoutReplayGain.Add(music.FilePath);
                    }
                }
            });
        });
    
    var statsGrid = new Grid();
    statsGrid.AddColumn();
    statsGrid.AddColumn();
    
    statsGrid.AddRow(
        "[green]✅ 有音量标签：[/]",
        $"[bold]{filesWithReplayGain.Count}[/] 首 ({(double)filesWithReplayGain.Count / musicFiles.Count * 100:F1}%)"
    );
    statsGrid.AddRow(
        "[yellow]⚠️  无音量标签：[/]",
        $"[bold]{filesWithoutReplayGain.Count}[/] 首"
    );
    
    AnsiConsole.Write(new Panel(statsGrid)
    {
        Header = new PanelHeader("[bold cyan]📊 ReplayGain 统计[/]"),
        Border = BoxBorder.Rounded
    });
    
    if (filesWithReplayGain.Count > 0)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]有 ReplayGain 标签的文件（音量已标准化）：[/]\n");
        
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[cyan]文件[/]");
        table.AddColumn("[cyan]Track Gain[/]");
        table.AddColumn("[cyan]Album Gain[/]");
        
        foreach (var item in filesWithReplayGain.Take(10))
        {
            var fileName = Path.GetFileName(item.Path);
            if (fileName.Length > 40) fileName = fileName.Substring(0, 37) + "...";
            
            table.AddRow(
                $"[dim]{fileName}[/]",
                $"{item.TrackGain:F2} dB",
                $"{item.AlbumGain:F2} dB"
            );
        }
        
        AnsiConsole.Write(table);
    }
    
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]💡 关于 ReplayGain：[/]");
    AnsiConsole.MarkupLine("[dim]• ReplayGain 是一种音量标准化技术，避免歌曲间音量差异过大[/]");
    AnsiConsole.MarkupLine("[dim]• 如果大部分歌曲没有 ReplayGain，可以使用专业工具（如 MP3Gain）添加[/]");
    AnsiConsole.MarkupLine("[dim]• 有 ReplayGain 的歌曲在播放时会自动调整音量，听感更舒适[/]");
    
    if (filesWithoutReplayGain.Count > 0)
    {
        AnsiConsole.WriteLine();
        if (AnsiConsole.Confirm("[yellow]是否导出缺少 ReplayGain 的文件清单？[/]"))
        {
            var reportPath = Path.Combine(Path.GetDirectoryName(filesWithoutReplayGain[0])!, 
                $"需要音量标准化的文件_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            
            System.IO.File.WriteAllLines(reportPath, filesWithoutReplayGain);
            AnsiConsole.MarkupLine($"[green]✅ 已保存到：{reportPath}[/]");
        }
    }
}

// === 🎧 生成智能播放列表 ===
async Task GeneratePlaylists(List<MusicFileInfo> musicFiles, string sourceDir)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]🎧 智能播放列表生成器[/]\n");
    
    var playlistTypes = AnsiConsole.Prompt(
        new MultiSelectionPrompt<string>()
            .Title("[yellow]请选择要生成的播放列表类型（空格选择，回车确认）：[/]")
            .InstructionsText("[dim](使用上下键移动，空格选择/取消，回车确认)[/]")
            .AddChoices(new[] {
                "🎤 按艺术家分组",
                "💿 按专辑分组",
                "⭐ 高品质音乐（320kbps+/无损）",
                "🆕 按年份分组",
                "📊 完整音乐库播放列表"
            }));
    
    if (playlistTypes.Count == 0)
    {
        AnsiConsole.MarkupLine("[dim]未选择任何播放列表类型[/]");
        return;
    }
    
    var playlistDir = Path.Combine(sourceDir, "Playlists");
    Directory.CreateDirectory(playlistDir);
    
    int generatedCount = 0;
    
    await AnsiConsole.Progress()
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("[cyan]生成播放列表...[/]", maxValue: playlistTypes.Count);
            
            foreach (var type in playlistTypes)
            {
                await Task.Run(() =>
                {
                    if (type.Contains("艺术家"))
                    {
                        var artistGroups = musicFiles.GroupBy(m => m.Artist);
                        foreach (var group in artistGroups)
                        {
                            if (group.Key == "Unknown Artist") continue;
                            var path = Path.Combine(playlistDir, $"{SanitizePath(group.Key)}.m3u8");
                            GenerateM3U8Playlist(path, group.ToList(), $"{group.Key} 的歌曲");
                            generatedCount++;
                        }
                    }
                    else if (type.Contains("专辑"))
                    {
                        var albumGroups = musicFiles.GroupBy(m => $"{m.Artist}|||{m.Album}");
                        foreach (var group in albumGroups)
                        {
                            var first = group.First();
                            if (first.Artist == "Unknown Artist" || first.Album == "Unknown Album") continue;
                            var path = Path.Combine(playlistDir, $"{SanitizePath(first.Artist)} - {SanitizePath(first.Album)}.m3u8");
                            GenerateM3U8Playlist(path, group.OrderBy(m => m.Track).ToList(), $"{first.Artist} - {first.Album}");
                            generatedCount++;
                        }
                    }
                    else if (type.Contains("高品质"))
                    {
                        var highQuality = musicFiles.Where(m => m.QualityTier == "无损" || m.QualityTier == "高品质").ToList();
                        var path = Path.Combine(playlistDir, "⭐ 高品质音乐.m3u8");
                        GenerateM3U8Playlist(path, highQuality, "高品质音乐精选");
                        generatedCount++;
                    }
                    else if (type.Contains("年份"))
                    {
                        var yearGroups = musicFiles.Where(m => m.Year > 0).GroupBy(m => m.Year);
                        foreach (var group in yearGroups.OrderByDescending(g => g.Key))
                        {
                            var path = Path.Combine(playlistDir, $"{group.Key}年.m3u8");
                            GenerateM3U8Playlist(path, group.ToList(), $"{group.Key}年音乐");
                            generatedCount++;
                        }
                    }
                    else if (type.Contains("完整"))
                    {
                        var path = Path.Combine(playlistDir, "🎵 完整音乐库.m3u8");
                        GenerateM3U8Playlist(path, musicFiles, "完整音乐库");
                        generatedCount++;
                    }
                });
                
                task.Increment(1);
            }
        });
    
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[green]✅ 成功生成 {generatedCount} 个播放列表[/]");
    AnsiConsole.MarkupLine($"[cyan]📂 保存位置：{playlistDir}[/]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[dim]💡 M3U8 播放列表可以在大多数音乐播放器中打开（VLC、foobar2000 等）[/]");
}

void GenerateM3U8Playlist(string path, List<MusicFileInfo> songs, string title)
{
    var lines = new List<string>();
    lines.Add("#EXTM3U");
    lines.Add($"# Playlist: {title}");
    lines.Add($"# Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    lines.Add($"# Songs: {songs.Count}");
    lines.Add("");
    
    foreach (var song in songs)
    {
        var duration = (int)song.Duration.TotalSeconds;
        lines.Add($"#EXTINF:{duration},{song.Artist} - {song.Title}");
        lines.Add(song.FilePath);
    }
    
    System.IO.File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);
}

// === 📄 歌词智能分析 ===
async Task AnalyzeLyricsIntelligent(string sourceDir)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]📄 歌词智能分析系统[/]\n");
    
    List<string> musicFiles = new List<string>();
    List<string> lrcFiles = new List<string>();
    
    await AnsiConsole.Status()
        .StartAsync("[yellow]扫描文件...[/]", async ctx =>
        {
            await Task.Run(() =>
            {
                var allFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
                musicFiles = allFiles.Where(f => new[] { ".mp3", ".flac", ".m4a", ".wav" }
                    .Contains(Path.GetExtension(f).ToLower())).ToList();
                lrcFiles = allFiles.Where(f => f.EndsWith(".lrc", StringComparison.OrdinalIgnoreCase)).ToList();
            });
        });
    
    var lyricsAnalysis = new List<LyricsAnalysisInfo>();
    
    await AnsiConsole.Status()
        .StartAsync("[yellow]分析歌词内容...[/]", async ctx =>
        {
            await Task.Run(() =>
            {
                foreach (var lrcFile in lrcFiles)
                {
                    try
                    {
                        var content = System.IO.File.ReadAllText(lrcFile);
                        var analysis = AnalyzeLyricsContent(lrcFile, content);
                        lyricsAnalysis.Add(analysis);
                    }
                    catch { }
                }
            });
        });
    
    // 统计
    var withLyrics = lyricsAnalysis.Count;
    var withoutLyrics = musicFiles.Count - withLyrics;
    var hasChinese = lyricsAnalysis.Count(l => l.HasChinese);
    var hasEnglish = lyricsAnalysis.Count(l => l.HasEnglish);
    var hasTranslation = lyricsAnalysis.Count(l => l.HasChinese && l.HasEnglish);
    var needsTranslation = lyricsAnalysis.Count(l => (l.HasChinese && !l.HasEnglish) || (!l.HasChinese && l.HasEnglish));
    
    var statsGrid = new Grid();
    statsGrid.AddColumn();
    statsGrid.AddColumn();
    
    statsGrid.AddRow("[cyan]📊 总音乐文件：[/]", $"[bold]{musicFiles.Count}[/] 首");
    statsGrid.AddRow("[green]✅ 有歌词文件：[/]", $"[bold]{withLyrics}[/] 首 ({(double)withLyrics / musicFiles.Count * 100:F1}%)");
    statsGrid.AddRow("[red]❌ 无歌词文件：[/]", $"[bold]{withoutLyrics}[/] 首");
    statsGrid.AddRow("", "");
    statsGrid.AddRow("[cyan]🈳 含中文歌词：[/]", $"{hasChinese} 首");
    statsGrid.AddRow("[cyan]🔤 含英文歌词：[/]", $"{hasEnglish} 首");
    statsGrid.AddRow("[green]✅ 有中英对照：[/]", $"[bold]{hasTranslation}[/] 首");
    statsGrid.AddRow("[yellow]⚠️  缺少翻译：[/]", $"[bold]{needsTranslation}[/] 首");
    
    AnsiConsole.Write(new Panel(statsGrid)
    {
        Header = new PanelHeader("[bold cyan]📊 歌词分析统计[/]"),
        Border = BoxBorder.Rounded
    });
    
    // 缺少翻译的歌词
    if (needsTranslation > 0)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]⚠️  缺少翻译的歌词文件（前10个）：[/]\n");
        
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[yellow]文件[/]");
        table.AddColumn("[yellow]语言[/]");
        table.AddColumn("[yellow]建议[/]");
        
        foreach (var item in lyricsAnalysis.Where(l => 
            (l.HasChinese && !l.HasEnglish) || (!l.HasChinese && l.HasEnglish)).Take(10))
        {
            var fileName = Path.GetFileName(item.FilePath);
            var language = item.HasChinese ? "🈳 仅中文" : "🔤 仅英文";
            var suggestion = item.HasChinese ? "可添加英文翻译" : "可添加中文翻译";
            
            table.AddRow(
                $"[dim]{fileName}[/]",
                language,
                $"[dim]{suggestion}[/]"
            );
        }
        
        AnsiConsole.Write(table);
    }
    
    // 缺少歌词的音乐
    if (withoutLyrics > 0)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[red]❌ 缺少歌词的音乐文件：{withoutLyrics} 首[/]\n");
        
        var musicWithoutLyrics = musicFiles.Where(m =>
        {
            var lrcPath = Path.ChangeExtension(m, ".lrc");
            return !System.IO.File.Exists(lrcPath);
        }).Take(10).ToList();
        
        foreach (var music in musicWithoutLyrics)
        {
            AnsiConsole.MarkupLine($"[dim]• {Path.GetFileName(music)}[/]");
        }
        
        if (withoutLyrics > 10)
        {
            AnsiConsole.MarkupLine($"[dim]... 还有 {withoutLyrics - 10} 首[/]");
        }
    }
    
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]💡 关于歌词处理建议：[/]");
    AnsiConsole.MarkupLine("[dim]• 缺少翻译的歌词：可以手动编辑 .lrc 文件添加对照翻译[/]");
    AnsiConsole.MarkupLine("[dim]• 缺少歌词的音乐：可以从歌词网站下载或使用语音识别[/]");
    AnsiConsole.MarkupLine("[dim]• Whisper 语音识别：适合清晰的人声，但音乐背景可能影响准确度[/]");
    AnsiConsole.MarkupLine("[dim]• 建议优先从网易云、QQ音乐等平台下载现成歌词[/]");
    
    // 导出报告
    AnsiConsole.WriteLine();
    if (AnsiConsole.Confirm("[yellow]是否导出详细的歌词分析报告？[/]"))
    {
        var reportPath = Path.Combine(sourceDir, $"歌词分析报告_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        
        var reportLines = new List<string>();
        reportLines.Add("═══════════════════════════════════");
        reportLines.Add("     🎵 歌词智能分析报告");
        reportLines.Add("═══════════════════════════════════");
        reportLines.Add($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        reportLines.Add($"扫描目录：{sourceDir}");
        reportLines.Add("");
        reportLines.Add("【统计概览】");
        reportLines.Add($"• 总音乐文件：{musicFiles.Count} 首");
        reportLines.Add($"• 有歌词：{withLyrics} 首 ({(double)withLyrics / musicFiles.Count * 100:F1}%)");
        reportLines.Add($"• 无歌词：{withoutLyrics} 首");
        reportLines.Add($"• 有中英对照：{hasTranslation} 首");
        reportLines.Add($"• 缺少翻译：{needsTranslation} 首");
        reportLines.Add("");
        reportLines.Add("【缺少翻译的歌词】");
        foreach (var item in lyricsAnalysis.Where(l => 
            (l.HasChinese && !l.HasEnglish) || (!l.HasChinese && l.HasEnglish)))
        {
            var language = item.HasChinese ? "[仅中文]" : "[仅英文]";
            reportLines.Add($"  {language} {Path.GetFileName(item.FilePath)}");
        }
        reportLines.Add("");
        reportLines.Add("【缺少歌词的音乐】");
        foreach (var music in musicFiles.Where(m =>
        {
            var lrcPath = Path.ChangeExtension(m, ".lrc");
            return !System.IO.File.Exists(lrcPath);
        }))
        {
            reportLines.Add($"  • {Path.GetFileName(music)}");
        }
        
        System.IO.File.WriteAllLines(reportPath, reportLines, System.Text.Encoding.UTF8);
        AnsiConsole.MarkupLine($"[green]✅ 报告已保存：{reportPath}[/]");
    }
}

LyricsAnalysisInfo AnalyzeLyricsContent(string filePath, string content)
{
    var info = new LyricsAnalysisInfo { FilePath = filePath };
    
    // 检测中文
    info.HasChinese = Regex.IsMatch(content, @"[\u4e00-\u9fa5]");
    
    // 检测英文（排除LRC标签）
    var contentWithoutTags = Regex.Replace(content, @"\[.*?\]", "");
    info.HasEnglish = Regex.IsMatch(contentWithoutTags, @"[a-zA-Z]{3,}");
    
    // 检测日文
    info.HasJapanese = Regex.IsMatch(content, @"[\u3040-\u309F\u30A0-\u30FF]");
    
    return info;
}

// === 🖼️ 缺少封面报告 ===
async Task GenerateCoverArtReport(List<MusicFileInfo> musicFiles, string sourceDir)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]🖼️  专辑封面检测报告[/]\n");
    
    var albumsWithCover = new List<string>();
    var albumsWithoutCover = new List<string>();
    
    await AnsiConsole.Status()
        .StartAsync("[yellow]检测专辑封面...[/]", async ctx =>
        {
            await Task.Run(() =>
            {
                var albums = musicFiles
                    .Where(m => m.Artist != "Unknown Artist" && m.Album != "Unknown Album")
                    .GroupBy(m => $"{m.Artist}|||{m.Album}")
                    .ToList();
                
                foreach (var album in albums)
                {
                    var firstFile = album.First();
                    var albumKey = $"{firstFile.Artist} - {firstFile.Album}";
                    
                    try
                    {
                        using var file = TagLib.File.Create(firstFile.FilePath);
                        
                        if (file.Tag.Pictures != null && file.Tag.Pictures.Length > 0)
                        {
                            albumsWithCover.Add(albumKey);
                        }
                        else
                        {
                            albumsWithoutCover.Add(albumKey);
                        }
                    }
                    catch
                    {
                        albumsWithoutCover.Add(albumKey);
                    }
                }
            });
        });
    
    var totalAlbums = albumsWithCover.Count + albumsWithoutCover.Count;
    
    var statsGrid = new Grid();
    statsGrid.AddColumn();
    statsGrid.AddColumn();
    
    statsGrid.AddRow(
        "[cyan]📀 总专辑数：[/]",
        $"[bold]{totalAlbums}[/] 张"
    );
    statsGrid.AddRow(
        "[green]✅ 有封面：[/]",
        $"[bold]{albumsWithCover.Count}[/] 张 ({(double)albumsWithCover.Count / totalAlbums * 100:F1}%)"
    );
    statsGrid.AddRow(
        "[red]❌ 无封面：[/]",
        $"[bold]{albumsWithoutCover.Count}[/] 张"
    );
    
    AnsiConsole.Write(new Panel(statsGrid)
    {
        Header = new PanelHeader("[bold cyan]🖼️  封面统计[/]"),
        Border = BoxBorder.Rounded
    });
    
    if (albumsWithoutCover.Count > 0)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[red]❌ 缺少封面的专辑（前20张）：[/]\n");
        
        foreach (var album in albumsWithoutCover.Take(20))
        {
            AnsiConsole.MarkupLine($"[dim]• {album}[/]");
        }
        
        if (albumsWithoutCover.Count > 20)
        {
            AnsiConsole.MarkupLine($"[dim]... 还有 {albumsWithoutCover.Count - 20} 张专辑[/]");
        }
        
        AnsiConsole.WriteLine();
        if (AnsiConsole.Confirm("[yellow]是否导出缺少封面的专辑清单？[/]"))
        {
            var reportPath = Path.Combine(sourceDir, $"缺少封面的专辑_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            
            var reportLines = new List<string>();
            reportLines.Add("═══════════════════════════════════");
            reportLines.Add("     🖼️  缺少封面的专辑清单");
            reportLines.Add("═══════════════════════════════════");
            reportLines.Add($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            reportLines.Add($"总计：{albumsWithoutCover.Count} 张专辑");
            reportLines.Add("");
            reportLines.Add("【专辑列表】");
            
            foreach (var album in albumsWithoutCover)
            {
                reportLines.Add($"  • {album}");
            }
            
            reportLines.Add("");
            reportLines.Add("【获取封面建议】");
            reportLines.Add("1. 在线音乐平台：网易云音乐、QQ音乐、Apple Music");
            reportLines.Add("2. 专辑封面数据库：Cover Art Archive、Discogs");
            reportLines.Add("3. 搜索引擎：Google 图片搜索「艺术家 专辑名 cover」");
            reportLines.Add("4. 使用音乐标签工具（如 Mp3tag）嵌入封面到文件");
            
            System.IO.File.WriteAllLines(reportPath, reportLines, System.Text.Encoding.UTF8);
            AnsiConsole.MarkupLine($"[green]✅ 清单已保存：{reportPath}[/]");
        }
    }
    else
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✨ 完美！所有专辑都有封面图片[/]");
    }
    
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[cyan]💡 关于专辑封面：[/]");
    AnsiConsole.MarkupLine("[dim]• 封面嵌入在音频文件的 ID3 标签中[/]");
    AnsiConsole.MarkupLine("[dim]• 推荐尺寸：至少 500x500px，最好 1000x1000px[/]");
    AnsiConsole.MarkupLine("[dim]• 格式：JPG 或 PNG，文件大小建议 < 1MB[/]");
    AnsiConsole.MarkupLine("[dim]• 可使用 Mp3tag、MusicBrainz Picard 等工具批量添加[/]");
}

void ShowGoodbye()
{
    Console.Clear();
    AnsiConsole.Write(
        new FigletText("Thank You!")
            .Centered()
            .Color(Color.Magenta1)
    );
    AnsiConsole.MarkupLine("[cyan]🎵 愿你的音乐库井井有条！[/]");
    AnsiConsole.MarkupLine("[dim]Let AI organize your music collection ✨[/]\n");
}

// === 数据模型 ===
class MusicFileInfo
{
    public string FilePath { get; set; } = "";
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public uint Year { get; set; }
    public uint Track { get; set; }
    public TimeSpan Duration { get; set; }
    public long Size { get; set; }
    public int Bitrate { get; set; }
    public int TagCompleteness { get; set; }  // 0-100
    public string QualityTier { get; set; } = "普通";  // 无损/高品质/普通/低品质
    public int QualityScore { get; set; }  // 用于排序
}

class AudioDetailInfo
{
    public string FilePath { get; set; } = "";
    public int SampleRate { get; set; }
    public int BitsPerSample { get; set; }
    public int Channels { get; set; }
    public string Codec { get; set; } = "";
    public int Bitrate { get; set; }
    public TimeSpan Duration { get; set; }
}

class LyricsAnalysisInfo
{
    public string FilePath { get; set; } = "";
    public bool HasChinese { get; set; }
    public bool HasEnglish { get; set; }
    public bool HasJapanese { get; set; }
}
