#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0-windows
#:property UseWindowsForms=true
#:property OutputType=WinExe
#:property PublishTrimmed=false
#:package Microsoft.EntityFrameworkCore.Sqlite@9.0.0
#:package Spectre.Console@0.49.1

using System.Diagnostics;
using System.Drawing;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using Spectre.Console;
using SpectreColor = Spectre.Console.Color;
using DrawingColor = System.Drawing.Color;
using SpectrePanel = Spectre.Console.Panel;

// ================================================
// SmartClipboard v5 - 极简控制台版本
// ================================================

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        // 检查单实例
        using var mutex = new Mutex(true, "SmartClipboard_v5", out var isNewInstance);
        if (!isNewInstance)
        {
            AnsiConsole.MarkupLine("[red]SmartClipboard 已在运行中！[/]");
            return 0;
        }

        // 全局异常处理
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            Utils.LogError($"FATAL: {ex?.Message}\n{ex?.StackTrace}");
        };

        // 初始化数据库
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartClipboard", "data.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var db = new Database(dbPath);

        // 处理命令行参数
        if (args.Length > 0)
        {
            return Commands.Handle(args, db);
        }

        // 运行主程序
        return App.Run(db);
    }
}


// ================================================
// 命令处理
// ================================================
static class Commands
{
    public static int Handle(string[] args, Database db)
    {
        return args[0].ToLower() switch
        {
            "config" => ShowConfig(db),
            "history" => ShowHistory(db),
            "search" => SearchHistory(db, args.Length > 1 ? args[1] : ""),
            "clear" => ClearHistory(db),
            "autostart" => ToggleAutoStart(),
            "help" => ShowHelp(),
            _ => ShowHelp()
        };
    }

    static int ShowConfig(Database db)
    {
        var config = db.GetConfig();
        
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[yellow]配置项[/]")
            .AddColumn("[cyan]当前值[/]");
        
        table.AddRow("自动保存", config.AutoSave ? "✓ 启用" : "✗ 禁用");
        table.AddRow("敏感信息过滤", config.FilterSensitive ? "✓ 启用" : "✗ 禁用");
        table.AddRow("AI 分析", config.EnableAI ? $"✓ {config.AIProvider}" : "✗ 禁用");
        table.AddRow("Matrix 同步", config.EnableMatrix ? "✓ 启用" : "✗ 禁用");
        table.AddRow("开机自启", AutoStartManager.IsEnabled() ? "✓ 启用" : "✗ 禁用");
        
        AnsiConsole.Write(table);
        
        if (AnsiConsole.Confirm("\n[yellow]是否修改配置?[/]", false))
        {
            ConfigureInteractive(db);
        }
        
        return 0;
    }

    public static void ConfigureInteractive(Database db)
    {
        var config = db.GetConfig();
        
        AnsiConsole.MarkupLine("\n[bold cyan]━━━━━ SmartClipboard 配置向导 ━━━━━[/]\n");
        
        config.AutoSave = AnsiConsole.Confirm("自动保存所有剪贴板内容?", config.AutoSave);
        config.FilterSensitive = AnsiConsole.Confirm("启用敏感信息过滤 (密码、密钥)?", config.FilterSensitive);
        
        config.EnableAI = AnsiConsole.Confirm("\n启用 AI 智能分析?", config.EnableAI);
        if (config.EnableAI)
        {
            config.AIProvider = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("选择 AI 服务商:")
                    .AddChoices("openai", "deepseek", "ollama", "custom"));
            
            config.AIApiUrl = config.AIProvider switch
            {
                "openai" => "https://api.openai.com/v1",
                "deepseek" => "https://api.deepseek.com/v1",
                "ollama" => "http://localhost:11434",
                _ => AnsiConsole.Ask<string>("API 地址:", config.AIApiUrl)
            };
            
            config.AIModel = config.AIProvider switch
            {
                "openai" => "gpt-4o-mini",
                "deepseek" => "deepseek-chat",
                "ollama" => "qwen2.5:3b",
                _ => AnsiConsole.Ask<string>("模型名称:", config.AIModel)
            };
            
            config.AIApiKey = AnsiConsole.Prompt(
                new TextPrompt<string>($"API Key [dim](可选)[/]:")
                    .AllowEmpty()
                    .Secret());
        }
        
        config.EnableMatrix = AnsiConsole.Confirm("\n启用 Matrix 房间同步?", config.EnableMatrix);
        if (config.EnableMatrix)
        {
            config.MatrixHomeserver = AnsiConsole.Ask("Matrix 服务器:", config.MatrixHomeserver);
            config.MatrixUserId = AnsiConsole.Ask("用户 ID:", config.MatrixUserId);
            config.MatrixAccessToken = AnsiConsole.Prompt(
                new TextPrompt<string>("Access Token:")
                    .Secret());
            config.MatrixRoomId = AnsiConsole.Ask("房间 ID:", config.MatrixRoomId);
        }
        
        db.SaveConfig(config);
        AnsiConsole.MarkupLine("\n[green]✓ 配置已保存[/]");
    }

    static int ShowHistory(Database db)
    {
        var items = db.GetRecent(50);
        
        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]暂无历史记录[/]");
            return 0;
        }
        
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[cyan]时间[/]")
            .AddColumn("[yellow]分类[/]")
            .AddColumn("[white]摘要[/]")
            .AddColumn("[dim]长度[/]");
        
        foreach (var item in items)
        {
            table.AddRow(
                item.CreatedAt.ToString("MM-dd HH:mm"),
                item.Category,
                item.Summary.Length > 50 ? item.Summary[..50] + "..." : item.Summary,
                item.RawContent.Length.ToString());
        }
        
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]共 {items.Count} 条记录，今日: {db.GetTodayCount()}[/]");
        
        return 0;
    }

    static int SearchHistory(Database db, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            keyword = AnsiConsole.Ask<string>("搜索关键词:");
        }
        
        var items = db.Search(keyword);
        
        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]未找到包含 '{keyword}' 的记录[/]");
            return 0;
        }
        
        foreach (var item in items)
        {
            var panel = new SpectrePanel($"[dim]{item.CreatedAt:yyyy-MM-dd HH:mm:ss}[/]\n{item.RawContent}")
                .Header($"[cyan]{item.Category}[/]")
                .BorderColor(SpectreColor.Blue);
            AnsiConsole.Write(panel);
        }
        
        AnsiConsole.MarkupLine($"\n[green]找到 {items.Count} 条匹配记录[/]");
        return 0;
    }

    static int ClearHistory(Database db)
    {
        if (AnsiConsole.Confirm("[red]确定清空所有历史记录?[/]", false))
        {
            db.ClearAll();
            AnsiConsole.MarkupLine("[green]✓ 已清空[/]");
        }
        return 0;
    }

    static int ToggleAutoStart()
    {
        var enabled = AutoStartManager.Toggle();
        AnsiConsole.MarkupLine(enabled 
            ? "[green]✓ 已启用开机自启[/]" 
            : "[yellow]✗ 已禁用开机自启[/]");
        return 0;
    }

    static int ShowHelp()
    {
        AnsiConsole.Write(
            new FigletText("SmartClipboard")
                .LeftJustified()
                .Color(SpectreColor.Cyan1));
        
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn("")
            .AddColumn("");
        
        table.AddRow("[cyan]config[/]", "配置管理 (AI、Matrix 等)");
        table.AddRow("[cyan]history[/]", "查看最近 50 条记录");
        table.AddRow("[cyan]search <关键词>[/]", "搜索历史记录");
        table.AddRow("[cyan]clear[/]", "清空所有历史");
        table.AddRow("[cyan]autostart[/]", "切换开机自启");
        table.AddRow("[cyan]help[/]", "显示此帮助");
        table.AddRow("", "");
        table.AddRow("[dim]无参数[/]", "启动后台监听 (托盘模式)");
        
        AnsiConsole.Write(table);
        
        return 0;
    }
}

// ================================================
// 主应用逻辑
// ================================================
static class App
{
    public static int Run(Database db)
    {
        var config = db.GetConfig();
        
        if (config.IsFirstRun)
        {
            config.IsFirstRun = false;
            db.SaveConfig(config);
            Utils.LogInfo("首次运行，使用默认配置。使用 'config' 命令进行配置。");
        }
        
        var cts = new CancellationTokenSource();
        var aiService = new AIService(db);
        var matrixService = new MatrixService(db);
        var clipboardService = new ClipboardService(db, aiService, matrixService, cts.Token);
        
        Win32.ShowWindow(Win32.GetConsoleWindow(), Win32.SW_MINIMIZE);
        
        using var trayIcon = UI.CreateTrayIcon(db, clipboardService, cts);
        
        _ = Task.Run(() => clipboardService.StartAsync());
        
        Utils.LogInfo($"SmartClipboard 已启动 (PID: {Process.GetCurrentProcess().Id})");
        
        Application.Run();
        
        cts.Cancel();
        return 0;
    }
}

// ================================================
// UI 组件
// ================================================
static class UI
{
    public static NotifyIcon CreateTrayIcon(Database db, ClipboardService clipboard, CancellationTokenSource cts)
    {
        var icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "SmartClipboard - 运行中"
        };
        
        var menu = new ContextMenuStrip();
        
        var statusItem = new ToolStripMenuItem($"📊 已捕获: {clipboard.CapturedCount} 条");
        statusItem.Enabled = false;
        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
        
        menu.Items.Add("💻 打开控制台", null, (s, e) =>
        {
            var hwnd = Win32.GetConsoleWindow();
            if (hwnd != IntPtr.Zero)
            {
                Win32.ShowWindow(hwnd, Win32.SW_RESTORE);
                Win32.SetForegroundWindow(hwnd);
                Win32.BringWindowToTop(hwnd);
            }
        });
        
        menu.Items.Add("📜 查看历史", null, (s, e) =>
        {
            var hwnd = Win32.GetConsoleWindow();
            if (hwnd != IntPtr.Zero)
            {
                Win32.ShowWindow(hwnd, Win32.SW_RESTORE);
                Win32.SetForegroundWindow(hwnd);
                Win32.BringWindowToTop(hwnd);
                Thread.Sleep(300);
                Task.Run(() => Commands.Handle(new[] { "history" }, db));
            }
        });
        
        menu.Items.Add("⚙️ 设置", null, (s, e) =>
        {
            var hwnd = Win32.GetConsoleWindow();
            if (hwnd != IntPtr.Zero)
            {
                Win32.ShowWindow(hwnd, Win32.SW_RESTORE);
                Win32.SetForegroundWindow(hwnd);
                Win32.BringWindowToTop(hwnd);
                Thread.Sleep(300);
                Task.Run(() => Commands.ConfigureInteractive(db));
            }
        });
        
        menu.Items.Add(new ToolStripSeparator());
        
        var pauseItem = new ToolStripMenuItem("⏸️ 暂停监听", null, (s, e) =>
        {
            clipboard.TogglePause();
            ((ToolStripMenuItem)s!).Text = clipboard.IsPaused ? "▶️ 继续监听" : "⏸️ 暂停监听";
            icon.Text = clipboard.IsPaused ? "SmartClipboard - 已暂停" : "SmartClipboard - 运行中";
        });
        menu.Items.Add(pauseItem);
        
        menu.Items.Add("📝 查看日志", null, (s, e) =>
        {
            Process.Start(new ProcessStartInfo(Utils.GetLogPath()) { UseShellExecute = true });
        });
        
        var autoStartItem = new ToolStripMenuItem("🚀 开机自启", null, (s, e) =>
        {
            var enabled = AutoStartManager.Toggle();
            ((ToolStripMenuItem)s!).Checked = enabled;
        })
        {
            Checked = AutoStartManager.IsEnabled()
        };
        menu.Items.Add(autoStartItem);
        
        menu.Items.Add(new ToolStripSeparator());
        
        menu.Items.Add("❌ 退出", null, (s, e) =>
        {
            if (MessageBox.Show("确定退出?", "SmartClipboard", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                icon.Visible = false;
                cts.Cancel();
                Application.Exit();
            }
        });
        
        icon.ContextMenuStrip = menu;
        
        icon.DoubleClick += (s, e) =>
        {
            var hwnd = Win32.GetConsoleWindow();
            if (hwnd != IntPtr.Zero)
            {
                Win32.ShowWindow(hwnd, Win32.SW_RESTORE);
                Win32.SetForegroundWindow(hwnd);
                Win32.BringWindowToTop(hwnd);
            }
        };
        
        var timer = new System.Windows.Forms.Timer { Interval = 3000 };
        timer.Tick += (s, e) =>
        {
            statusItem.Text = $"📊 已捕获: {clipboard.CapturedCount} 条 | 今日: {db.GetTodayCount()}";
        };
        timer.Start();
        
        return icon;
    }
}

// ================================================
// 配置类
// ================================================
class Config
{
    public bool IsFirstRun { get; set; } = true;
    public bool AutoSave { get; set; } = true;
    public bool FilterSensitive { get; set; } = true;
    
    public bool EnableAI { get; set; } = false;
    public string AIProvider { get; set; } = "openai";
    public string AIApiUrl { get; set; } = "https://api.openai.com/v1";
    public string AIApiKey { get; set; } = "";
    public string AIModel { get; set; } = "gpt-4o-mini";
    
    public bool EnableMatrix { get; set; } = false;
    public string MatrixHomeserver { get; set; } = "https://matrix.org";
    public string MatrixUserId { get; set; } = "";
    public string MatrixAccessToken { get; set; } = "";
    public string MatrixRoomId { get; set; } = "";
}

// ================================================
// 数据库
// ================================================
class Database
{
    private readonly string _conn;
    
    public Database(string dbPath)
    {
        _conn = $"Data Source={dbPath}";
        InitDb();
    }
    
    private void InitDb()
    {
        using var conn = new SqliteConnection(_conn);
        conn.Open();
        
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Config (
                Key TEXT PRIMARY KEY,
                Value TEXT
            );
            
            CREATE TABLE IF NOT EXISTS History (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ContentHash TEXT UNIQUE,
                Category TEXT,
                RawContent TEXT,
                Summary TEXT,
                Importance INTEGER DEFAULT 3,
                CreatedAt DATETIME
            );
            
            CREATE INDEX IF NOT EXISTS idx_date ON History(CreatedAt DESC);
            CREATE INDEX IF NOT EXISTS idx_hash ON History(ContentHash);
        ";
        cmd.ExecuteNonQuery();
    }
    
    public Config GetConfig()
    {
        using var conn = new SqliteConnection(_conn);
        conn.Open();
        
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Value FROM Config WHERE Key = 'config'";
        var json = cmd.ExecuteScalar() as string;
        
        return string.IsNullOrEmpty(json) 
            ? new Config() 
            : JsonSerializer.Deserialize<Config>(json) ?? new Config();
    }
    
    public void SaveConfig(Config config)
    {
        using var conn = new SqliteConnection(_conn);
        conn.Open();
        
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO Config (Key, Value) VALUES ('config', @json)";
        cmd.Parameters.AddWithValue("@json", JsonSerializer.Serialize(config));
        cmd.ExecuteNonQuery();
    }
    
    public bool Exists(string hash)
    {
        using var conn = new SqliteConnection(_conn);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM History WHERE ContentHash = @hash";
        cmd.Parameters.AddWithValue("@hash", hash);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
    
    public void Insert(ClipboardEntry entry)
    {
        using var conn = new SqliteConnection(_conn);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO History 
            (ContentHash, Category, RawContent, Summary, Importance, CreatedAt)
            VALUES (@hash, @cat, @content, @summary, @imp, @date)";
        cmd.Parameters.AddWithValue("@hash", entry.ContentHash);
        cmd.Parameters.AddWithValue("@cat", entry.Category);
        cmd.Parameters.AddWithValue("@content", entry.RawContent);
        cmd.Parameters.AddWithValue("@summary", entry.Summary);
        cmd.Parameters.AddWithValue("@imp", entry.Importance);
        cmd.Parameters.AddWithValue("@date", entry.CreatedAt);
        cmd.ExecuteNonQuery();
    }
    
    public List<ClipboardEntry> GetRecent(int count)
    {
        using var conn = new SqliteConnection(_conn);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM History ORDER BY CreatedAt DESC LIMIT @count";
        cmd.Parameters.AddWithValue("@count", count);
        return ReadEntries(cmd);
    }
    
    public List<ClipboardEntry> Search(string keyword)
    {
        using var conn = new SqliteConnection(_conn);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT * FROM History 
            WHERE RawContent LIKE @kw OR Summary LIKE @kw OR Category LIKE @kw
            ORDER BY CreatedAt DESC LIMIT 100";
        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
        return ReadEntries(cmd);
    }
    
    public void ClearAll()
    {
        using var conn = new SqliteConnection(_conn);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM History";
        cmd.ExecuteNonQuery();
    }
    
    public int GetTodayCount()
    {
        using var conn = new SqliteConnection(_conn);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM History WHERE DATE(CreatedAt) = DATE('now')";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
    
    private List<ClipboardEntry> ReadEntries(SqliteCommand cmd)
    {
        var list = new List<ClipboardEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ClipboardEntry
            {
                Id = reader.GetInt32(0),
                ContentHash = reader.GetString(1),
                Category = reader.GetString(2),
                RawContent = reader.GetString(3),
                Summary = reader.GetString(4),
                Importance = reader.GetInt32(5),
                CreatedAt = reader.GetDateTime(6)
            });
        }
        return list;
    }
}

class ClipboardEntry
{
    public int Id { get; set; }
    public string ContentHash { get; set; } = "";
    public string Category { get; set; } = "";
    public string RawContent { get; set; } = "";
    public string Summary { get; set; } = "";
    public int Importance { get; set; } = 3;
    public DateTime CreatedAt { get; set; }
}

// ================================================
// AI 服务
// ================================================
class AIService
{
    private readonly Database _db;
    private readonly HttpClient _client = new();
    
    public AIService(Database db)
    {
        _db = db;
    }
    
    public async Task<(string category, string summary, int importance)> AnalyzeAsync(string text)
    {
        var config = _db.GetConfig();
        
        if (!config.EnableAI || string.IsNullOrWhiteSpace(text))
        {
            return (ClassifyBasic(text), 
                    text.Length > 80 ? text[..80] + "..." : text, 
                    3);
        }
        
        try
        {
            var prompt = "分析剪贴板内容，返回 JSON: " +
                "{\"category\":\"分类(代码/链接/文档/数据/其他)\",\"summary\":\"摘要(50字内)\",\"importance\":1-5}\n\n" +
                text[..Math.Min(1000, text.Length)];
            
            var request = new
            {
                model = config.AIModel,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.3
            };
            
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, 
                config.AIProvider == "ollama" 
                    ? $"{config.AIApiUrl}/api/chat" 
                    : $"{config.AIApiUrl}/chat/completions")
            {
                Content = JsonContent.Create(request)
            };
            
            if (!string.IsNullOrEmpty(config.AIApiKey))
            {
                httpRequest.Headers.Add("Authorization", $"Bearer {config.AIApiKey}");
            }
            
            var response = await _client.SendAsync(httpRequest);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                var content = config.AIProvider == "ollama"
                    ? result.GetProperty("message").GetProperty("content").GetString()
                    : result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                
                if (content != null)
                {
                    var analysis = JsonSerializer.Deserialize<AIResult>(content);
                    if (analysis != null)
                        return (analysis.Category, analysis.Summary, analysis.Importance);
                }
            }
        }
        catch (Exception ex)
        {
            Utils.LogError($"AI分析失败: {ex.Message}");
        }
        
        return (ClassifyBasic(text), text[..Math.Min(80, text.Length)], 3);
    }
    
    private string ClassifyBasic(string text)
    {
        if (Regex.IsMatch(text, @"^(https?|ftp)://")) return "🔗 链接";
        if (Regex.IsMatch(text, @"(class|function|def|const|var)\s+")) return "💻 代码";
        if (text.Contains('\n') && text.Length > 200) return "📄 文档";
        if (Regex.IsMatch(text, @"^\d+$")) return "🔢 数字";
        return "📝 文本";
    }
}

class AIResult
{
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("summary")] public string Summary { get; set; } = "";
    [JsonPropertyName("importance")] public int Importance { get; set; } = 3;
}

// ================================================
// Matrix 服务
// ================================================
class MatrixService
{
    private readonly Database _db;
    private readonly HttpClient _client = new();
    
    public MatrixService(Database db)
    {
        _db = db;
    }
    
    public async Task SendAsync(string content, string category)
    {
        var config = _db.GetConfig();
        if (!config.EnableMatrix) return;
        
        try
        {
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.MatrixAccessToken}");
            
            var txnId = Guid.NewGuid().ToString();
            var url = $"{config.MatrixHomeserver}/_matrix/client/v3/rooms/{Uri.EscapeDataString(config.MatrixRoomId)}/send/m.room.message/{txnId}";
            
            var message = new
            {
                msgtype = "m.text",
                body = $"{category}\n{content}",
                format = "org.matrix.custom.html",
                formatted_body = $"<b>{category}</b><br><pre>{content[..Math.Min(500, content.Length)]}</pre>"
            };
            
            await _client.PutAsJsonAsync(url, message);
        }
        catch (Exception ex)
        {
            Utils.LogError($"Matrix同步失败: {ex.Message}");
        }
    }
}

// ================================================
// 剪贴板服务
// ================================================
class ClipboardService
{
    private readonly Database _db;
    private readonly AIService _ai;
    private readonly MatrixService _matrix;
    private readonly CancellationToken _token;
    private string _lastHash = "";
    private ClipboardMonitorForm? _form;
    
    public int CapturedCount { get; private set; }
    public bool IsPaused { get; private set; }
    
    public ClipboardService(Database db, AIService ai, MatrixService matrix, CancellationToken token)
    {
        _db = db;
        _ai = ai;
        _matrix = matrix;
        _token = token;
    }
    
    public void TogglePause()
    {
        IsPaused = !IsPaused;
        Utils.LogInfo(IsPaused ? "监听已暂停" : "监听已恢复");
    }
    
    public async Task StartAsync()
    {
        await Task.Run(() =>
        {
            _form = new ClipboardMonitorForm(OnClipboardChanged);
            Application.Run(_form);
        });
    }
    
    private void OnClipboardChanged()
    {
        if (IsPaused) return;
        
        Task.Run(async () =>
        {
            try
            {
                if (!Clipboard.ContainsText()) return;
                
                var text = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(text) || text.Length < 3) return;
                
                var hash = Utils.ComputeHash(text);
                if (hash == _lastHash || _db.Exists(hash)) return;
                _lastHash = hash;
                
                var config = _db.GetConfig();
                
                if (config.FilterSensitive && IsSensitive(text))
                {
                    Utils.LogInfo($"过滤敏感内容");
                    return;
                }
                
                var (category, summary, importance) = await _ai.AnalyzeAsync(text);
                
                var entry = new ClipboardEntry
                {
                    ContentHash = hash,
                    Category = category,
                    RawContent = text,
                    Summary = summary,
                    Importance = importance,
                    CreatedAt = DateTime.Now
                };
                
                _db.Insert(entry);
                CapturedCount++;
                
                Utils.LogInfo($"捕获 [{category}]: {summary}");
                
                _ = _matrix.SendAsync(text, category);
            }
            catch (Exception ex)
            {
                Utils.LogError($"处理剪贴板失败: {ex.Message}");
            }
        });
    }
    
    private bool IsSensitive(string text)
    {
        var patterns = new[]
        {
            @"password\s*[:=]",
            @"BEGIN (RSA|DSA|EC) PRIVATE KEY",
            @"sk-[a-zA-Z0-9]{32,}",
            @"ghp_[a-zA-Z0-9]{36}"
        };
        
        return patterns.Any(p => Regex.IsMatch(text, p, RegexOptions.IgnoreCase));
    }
}

class ClipboardMonitorForm : Form
{
    private readonly Action _onChange;
    
    [DllImport("user32.dll")]
    private static extern IntPtr AddClipboardFormatListener(IntPtr hwnd);
    
    private const int WM_CLIPBOARDUPDATE = 0x031D;
    
    public ClipboardMonitorForm(Action onChange)
    {
        _onChange = onChange;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        Opacity = 0;
    }
    
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        AddClipboardFormatListener(Handle);
    }
    
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_CLIPBOARDUPDATE)
        {
            _onChange();
        }
        base.WndProc(ref m);
    }
}

// ================================================
// 开机自启
// ================================================
static class AutoStartManager
{
    private static readonly string AppName = "SmartClipboard";
    private static readonly string ExePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
    
    public static bool IsEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            return key?.GetValue(AppName) != null;
        }
        catch { return false; }
    }
    
    public static bool Toggle()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return false;
            
            if (IsEnabled())
            {
                key.DeleteValue(AppName);
                return false;
            }
            else
            {
                key.SetValue(AppName, $"\"{ExePath}\"");
                return true;
            }
        }
        catch { return false; }
    }
}

// ================================================
// 工具类
// ================================================
static class Utils
{
    public static string ComputeHash(string input)
    {
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(input))).ToLower();
    }

    public static string GetLogPath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartClipboard");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "app.log");
    }

    public static void LogInfo(string msg)
    {
        try
        {
            File.AppendAllText(GetLogPath(), $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        }
        catch { }
    }

    public static void LogError(string msg)
    {
        try
        {
            File.AppendAllText(GetLogPath(), $"[{DateTime.Now:HH:mm:ss}] ERROR: {msg}\n");
        }
        catch { }
    }
}

// ================================================
// Win32 API
// ================================================
static class Win32
{
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    public const int SW_MINIMIZE = 6;
    public const int SW_RESTORE = 9;
}
