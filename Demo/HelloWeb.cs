#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk.Web

using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

// ========= 伪文件列表（进程内存） =========
var files = new List<FileItem>
{
    new("readme.txt", 1_234, "text/plain"),
    new("photo.png", 256_000, "image/png"),
    new("report.pdf", 1_024_000, "application/pdf")
};

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    WriteIndented = false
};

// ========= Web 服务启动 =========
var builder = WebApplication.CreateBuilder(args);

// 日志
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(BuildHtmlPage(files));
});

app.MapGet("/api/files", () =>
{
    return Results.Json(files, jsonOptions);
});

// 伪下载：仅返回成功，并在日志打印提示
app.MapPost("/api/download/{name}", (string name, ILoggerFactory lf) =>
{
    var logger = lf.CreateLogger("FakeDownload");
    var item = files.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
    if (item is null)
    {
        return Results.NotFound(new { message = "文件不存在" });
    }

    logger.LogInformation("模拟下载文件: {FileName}", item.Name);
    return Results.Ok(new { message = $"下载成功: {item.Name}" });
});

// 伪上传：接收 multipart/form-data 文件，取文件名并加入列表
app.MapPost("/api/upload", async (HttpRequest request, ILoggerFactory lf) =>
{
    var logger = lf.CreateLogger("FakeUpload");

    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { message = "请求内容类型错误（需 multipart/form-data）" });
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
    {
        return Results.BadRequest(new { message = "未选择文件或文件为空" });
    }

    var name = file.FileName;
    var length = (long)file.Length;
    var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

    // 如果已存在就不重复添加，仅提示成功
    if (files.All(f => !string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)))
    {
        files.Add(new FileItem(name, length, contentType));
    }

    logger.LogInformation("模拟上传文件: {FileName}, 大小: {Length}, 类型: {ContentType}", name, length, contentType);
    return Results.Ok(new { message = $"上传成功: {name}" });
});

app.Run();

// ========= 页面 HTML（嵌入式前端） =========
static string BuildHtmlPage(IReadOnlyCollection<FileItem> files)
{
    var sb = new StringBuilder();
    sb.Append("""
<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width,initial-scale=1" />
<title>伪文件分享与上传</title>
<style>
:root {
  --bg: #0f172a;
  --panel: #111827;
  --text: #e5e7eb;
  --muted: #9ca3af;
  --accent: #22c55e;
  --danger: #ef4444;
  --border: #374151;
  --link: #60a5fa;
}
* { box-sizing: border-box; }
body {
  margin: 0; padding: 32px; background: linear-gradient(180deg, #0b1220, #0f172a);
  color: var(--text); font-family: -apple-system, Segoe UI, Roboto, Helvetica, Arial, "PingFang SC", "Microsoft YaHei", sans-serif;
}
.container {
  max-width: 960px; margin: 0 auto; background: rgba(17,24,39,0.7); border: 1px solid var(--border);
  border-radius: 16px; padding: 24px; backdrop-filter: blur(6px);
}
.header {
  display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px;
}
h1 { font-size: 20px; margin: 0; letter-spacing: .5px; }
.btn {
  appearance: none; border: 1px solid var(--border); background: #0b1220; color: var(--text);
  padding: 8px 14px; border-radius: 10px; cursor: pointer; transition: all .2s ease;
}
.btn:hover { border-color: #4b5563; transform: translateY(-1px); }
.btn-accent { border-color: #16a34a; color: #d1fae5; }
.btn-accent:hover { border-color: #22c55e; box-shadow: 0 0 0 2px rgba(34,197,94,.15) inset; }
.btn-danger { border-color: #b91c1c; color: #fee2e2; }
.list { margin: 8px 0 0 0; border-top: 1px dashed var(--border); }
.item {
  display: grid; grid-template-columns: 1fr auto auto; gap: 12px; align-items: center;
  padding: 14px 0; border-bottom: 1px dashed var(--border);
}
.meta { display: flex; gap: 10px; align-items: baseline; color: var(--muted); font-size: 12px; }
.name { font-weight: 600; color: var(--text); }
.footer { margin-top: 16px; color: var(--muted); font-size: 12px; }
.hidden { display: none; }
.toast {
  position: fixed; right: 16px; bottom: 16px; padding: 12px 14px; background: #111827; border: 1px solid var(--border);
  color: var(--text); border-radius: 12px; box-shadow: 0 6px 20px rgba(0,0,0,.4);
}
a, .link { color: var(--link); text-decoration: none; }
.link:hover { text-decoration: underline; }
input[type=file] { color: var(--text); }
</style>
</head>
<body>
  <div class="container">
    <div class="header">
      <h1>📦 伪文件分享与上传服务</h1>
      <div>
        <label class="btn btn-accent" for="fileInput">选择文件</label>
        <input id="fileInput" type="file" class="hidden" />
        <button id="uploadBtn" class="btn btn-accent" style="margin-left:8px;">上传</button>
      </div>
    </div>
    <div id="list" class="list"></div>
    <div class="footer">此页面为演示用，所有下载与上传均为伪操作。</div>
  </div>
  <div id="toast" class="toast hidden"></div>
<script>
const listEl = document.getElementById('list');
const fileInput = document.getElementById('fileInput');
const uploadBtn = document.getElementById('uploadBtn');
const toastEl = document.getElementById('toast');

function showToast(text, timeout = 2000) {
  toastEl.textContent = text;
  toastEl.classList.remove('hidden');
  clearTimeout(showToast.__t);
  showToast.__t = setTimeout(() => toastEl.classList.add('hidden'), timeout);
}

function fmtSize(n) {
  const units = ['B','KB','MB','GB']; let i=0; let v=n;
  while (v >= 1024 && i < units.length-1) { v/=1024; i++; }
  return (v>=10? v.toFixed(0): v.toFixed(1)) + ' ' + units[i];
}

async function loadFiles() {
  const res = await fetch('/api/files');
  const data = await res.json();
  renderList(data);
}

function renderList(items) {
  listEl.innerHTML = '';
  if (!items || items.length === 0) {
    listEl.innerHTML = '<div class="item"><div class="name">暂无文件</div></div>';
    return;
  }
  for (const it of items) {
    const row = document.createElement('div');
    row.className = 'item';
    const name = document.createElement('div');
    name.className = 'name';
    name.textContent = it.name;
    const meta = document.createElement('div');
    meta.className = 'meta';
    meta.innerHTML = `<span>${fmtSize(it.size)}</span><span>${it.contentType}</span>`;
    const dlBtn = document.createElement('button');
    dlBtn.className = 'btn';
    dlBtn.textContent = '下载';
    dlBtn.onclick = async () => {
      const r = await fetch(`/api/download/${encodeURIComponent(it.name)}`, { method: 'POST' });
      if (r.ok) {
        const msg = await r.json();
        showToast(msg.message ?? '下载成功');
      } else {
        showToast('下载失败', 2500);
      }
    };
    row.appendChild(name);
    row.appendChild(meta);
    row.appendChild(dlBtn);
    listEl.appendChild(row);
  }
}

uploadBtn.addEventListener('click', async () => {
  const f = fileInput.files?.[0];
  if (!f) { showToast('请先选择文件'); return; }
  const fd = new FormData();
  fd.append('file', f, f.name);
  const r = await fetch('/api/upload', { method: 'POST', body: fd });
  if (r.ok) {
    const msg = await r.json();
    showToast(msg.message ?? '上传成功');
    await loadFiles();
    fileInput.value = '';
  } else {
    showToast('上传失败', 2500);
  }
});

loadFiles();
</script>
</body>
</html>
""");
    return sb.ToString();
}

// ========= 共享模型 =========
public record FileItem(string Name, long Size, string ContentType);