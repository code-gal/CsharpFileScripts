#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net10.0
#:property LangVersion=preview
#:property Nullable=enable
#:package QRCoder@1.4.3

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QRCoder;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddSingleton<InMemoryMoodStore>();
builder.Services.AddSingleton<PaletteGenerator>();
builder.Services.AddSingleton<RecommendationEngine>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

app.MapGet("/", () => Results.Content(Page.IndexHtml, "text/html; charset=utf-8"));

app.MapGet("/api/plan", (string mood, string focus, string location, RecommendationEngine engine) =>
{
    var payload = engine.BuildPlan(mood, focus, location);
    return Results.Ok(payload);
});

app.MapGet("/api/palette", (string mood, PaletteGenerator generator) =>
{
    var palette = generator.CreatePalette(mood);
    return Results.Ok(palette);
});

app.MapPost("/api/mood", (MoodInput input, InMemoryMoodStore store) =>
{
    var aggregate = store.Store(input);
    return Results.Ok(aggregate);
});

app.MapGet("/api/qr", (string text) =>
{
    var image = QrHelper.Create(text);
    return Results.Ok(new QrResponse(image));
});

app.MapGet("/api/ping", () => Results.Ok(new PingResponse(
    "online",
    Environment.MachineName,
    DateTimeOffset.UtcNow)));

app.Run();

static class Page
{
    public const string IndexHtml = """
<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width,initial-scale=1" />
<title>PulseCanvas · 单文件 .NET 10 体验</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;600;700&display=swap" rel="stylesheet">
<style>
:root {
    color-scheme: dark;
    --bg: #050718;
    --panel: rgba(12,16,40,.85);
    --panel-border: rgba(255,255,255,.08);
    --accent: #7c5dfa;
    --accent-2: #48cfe0;
    --text: #f5f7ff;
    --muted: rgba(245,247,255,.7);
}
* { box-sizing: border-box; }
body {
    margin: 0;
    min-height: 100vh;
    font-family: "Manrope","Microsoft YaHei",system-ui;
    background: radial-gradient(circle at top,#1a1648,#050718 65%);
    color: var(--text);
}
main {
    max-width: 1200px;
    margin: 0 auto;
    padding: 56px 24px 96px;
    display: flex;
    flex-direction: column;
    gap: 32px;
}
.hero {
    padding: 32px;
    border-radius: 32px;
    border: 1px solid rgba(255,255,255,.08);
    background: linear-gradient(135deg,#7c5dfa 0%,#48cfe0 35%,rgba(5,7,24,.65) 80%);
    display: grid;
    grid-template-columns: repeat(auto-fit,minmax(240px,1fr));
    gap: 24px;
    align-items: center;
}
.hero h1 {
    margin: 0;
    font-size: 2.6rem;
    line-height: 1.15;
}
.hero p {
    margin: 12px 0 0;
    font-size: 1rem;
    color: rgba(255,255,255,.85);
}
.hero img {
    width: 100%;
    max-height: 220px;
    object-fit: cover;
    border-radius: 24px;
    box-shadow: 0 20px 50px rgba(7,0,37,.4);
}
.layout-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit,minmax(280px,1fr));
    gap: 24px;
}
.panel {
    background: var(--panel);
    border-radius: 24px;
    border: 1px solid var(--panel-border);
    padding: 24px;
    backdrop-filter: blur(16px);
    display: flex;
    flex-direction: column;
    gap: 16px;
    min-height: 260px;
}
.panel h2 {
    margin: 0;
    font-size: 1.2rem;
}
.panel p {
    margin: 0;
    color: var(--muted);
}
label {
    font-size: .85rem;
    color: var(--muted);
}
select {
    width: 100%;
    margin-top: 8px;
    padding: 12px 14px;
    border-radius: 14px;
    border: 1px solid rgba(255,255,255,.15);
    background: rgba(255,255,255,.04);
    color: var(--text);
    font-size: 1rem;
    appearance: none;
}
select option {
    color: #050718;
    background: #ffffff;
}
.panel-hint {
    margin: 0;
    font-size: .85rem;
    color: var(--muted);
}
button {
    border: none;
    cursor: pointer;
    border-radius: 18px;
    padding: 12px 18px;
    font-weight: 600;
    font-size: 1rem;
    display: inline-flex;
    align-items: center;
    gap: 6px;
    background: linear-gradient(120deg,var(--accent),var(--accent-2));
    color: var(--text);
    transition: transform .2s ease;
}
button.ghost {
    background: rgba(255,255,255,.08);
    border: 1px solid rgba(255,255,255,.2);
    color: var(--text);
}
button[disabled] {
    opacity: .6;
    cursor: progress;
}
.timeline {
    list-style: none;
    margin: 12px 0 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 14px;
}
.timeline li {
    padding: 16px;
    border-radius: 18px;
    background: rgba(255,255,255,.03);
    border: 1px solid rgba(255,255,255,.05);
    display: grid;
    grid-template-columns: 80px 1fr;
    gap: 12px;
    align-items: center;
}
.timeline .time {
    font-size: .9rem;
    color: var(--muted);
}
.timeline .action {
    font-size: 1rem;
    font-weight: 600;
}
.timeline .details {
    font-size: .85rem;
    color: var(--muted);
    margin-top: 6px;
}
.palette {
    display: grid;
    grid-template-columns: repeat(auto-fit,minmax(120px,1fr));
    gap: 12px;
}
.palette div {
    border-radius: 18px;
    padding: 16px;
    display: flex;
    flex-direction: column;
    gap: 8px;
    border: 1px solid rgba(255,255,255,.08);
}
.palette span {
    font-size: .8rem;
}
stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit,minmax(140px,1fr));
    gap: 12px;
}
.stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit,minmax(140px,1fr));
    gap: 12px;
}
.stat-card {
    padding: 16px;
    border-radius: 16px;
    border: 1px solid rgba(255,255,255,.06);
    background: rgba(255,255,255,.03);
}
.stat-card strong {
    display: block;
    font-size: 1.4rem;
}
.qr-box {
    margin-top: 12px;
    padding: 18px;
    border-radius: 16px;
    background: rgba(0,0,0,.3);
    border: 1px dashed rgba(255,255,255,.2);
    text-align: center;
}
.qr-box img {
    width: 140px;
    height: 140px;
    object-fit: contain;
    margin-top: 12px;
    opacity: 0;
    transition: opacity .3s ease;
}
.qr-box img.visible { opacity: 1; }
.status-chip {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 8px 14px;
    border-radius: 999px;
    background: rgba(5,7,24,.35);
    border: 1px solid rgba(255,255,255,.25);
    font-size: .85rem;
}
@media (max-width: 720px) {
    .timeline li {
        grid-template-columns: 1fr;
    }
}
</style>
</head>
<body>
<main>
<section class="hero">
<div>
<h1>PulseCanvas · 你的情绪化数字仪表盘</h1>
<p>这是一个真正的单文件 .NET 10 应用，带上 NuGet 支持与 Web SDK，只需 <code>dotnet run HelloCsharp.cs</code> 就能启动一套可在线互动的体验。</p>
<div class="status-chip" id="status-chip">⏳ 检测中...</div>
</div>
<img src="https://images.unsplash.com/photo-1487412720507-e7ab37603c6f?auto=format&fit=crop&w=900&q=80" alt="Mood Board" />
</section>

<section class="layout-grid">
<article class="panel">
<h2>1. 选择今日的状态</h2>
<p class="panel-hint">告诉我你当前的情绪、想做的事和所在空间，我会据此编排体验。</p>
<form id="mood-form">
<label>我现在的情绪
<select name="mood">
<option value="活力">活力</option>
<option value="舒缓">舒缓</option>
<option value="沉稳">沉稳</option>
<option value="缤纷">缤纷</option>
</select>
</label>
<label>想专注的领域
<select name="focus">
<option value="学习">学习</option>
<option value="创作">创作</option>
<option value="办公">办公</option>
<option value="休闲">休闲</option>
</select>
</label>
<label>所在空间
<select name="location">
<option value="客厅">客厅</option>
<option value="书房">书房</option>
<option value="咖啡馆">咖啡馆</option>
<option value="旅途中">旅途中</option>
</select>
</label>
<div style="display:flex;gap:12px;flex-wrap:wrap;margin-top:12px;">
<button type="button" id="refresh-btn">⚡ 智能编排今日节奏</button>
<button type="submit" id="sync-btn" class="ghost">📡 同步到趋势</button>
</div>
<p class="panel-hint">刷新 = 重新生成计划及配色；同步 = 记录一次情绪，更新下方趋势。</p>
</form>
</article>

<article class="panel" id="plan-panel">
<div style="display:flex;justify-content:space-between;align-items:center;">
<h2 id="plan-title">等待生成...</h2>
<span id="plan-tip" style="font-size:.85rem;color:var(--muted);"></span>
</div>
<p id="plan-summary">系统会根据你的选择生成时间线与行动提示。</p>
<ul class="timeline" id="plan-list"></ul>
</article>

<article class="panel">
<h2>2. Mood 色板</h2>
<p id="palette-desc">选择一个情绪后，我会生成一套配色。</p>
<div class="palette" id="palette"></div>
</article>

<article class="panel">
<h2>3. 趋势 & 分享</h2>
<p class="stats-subline" id="stats-summary">同步一次即可生成趋势，了解最近哪种情绪最常出现。</p>
<div class="stats-highlight">
<div>
<strong id="total-count">0</strong>
<span>累计记录</span>
</div>
<div>
<strong id="last-updated">--:--</strong>
<span>最近同步</span>
</div>
</div>
<div class="stats-grid" id="stats"></div>
<div class="qr-box">
<p>生成分享二维码，把你的状态发给朋友。</p>
<button type="button" id="share-btn">✨ 生成二维码</button>
<img id="qr-img" alt="二维码" />
</div>
</article>
</section>
</main>
<script>
const state = { mood: "活力", focus: "学习", location: "客厅" };
const planList = document.querySelector("#plan-list");
const planTitle = document.querySelector("#plan-title");
const planSummary = document.querySelector("#plan-summary");
const planTip = document.querySelector("#plan-tip");
const paletteBox = document.querySelector("#palette");
const paletteDesc = document.querySelector("#palette-desc");
const statsBox = document.querySelector("#stats");
const statsSummary = document.querySelector("#stats-summary");
const totalCountEl = document.querySelector("#total-count");
const lastUpdatedEl = document.querySelector("#last-updated");
const qrImg = document.querySelector("#qr-img");
const statusChip = document.querySelector("#status-chip");
const refreshBtn = document.querySelector("#refresh-btn");
const syncBtn = document.querySelector("#sync-btn");
const form = document.querySelector("#mood-form");
let shareText = "PulseCanvas 体验";
const formatter = new Intl.DateTimeFormat("zh-CN",{hour:"2-digit",minute:"2-digit"});

form.addEventListener("change", (evt) => {
    const target = evt.target;
    if (!target.name) return;
    state[target.name] = target.value;
});
document.querySelector("#refresh-btn").addEventListener("click", async () => {
    setButtonLoading(refreshBtn, true, "生成中...");
    await refreshAll();
    setButtonLoading(refreshBtn, false, "⚡ 智能编排今日节奏");
});
form.addEventListener("submit", async (evt) => {
    evt.preventDefault();
    await sendMood();
});
document.querySelector("#share-btn").addEventListener("click", () => generateQr());

async function refreshAll() {
    await Promise.all([refreshPlan(), refreshPalette()]);
}

async function refreshPlan() {
    const params = new URLSearchParams(state).toString();
    const res = await fetch(`/api/plan?${params}`);
    const data = await res.json();
    planTitle.textContent = data.title;
    planSummary.textContent = data.summary;
    planTip.textContent = data.tip;
    shareText = `${data.title}｜${data.summary}`;
    planList.innerHTML = data.timeline.map(item => `
        <li>
            <div class="time">${item.time}</div>
            <div>
                <div class="action">${item.accent} ${item.action}</div>
                <div class="details">${item.details}</div>
            </div>
        </li>
    `).join("");
}

async function refreshPalette() {
    const params = new URLSearchParams({ mood: state.mood }).toString();
    const res = await fetch(`/api/palette?${params}`);
    const data = await res.json();
    paletteDesc.textContent = data.description;
    paletteBox.innerHTML = data.colors.map(color => `
        <div style="background:${color}22;border-color:${color}55;">
            <div style="width:100%;height:36px;border-radius:12px;background:${color};"></div>
            <span>${color}</span>
        </div>
    `).join("");
}

async function sendMood() {
    setButtonLoading(syncBtn, true, "同步中...");
    try {
        const res = await fetch("/api/mood", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(state)
        });
        const data = await res.json();
        totalCountEl.textContent = data.total;
        lastUpdatedEl.textContent = data.lastUpdated;
        statsSummary.textContent = data.topMoods.length
            ? `今天 ${data.topMoods[0].mood} 被记录了 ${data.topMoods[0].count} 次`
            : "成为今天的第一条记录吧。";
        statsBox.innerHTML = data.topMoods.length
            ? data.topMoods.map(item => `
                <div class="stat-card">
                    <span>${item.mood}</span>
                    <strong>${item.percent}%</strong>
                    <small>${item.count} 次</small>
                </div>
            `).join("")
            : "<p class=\"muted\">暂无数据，点击上方“同步到趋势”即可记录。</p>";
    } finally {
        setButtonLoading(syncBtn, false, "📡 同步到趋势");
    }
}

async function generateQr() {
    qrImg.classList.remove("visible");
    const res = await fetch(`/api/qr?text=${encodeURIComponent(shareText)}`);
    const data = await res.json();
    qrImg.src = data.image;
    setTimeout(() => qrImg.classList.add("visible"), 80);
}

async function pingServer() {
    try {
        const res = await fetch("/api/ping");
        const data = await res.json();
        statusChip.textContent = `🟢 ${data.status} · ${formatter.format(new Date(data.now))}`;
    } catch {
        statusChip.textContent = "🔴 离线";
    }
}
refreshAll();
sendMood();
pingServer();
setInterval(pingServer, 15000);

function setButtonLoading(button, loading, label) {
    if (!button) return;
    button.disabled = loading;
    if (label) button.textContent = label;
}
</script>
</body>
</html>
""";
}

record MoodInput(string Mood, string Focus, string Location);
record PlanResponse(string Title, string Summary, IReadOnlyList<PlanItem> Timeline, string Tip);
record PlanItem(string Time, string Action, string Details, string Accent);
record PaletteResponse(string Mood, IReadOnlyList<string> Colors, string Description);
record MoodSnapshot(string Mood, string Focus, string Location, DateTimeOffset Timestamp);
record MoodAggregate(int Total, IReadOnlyList<MoodStat> TopMoods, string LastUpdated);
record MoodStat(string Mood, int Count, double Percent);
record QrResponse(string Image);
record PingResponse(string Status, string Server, DateTimeOffset Now);

sealed class RecommendationEngine
{
    private readonly string[] _microHabits =
    [
        "3 分钟方块呼吸",
        "随手写下 2 件感谢的事",
        "换一首配乐刷新节奏",
        "补给一杯水 + 拉伸",
        "关闭通知 15 分钟"
    ];

    private readonly string[] _focusBoosters =
    [
        "番茄钟 25 分钟 + 5 分钟散步",
        "把任务拆成 3 个可完成的小块",
        "先完成最具摩擦力的一步",
        "用语音把想法读给自己听",
        "把屏幕亮度调低 10%，降低刺激"
    ];

    public PlanResponse BuildPlan(string mood, string focus, string location)
    {
        var timeline = new List<PlanItem>
        {
            new("09:00", $"{focus} 热身", $"在 {location} 打开 1 首能代表「{mood}」的歌，写下今天成功的样子。", "🌅"),
            new("10:30", "沉浸时段", $"将手机放远 2 米，记录一个灵感碎片：{Pick(_focusBoosters)}。", "🎯"),
            new("14:00", "感官补给", $"切换坐姿或走到窗边，完成 {Pick(_microHabits)}。", "🌿"),
            new("20:30", "收束 & 分享", "用一句话总结今天的亮点，并准备分享给未来的你。", "✨")
        };

        var title = $"{mood} · {focus} 时间线";
        var summary = $"在 {location} 构建一条易于执行的体验旅程，保持 {mood} 的韵律。";
        var tip = $"为「{focus}」保留 2 段 45 分钟的整块时间。";

        return new PlanResponse(title, summary, timeline, tip);
    }

    private static string Pick(IReadOnlyList<string> source)
        => source.Count == 0 ? string.Empty : source[RandomNumberGenerator.GetInt32(source.Count)];
}

sealed class PaletteGenerator
{
    private readonly Dictionary<string, string[]> _presets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["活力"] = ["#FF6B6B", "#FFD166", "#4ECDC4", "#1A535C"],
        ["舒缓"] = ["#A8E6CF", "#DCEDC1", "#FFD3B6", "#FFAAA5"],
        ["沉稳"] = ["#0B132B", "#1C2541", "#3A506B", "#5BC0BE"],
        ["缤纷"] = ["#845EC2", "#FF847C", "#FFB997", "#FBEAFF"]
    };

    public PaletteResponse CreatePalette(string mood)
    {
        var colors = _presets.TryGetValue(mood, out var ready)
            ? ready
            : GenerateDynamicPalette(mood);
        var description = $"「{mood}」灵感：{string.Join(" · ", colors)}";
        return new PaletteResponse(mood, colors, description);
    }

    private static string[] GenerateDynamicPalette(string seed)
    {
        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes);
        var colors = new string[4];
        for (var i = 0; i < colors.Length; i++)
        {
            var r = (bytes[i * 3] + seed.Length * 23) % 255;
            var g = (bytes[i * 3 + 1] + seed.Length * 17) % 255;
            var b = (bytes[i * 3 + 2] + seed.Length * 11) % 255;
            colors[i] = $"#{r:X2}{g:X2}{b:X2}";
        }

        return colors;
    }
}

sealed class InMemoryMoodStore
{
    private readonly List<MoodSnapshot> _history = new();
    private readonly object _gate = new();

    public MoodAggregate Store(MoodInput input)
    {
        lock (_gate)
        {
            _history.Add(new MoodSnapshot(input.Mood, input.Focus, input.Location, DateTimeOffset.UtcNow));
            var total = _history.Count;
            var top = _history
                .GroupBy(x => x.Mood)
                .Select(g => new MoodStat(
                    g.Key,
                    g.Count(),
                    Math.Round(g.Count() * 100d / total, 1)))
                .OrderByDescending(stat => stat.Count)
                .Take(4)
                .ToList();

            var lastUpdated = DateTimeOffset.UtcNow
                .ToLocalTime()
                .ToString("HH:mm:ss", CultureInfo.InvariantCulture);

            return new MoodAggregate(total, top, lastUpdated);
        }
    }
}

static class QrHelper
{
    public static string Create(string? text)
    {
        var payload = string.IsNullOrWhiteSpace(text)
            ? "PulseCanvas"
            : text.Trim();

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        var bytes = qrCode.GetGraphic(
            5,
            new byte[] { 0x1F, 0x23, 0x33, 0xFF },
            new byte[] { 0xF5, 0xF5, 0xF5, 0xFF },
            true);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MoodInput))]
[JsonSerializable(typeof(PlanResponse))]
[JsonSerializable(typeof(PaletteResponse))]
[JsonSerializable(typeof(MoodAggregate))]
[JsonSerializable(typeof(MoodStat))]
[JsonSerializable(typeof(QrResponse))]
[JsonSerializable(typeof(PingResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
