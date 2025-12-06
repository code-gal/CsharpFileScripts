#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net10.0-windows
#:property UseWindowsForms=true
#:property OutputType=WinExe
#:property PublishTrimmed=false
#:property EnableWindowsTargeting=true
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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Color = System.Drawing.Color;

// ================================================
// SmartClipboard v2 - 智能剪贴板历史管理器
// ================================================
// 功能:
// 1. ✅ 事件触发式监听 (零CPU占用)
// 2. ✅ 系统托盘图标 + 右键菜单
// 3. ✅ 内嵌Web界面 (查看/审核)
// 4. ✅ 可选自启动注册
// 5. ✅ Matrix房间同步
// 6. ✅ AI智能分析 (Ollama)
// ================================================

[STAThread]
static int Main()
{
    // 检查是否已有实例运行
    using var mutex = new Mutex(true, "SmartClipboard_SingleInstance", out var isNewInstance);
if (!isNewInstance)
{
    MessageBox.Show("SmartClipboard 已在运行中！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
    return 0;
}

// 全局异常处理
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    var ex = e.ExceptionObject as Exception;
    File.AppendAllText(Helpers.GetLogPath(), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL: {ex?.Message}\n{ex?.StackTrace}\n\n");
};

// 初始化配置和数据库
var config = AppConfig.Load();
var db = new ClipboardDatabase(config.DatabasePath);
var aiService = new AIService(config);
var matrixService = new MatrixService(config);

// 启动后台服务
var cts = new CancellationTokenSource();
var clipboardService = new ClipboardService(db, config, aiService, matrixService, cts.Token);
var webService = new WebService(db, config);

// 系统托盘图标
using var trayIcon = CreateTrayIcon(clipboardService, webService, cts);

// 启动监听
_ = Task.Run(() => clipboardService.StartAsync());
_ = Task.Run(() => webService.StartAsync());

// 保持运行
Application.Run();

    cts.Cancel();
    return 0;
}

// ================================================
// 系统托盘图标
// ================================================
NotifyIcon CreateTrayIcon(ClipboardService clipboard, WebService web, CancellationTokenSource cts)
{
    var icon = new NotifyIcon
    {
        Icon = SystemIcons.Application,
        Visible = true,
        Text = "SmartClipboard - 运行中"
    };

    var menu = new ContextMenuStrip();
    
    // 状态信息
    var statusItem = new ToolStripMenuItem($"📊 已捕获: {clipboard.CapturedCount} 条");
    statusItem.Enabled = false;
    menu.Items.Add(statusItem);
    menu.Items.Add(new ToolStripSeparator());
    
    // 打开Web界面
    menu.Items.Add("🌐 打开Web界面", null, (s, e) =>
    {
        Process.Start(new ProcessStartInfo($"http://localhost:{web.Port}") { UseShellExecute = true });
    });
    
    // 暂停/继续
    var pauseItem = new ToolStripMenuItem("⏸️ 暂停监听", null, (s, e) =>
    {
        clipboard.TogglePause();
        ((ToolStripMenuItem)s!).Text = clipboard.IsPaused ? "▶️ 继续监听" : "⏸️ 暂停监听";
        icon.Text = clipboard.IsPaused ? "SmartClipboard - 已暂停" : "SmartClipboard - 运行中";
    });
    menu.Items.Add(pauseItem);
    
    // 查看日志
    menu.Items.Add("📝 查看日志", null, (s, e) =>
    {
        Process.Start(new ProcessStartInfo(Helpers.GetLogPath()) { UseShellExecute = true });
    });
    
    menu.Items.Add(new ToolStripSeparator());
    
    // 设置
    menu.Items.Add("⚙️ 设置", null, (s, e) =>
    {
        Process.Start(new ProcessStartInfo(config.ConfigPath) { UseShellExecute = true });
    });
    
    // 开机自启
    var autoStartItem = new ToolStripMenuItem("🚀 开机自启", null, (s, e) =>
    {
        var enabled = AutoStartManager.Toggle();
        ((ToolStripMenuItem)s!).Checked = enabled;
        MessageBox.Show(enabled ? "已启用开机自启" : "已禁用开机自启", "提示");
    })
    {
        Checked = AutoStartManager.IsEnabled()
    };
    menu.Items.Add(autoStartItem);
    
    menu.Items.Add(new ToolStripSeparator());
    
    // 退出
    menu.Items.Add("❌ 退出", null, (s, e) =>
    {
        if (MessageBox.Show("确定要退出 SmartClipboard 吗？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            icon.Visible = false;
            cts.Cancel();
            Application.Exit();
        }
    });
    
    icon.ContextMenuStrip = menu;
    
    // 双击打开Web界面
    icon.DoubleClick += (s, e) =>
    {
        Process.Start(new ProcessStartInfo($"http://localhost:{web.Port}") { UseShellExecute = true });
    };
    
    // 定时更新状态
    var timer = new System.Windows.Forms.Timer { Interval = 2000 };
    timer.Tick += (s, e) =>
    {
        statusItem.Text = $"📊 已捕获: {clipboard.CapturedCount} 条 | 今日: {db.GetTodayCount()}";
    };
    timer.Start();
    
    return icon;
}

// ================================================
// 配置类
// ================================================
class AppConfig
{
    public string DatabasePath { get; set; } = "";
    public string ConfigPath { get; set; } = "";
    public bool AutoSave { get; set; } = true;
    public bool FilterSensitive { get; set; } = false;
    public List<string> SensitivePatterns { get; set; } = new();
    
    // AI 配置
    public bool EnableAI { get; set; } = false;
    public string AIProvider { get; set; } = "openai"; // openai, ollama, deepseek, etc
    public string AIApiUrl { get; set; } = "https://api.openai.com/v1";
    public string AIApiKey { get; set; } = "";
    public string AIModel { get; set; } = "gpt-4o-mini";
    public string AIVisionModel { get; set; } = "gpt-4o";
    
    // Matrix 配置
    public bool EnableMatrix { get; set; } = false;
    public string MatrixHomeserver { get; set; } = "https://matrix.org";
    public string MatrixUserId { get; set; } = "";
    public string MatrixAccessToken { get; set; } = "";
    public string MatrixRoomId { get; set; } = "";
    
    // 首次运行标记
    public bool IsFirstRun { get; set; } = true;
    
    public static AppConfig Load()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartClipboard");
        
        Directory.CreateDirectory(appData);
        
        var configPath = Path.Combine(appData, "config.json");
        var config = new AppConfig
        {
            DatabasePath = Path.Combine(appData, "clipboard.db"),
            ConfigPath = configPath,
            SensitivePatterns = new()
            {
                @"password\s*[:=]",
                @"BEGIN (RSA|DSA|EC) PRIVATE KEY",
                @"sk-[a-zA-Z0-9]{32,}",
                @"ghp_[a-zA-Z0-9]{36}"
            }
        };
        
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var loaded = JsonSerializer.Deserialize<AppConfig>(json);
                if (loaded != null)
                {
                    loaded.ConfigPath = configPath;
                    loaded.DatabasePath = config.DatabasePath;
                    return loaded;
                }
            }
            catch (Exception ex)
            {
                Helpers.LogError($"加载配置失败: {ex.Message}");
            }
        }
        
        // 保存默认配置
        File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        return config;
    }
}

// ================================================
// AI 服务 (Ollama)
// ================================================
class AIService
{
    private readonly AppConfig _config;
    private readonly HttpClient _client = new();
    
    public AIService(AppConfig config)
    {
        _config = config;
    }
    
    public async Task<AIAnalysisResult> AnalyzeTextAsync(string text)
    {
        if (!_config.EnableAI || string.IsNullOrWhiteSpace(text))
        {
            return new AIAnalysisResult
            {
                Category = ClassifyBasic(text),
                Summary = text.Length > 100 ? text[..100] + "..." : text,
                Importance = 3
            };
        }
        
        try
        {
            // OpenAI 兼容格式
            if (_config.AIProvider.ToLower() == "ollama")
            {
                return await AnalyzeWithOllamaAsync(text);
            }
            else
            {
                return await AnalyzeWithOpenAIAsync(text);
            }
        }
        catch (Exception ex)
        {
            Helpers.LogError($"AI分析失败: {ex.Message}");
        }
        
        // 降级到基础分类
        return new AIAnalysisResult
        {
            Category = ClassifyBasic(text),
            Summary = text.Length > 100 ? text[..100] + "..." : text,
            Importance = 3
        };
    }
    
    private async Task<AIAnalysisResult> AnalyzeWithOpenAIAsync(string text)
    {
        var request = new
        {
            model = _config.AIModel,
            messages = new[]
            {
                new { role = "system", content = "你是一个剪贴板内容分析助手。分析内容并返回JSON格式: {\"category\": \"分类\", \"summary\": \"摘要\", \"importance\": 1-5}" },
                new { role = "user", content = $"分析这段内容:\n{text[..Math.Min(1000, text.Length)]}" }
            },
            response_format = new { type = "json_object" },
            temperature = 0.3
        };
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_config.AIApiUrl}/chat/completions")
        {
            Content = JsonContent.Create(request)
        };
        
        if (!string.IsNullOrEmpty(_config.AIApiKey))
        {
            httpRequest.Headers.Add("Authorization", $"Bearer {_config.AIApiKey}");
        }
        
        var response = await _client.SendAsync(httpRequest);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>();
            if (result?.Choices?.Length > 0)
            {
                var content = result.Choices[0].Message?.Content;
                if (content != null)
                {
                    var analysis = JsonSerializer.Deserialize<AIAnalysisResult>(content);
                    if (analysis != null) return analysis;
                }
            }
        }
        
        throw new Exception("OpenAI API 调用失败");
    }
    
    private async Task<AIAnalysisResult> AnalyzeWithOllamaAsync(string text)
    {
        var prompt = "分析以下剪贴板内容，返回JSON格式:\n" +
            "{\"category\": \"分类(代码/链接/文档/数据/其他)\", \"summary\": \"一句话摘要(50字内)\", \"importance\": 1-5}\n\n" +
            "内容:\n" + text[..Math.Min(1000, text.Length)];
        
        var request = new
        {
            model = _config.AIModel,
            prompt = prompt,
            stream = false,
            format = "json"
        };
        
        var response = await _client.PostAsJsonAsync($"{_config.AIApiUrl}/api/generate", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
            if (result?.Response != null)
            {
                var analysis = JsonSerializer.Deserialize<AIAnalysisResult>(result.Response);
                if (analysis != null) return analysis;
            }
        }
        
        throw new Exception("Ollama API 调用失败");
    }
    
    private string ClassifyBasic(string text)
    {
        if (Regex.IsMatch(text, @"^(https?|ftp)://")) return "🔗 链接";
        if (Regex.IsMatch(text, @"(class|function|def|const|var|let)\s+")) return "💻 代码";
        if (text.Contains('\n') && text.Length > 200) return "📄 文档";
        if (Regex.IsMatch(text, @"^\d+$")) return "🔢 数字";
        return "📝 文本";
    }
}

class AIAnalysisResult
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = "";
    
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";
    
    [JsonPropertyName("importance")]
    public int Importance { get; set; } = 3;
}

class OllamaResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = "";
}

class OpenAIResponse
{
    [JsonPropertyName("choices")]
    public Choice[]? Choices { get; set; }
}

class Choice
{
    [JsonPropertyName("message")]
    public Message? Message { get; set; }
}

class Message
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

// ================================================
// Matrix 服务 (原生API实现)
// ================================================
class MatrixService
{
    private readonly AppConfig _config;
    private readonly HttpClient _client = new();
    private readonly Queue<(string content, string category)> _messageQueue = new();
    private bool _isProcessing = false;
    
    public MatrixService(AppConfig config)
    {
        _config = config;
        if (!string.IsNullOrEmpty(_config.MatrixAccessToken))
        {
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.MatrixAccessToken}");
        }
    }
    
    public async Task SendToMatrixAsync(string content, string category)
    {
        if (!_config.EnableMatrix || string.IsNullOrWhiteSpace(_config.MatrixRoomId))
        {
            return;
        }
        
        _messageQueue.Enqueue((content, category));
        
        if (!_isProcessing)
        {
            _ = Task.Run(ProcessQueueAsync);
        }
    }
    
    private async Task ProcessQueueAsync()
    {
        if (_isProcessing) return;
        _isProcessing = true;
        
        try
        {
            while (_messageQueue.Count > 0)
            {
                var (content, category) = _messageQueue.Dequeue();
                
                try
                {
                    var formattedContent = FormatMessage(content, category);
                    var txnId = Guid.NewGuid().ToString();
                    var url = $"{_config.MatrixHomeserver}/_matrix/client/v3/rooms/{Uri.EscapeDataString(_config.MatrixRoomId)}/send/m.room.message/{txnId}";
                    
                    var message = new
                    {
                        msgtype = "m.text",
                        body = content,
                        format = "org.matrix.custom.html",
                        formatted_body = formattedContent
                    };
                    
                    var response = await _client.PutAsJsonAsync(url, message);
                    if (response.IsSuccessStatusCode)
                    {
                        Helpers.LogInfo($"已同步到Matrix: {category}");
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Helpers.LogError($"Matrix同步失败 ({response.StatusCode}): {error}");
                    }
                    
                    await Task.Delay(1000); // 避免速率限制
                }
                catch (Exception ex)
                {
                    Helpers.LogError($"Matrix发送失败: {ex.Message}");
                }
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }
    
    private string FormatMessage(string content, string category)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var preview = content.Length > 500 ? content[..500] + "..." : content;
        var encoded = System.Net.WebUtility.HtmlEncode(preview);
        
        return $"<blockquote>\n<p><strong>{category}</strong> | 📅 {timestamp}</p>\n<pre><code>{encoded}</code></pre>\n</blockquote>";
    }
}

// ================================================
// 剪贴板监听服务
// ================================================
class ClipboardService
{
    private readonly ClipboardDatabase _db;
    private readonly AppConfig _config;
    private readonly AIService _aiService;
    private readonly MatrixService _matrixService;
    private readonly CancellationToken _cancellationToken;
    private string _lastHash = "";
    private ClipboardMonitorForm? _form;
    
    public int CapturedCount { get; private set; }
    public bool IsPaused { get; private set; }
    
    public ClipboardService(ClipboardDatabase db, AppConfig config, AIService aiService, MatrixService matrixService, CancellationToken cancellationToken)
    {
        _db = db;
        _config = config;
        _aiService = aiService;
        _matrixService = matrixService;
        _cancellationToken = cancellationToken;
    }
    
    public void TogglePause()
    {
        IsPaused = !IsPaused;
        Helpers.LogInfo(IsPaused ? "监听已暂停" : "监听已恢复");
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
                
                var hash = Helpers.ComputeHash(text);
                if (hash == _lastHash || _db.Exists(hash)) return;
                
                _lastHash = hash;
                
                // 敏感信息检测
                if (_config.FilterSensitive && IsSensitive(text))
                {
                    Helpers.LogInfo($"过滤敏感内容: {text[..Math.Min(30, text.Length)]}...");
                    return;
                }
                
                // AI分析
                var analysis = await _aiService.AnalyzeTextAsync(text);
                
                var entry = new ClipboardEntry
                {
                    ContentHash = hash,
                    Category = analysis.Category,
                    RawContent = text,
                    Summary = analysis.Summary,
                    Importance = analysis.Importance,
                    CreatedAt = DateTime.Now,
                    NeedsReview = !_config.AutoSave
                };
                
                _db.Insert(entry);
                CapturedCount++;
                
                Helpers.LogInfo($"捕获 [{analysis.Category}]: {analysis.Summary}");
                
                // 同步到Matrix
                _ = _matrixService.SendToMatrixAsync(text, analysis.Category);
            }
            catch (Exception ex)
            {
                Helpers.LogError($"处理剪贴板失败: {ex.Message}");
            }
        });
    }
    
    private bool IsSensitive(string text)
    {
        return _config.SensitivePatterns.Any(pattern => 
            Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase));
    }
}

// 隐藏窗口用于接收剪贴板消息
class ClipboardMonitorForm : Form
{
    private readonly Action _onClipboardChanged;
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr AddClipboardFormatListener(IntPtr hwnd);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
    
    private const int WM_CLIPBOARDUPDATE = 0x031D;
    
    public ClipboardMonitorForm(Action onClipboardChanged)
    {
        _onClipboardChanged = onClipboardChanged;
        
        // 隐藏窗口
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
    
    protected override void WndProc(ref System.Windows.Forms.Message m)
    {
        if (m.Msg == WM_CLIPBOARDUPDATE)
        {
            _onClipboardChanged();
        }
        base.WndProc(ref m);
    }
    
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        RemoveClipboardFormatListener(Handle);
        base.OnFormClosing(e);
    }
}

// ================================================
// Web服务
// ================================================
class WebService
{
    private readonly ClipboardDatabase _db;
    private readonly AppConfig _config;
    public int Port { get; } = 5678;
    
    public WebService(ClipboardDatabase db, AppConfig config)
    {
        _db = db;
        _config = config;
    }
    
    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://localhost:{Port}");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        
        var app = builder.Build();
        
        app.MapGet("/", () => Results.Content(_config.IsFirstRun ? GetSetupPage() : GetHtmlPage(), "text/html; charset=utf-8"));
        
        app.MapPost("/api/setup", async (HttpContext ctx) =>
        {
            var setupData = await ctx.Request.ReadFromJsonAsync<AppConfig>();
            if (setupData != null)
            {
                _config.EnableAI = setupData.EnableAI;
                _config.AIProvider = setupData.AIProvider;
                _config.AIApiUrl = setupData.AIApiUrl;
                _config.AIApiKey = setupData.AIApiKey;
                _config.AIModel = setupData.AIModel;
                _config.EnableMatrix = setupData.EnableMatrix;
                _config.MatrixHomeserver = setupData.MatrixHomeserver;
                _config.MatrixUserId = setupData.MatrixUserId;
                _config.MatrixAccessToken = setupData.MatrixAccessToken;
                _config.MatrixRoomId = setupData.MatrixRoomId;
                _config.FilterSensitive = setupData.FilterSensitive;
                _config.IsFirstRun = false;
                
                // 保存配置
                File.WriteAllText(_config.ConfigPath, JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true }));
            }
            return Results.Ok();
        });
        
        app.MapGet("/api/history", (int page = 1, int size = 50) =>
        {
            var items = _db.GetPage(page, size);
            return Results.Json(items);
        });
        
        app.MapGet("/api/search", (string q) =>
        {
            var items = _db.Search(q);
            return Results.Json(items);
        });
        
        app.MapDelete("/api/items/{id}", (int id) =>
        {
            _db.Delete(id);
            return Results.Ok();
        });
        
        app.MapPost("/api/batch", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<BatchRequest>();
            if (body != null && body.Action == "delete")
            {
                _db.BatchDelete(body.Ids);
            }
            return Results.Ok();
        });
        
        await app.RunAsync();
    }
    
    private string GetSetupPage()
    {
        // 优先查找与脚本同目录的 HTML 文件
        var scriptDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppDomain.CurrentDomain.BaseDirectory;
        var htmlPath = Path.Combine(scriptDir, "setup.html");
        
        if (!File.Exists(htmlPath))
        {
            // 如果不在同目录，尝试在当前工作目录查找
            htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "setup.html");
        }
        
        if (!File.Exists(htmlPath))
        {
            Helpers.LogError($"setup.html 未找到。查找路径: {htmlPath}");
            return "<h1>Setup page not found</h1><p>请确保 setup.html 与程序在同一目录</p>";
        }
        
        return File.ReadAllText(htmlPath);
    }
    
    private string GetHtmlPage()
    {
        // 优先查找与脚本同目录的 HTML 文件
        var scriptDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppDomain.CurrentDomain.BaseDirectory;
        var htmlPath = Path.Combine(scriptDir, "index.html");
        
        if (!File.Exists(htmlPath))
        {
            // 如果不在同目录，尝试在当前工作目录查找
            htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "index.html");
        }
        
        if (!File.Exists(htmlPath))
        {
            Helpers.LogError($"index.html 未找到。查找路径: {htmlPath}");
            return "<h1>Index page not found</h1><p>请确保 index.html 与程序在同一目录</p>";
        }
        
        return File.ReadAllText(htmlPath);
    }
}

record BatchRequest(string Action, List<int> Ids);

// ================================================
// 数据库
// ================================================
class ClipboardEntry
{
    public int Id { get; set; }
    public string ContentHash { get; set; } = "";
    public string Category { get; set; } = "";
    public string RawContent { get; set; } = "";
    public string Summary { get; set; } = "";
    public int Importance { get; set; } = 3;
    public DateTime CreatedAt { get; set; }
    public bool NeedsReview { get; set; }
}

class ClipboardDatabase
{
    private readonly string _connectionString;
    
    public ClipboardDatabase(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }
    
    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ClipboardHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ContentHash TEXT UNIQUE,
                Category TEXT,
                RawContent TEXT,
                Summary TEXT,
                Importance INTEGER DEFAULT 3,
                CreatedAt DATETIME,
                NeedsReview INTEGER DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS idx_date ON ClipboardHistory(CreatedAt DESC);
            CREATE INDEX IF NOT EXISTS idx_hash ON ClipboardHistory(ContentHash);
        ";
        cmd.ExecuteNonQuery();
    }
    
    public bool Exists(string hash)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM ClipboardHistory WHERE ContentHash = @hash";
        cmd.Parameters.AddWithValue("@hash", hash);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
    
    public void Insert(ClipboardEntry entry)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO ClipboardHistory 
            (ContentHash, Category, RawContent, Summary, Importance, CreatedAt, NeedsReview)
            VALUES (@hash, @category, @content, @summary, @importance, @created, @review)";
        cmd.Parameters.AddWithValue("@hash", entry.ContentHash);
        cmd.Parameters.AddWithValue("@category", entry.Category);
        cmd.Parameters.AddWithValue("@content", entry.RawContent);
        cmd.Parameters.AddWithValue("@summary", entry.Summary);
        cmd.Parameters.AddWithValue("@importance", entry.Importance);
        cmd.Parameters.AddWithValue("@created", entry.CreatedAt);
        cmd.Parameters.AddWithValue("@review", entry.NeedsReview ? 1 : 0);
        cmd.ExecuteNonQuery();
    }
    
    public List<ClipboardEntry> GetPage(int page, int size)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM ClipboardHistory ORDER BY CreatedAt DESC LIMIT @size OFFSET @offset";
        cmd.Parameters.AddWithValue("@size", size);
        cmd.Parameters.AddWithValue("@offset", (page - 1) * size);
        
        return ReadEntries(cmd);
    }
    
    public List<ClipboardEntry> Search(string keyword)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT * FROM ClipboardHistory 
            WHERE RawContent LIKE @kw OR Summary LIKE @kw OR Category LIKE @kw
            ORDER BY CreatedAt DESC LIMIT 200";
        cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
        
        return ReadEntries(cmd);
    }
    
    public void Delete(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM ClipboardHistory WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
    
    public void BatchDelete(List<int> ids)
    {
        if (ids.Count == 0) return;
        
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = $"DELETE FROM ClipboardHistory WHERE Id IN ({string.Join(",", ids)})";
        cmd.ExecuteNonQuery();
    }
    
    public int GetTodayCount()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM ClipboardHistory WHERE DATE(CreatedAt) = DATE('now')";
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
                CreatedAt = reader.GetDateTime(6),
                NeedsReview = reader.GetInt32(7) == 1
            });
        }
        return list;
    }
}

// ================================================
// 开机自启管理
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
// 辅助函数
// ================================================
static class Helpers
{
    public static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLower();
    }

    public static string GetLogPath()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartClipboard"
        );
        Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, "app.log");
    }

    public static void LogInfo(string message)
    {
        try
        {
            File.AppendAllText(GetLogPath(), $"[{DateTime.Now:HH:mm:ss}] INFO: {message}\n");
        }
        catch { }
    }

    public static void LogError(string message)
    {
        try
        {
            File.AppendAllText(GetLogPath(), $"[{DateTime.Now:HH:mm:ss}] ERROR: {message}\n");
        }
        catch { }
    }
}
