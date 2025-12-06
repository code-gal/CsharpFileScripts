#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk.Web
#:package Markdig@0.37.0

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 主页 - 完整的交互式教程
app.MapGet("/", () => Results.Content($$"""
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Z-Image-Turbo 完整实践指南 - 交互式教程</title>
    <script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3.x.x/dist/cdn.min.js"></script>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        
        :root {
            --bg-primary: #ffffff;
            --bg-secondary: #f8fafc;
            --bg-card: #ffffff;
            --text-primary: #1e293b;
            --text-secondary: #64748b;
            --border-color: #e2e8f0;
            --accent-purple: #8b5cf6;
            --accent-blue: #3b82f6;
            --accent-green: #10b981;
            --code-bg: #1e293b;
            --shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1);
        }
        
        .dark {
            --bg-primary: #0f172a;
            --bg-secondary: #1e293b;
            --bg-card: #1e293b;
            --text-primary: #f1f5f9;
            --text-secondary: #94a3b8;
            --border-color: #334155;
            --shadow: 0 4px 6px -1px rgb(0 0 0 / 0.3);
        }
        
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: var(--bg-primary);
            color: var(--text-primary);
            line-height: 1.6;
            transition: background-color 0.3s, color 0.3s;
        }
        
        /* 顶部导航栏 */
        .navbar {
            position: sticky;
            top: 0;
            z-index: 100;
            background: linear-gradient(135deg, var(--accent-purple), var(--accent-blue));
            padding: 1rem 2rem;
            box-shadow: var(--shadow);
        }
        
        .navbar-content {
            max-width: 1400px;
            margin: 0 auto;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }
        
        .navbar h1 {
            color: white;
            font-size: 1.5rem;
            font-weight: 700;
        }
        
        .navbar-actions {
            display: flex;
            gap: 1rem;
            align-items: center;
        }
        
        .search-box {
            position: relative;
        }
        
        .search-input {
            padding: 0.5rem 2.5rem 0.5rem 1rem;
            border: none;
            border-radius: 9999px;
            background: rgba(255, 255, 255, 0.2);
            color: white;
            font-size: 0.9rem;
            width: 250px;
            transition: all 0.3s;
        }
        
        .search-input::placeholder {
            color: rgba(255, 255, 255, 0.7);
        }
        
        .search-input:focus {
            outline: none;
            background: rgba(255, 255, 255, 0.3);
            width: 300px;
        }
        
        .search-icon {
            position: absolute;
            right: 1rem;
            top: 50%;
            transform: translateY(-50%);
            color: white;
        }
        
        .theme-toggle {
            background: rgba(255, 255, 255, 0.2);
            border: none;
            color: white;
            padding: 0.5rem 1rem;
            border-radius: 9999px;
            cursor: pointer;
            font-size: 1.2rem;
            transition: all 0.3s;
        }
        
        .theme-toggle:hover {
            background: rgba(255, 255, 255, 0.3);
            transform: scale(1.1);
        }
        
        /* 进度条 */
        .progress-bar {
            position: fixed;
            top: 73px;
            left: 0;
            width: 100%;
            height: 3px;
            background: var(--bg-secondary);
            z-index: 99;
        }
        
        .progress-fill {
            height: 100%;
            background: linear-gradient(90deg, var(--accent-purple), var(--accent-blue));
            transition: width 0.1s;
        }
        
        /* 主容器 */
        .container {
            max-width: 1400px;
            margin: 2rem auto;
            padding: 0 2rem;
            display: grid;
            grid-template-columns: 280px 1fr;
            gap: 2rem;
        }
        
        /* 侧边导航 */
        .sidebar {
            position: sticky;
            top: 100px;
            height: fit-content;
            background: var(--bg-card);
            border-radius: 1rem;
            padding: 1.5rem;
            box-shadow: var(--shadow);
        }
        
        .nav-title {
            font-size: 0.875rem;
            font-weight: 700;
            text-transform: uppercase;
            color: var(--text-secondary);
            margin-bottom: 1rem;
        }
        
        .nav-item {
            padding: 0.75rem 1rem;
            margin-bottom: 0.5rem;
            border-radius: 0.5rem;
            cursor: pointer;
            transition: all 0.2s;
            color: var(--text-primary);
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }
        
        .nav-item:hover {
            background: var(--bg-secondary);
            transform: translateX(5px);
        }
        
        .nav-item.active {
            background: linear-gradient(135deg, var(--accent-purple), var(--accent-blue));
            color: white;
        }
        
        .nav-icon {
            font-size: 1.2rem;
        }
        
        /* 主内容区 */
        .content {
            background: var(--bg-card);
            border-radius: 1rem;
            padding: 2rem;
            box-shadow: var(--shadow);
        }
        
        .section {
            margin-bottom: 3rem;
            padding-bottom: 2rem;
            border-bottom: 2px solid var(--border-color);
        }
        
        .section:last-child {
            border-bottom: none;
        }
        
        .section-header {
            display: flex;
            align-items: center;
            gap: 1rem;
            margin-bottom: 1.5rem;
            cursor: pointer;
        }
        
        .section-icon {
            font-size: 2rem;
        }
        
        .section-title {
            font-size: 2rem;
            font-weight: 700;
            background: linear-gradient(135deg, var(--accent-purple), var(--accent-blue));
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }
        
        .section-subtitle {
            font-size: 1.5rem;
            font-weight: 600;
            color: var(--text-primary);
            margin: 1.5rem 0 1rem 0;
        }
        
        .section-content {
            color: var(--text-secondary);
            font-size: 1.05rem;
        }
        
        /* 信息卡片 */
        .info-card {
            background: var(--bg-secondary);
            border-left: 4px solid var(--accent-blue);
            padding: 1rem 1.5rem;
            border-radius: 0.5rem;
            margin: 1rem 0;
        }
        
        .warning-card {
            background: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 1rem 1.5rem;
            border-radius: 0.5rem;
            margin: 1rem 0;
            color: #92400e;
        }
        
        .dark .warning-card {
            background: #451a03;
            color: #fbbf24;
        }
        
        /* 表格 */
        table {
            width: 100%;
            border-collapse: collapse;
            margin: 1rem 0;
            font-size: 0.95rem;
        }
        
        th, td {
            padding: 0.75rem;
            text-align: left;
            border-bottom: 1px solid var(--border-color);
        }
        
        th {
            background: var(--bg-secondary);
            font-weight: 600;
            color: var(--text-primary);
        }
        
        tr:hover {
            background: var(--bg-secondary);
        }
        
        /* 代码块 */
        .code-block {
            position: relative;
            margin: 1rem 0;
        }
        
        pre {
            background: var(--code-bg);
            color: #e2e8f0;
            padding: 1.5rem;
            border-radius: 0.5rem;
            overflow-x: auto;
            font-size: 0.9rem;
            line-height: 1.5;
        }
        
        code {
            font-family: 'Courier New', monospace;
        }
        
        .copy-button {
            position: absolute;
            top: 0.5rem;
            right: 0.5rem;
            background: rgba(255, 255, 255, 0.1);
            border: 1px solid rgba(255, 255, 255, 0.2);
            color: white;
            padding: 0.5rem 1rem;
            border-radius: 0.25rem;
            cursor: pointer;
            font-size: 0.85rem;
            transition: all 0.2s;
        }
        
        .copy-button:hover {
            background: rgba(255, 255, 255, 0.2);
        }
        
        .copy-button.copied {
            background: var(--accent-green);
            border-color: var(--accent-green);
        }
        
        /* 图片 */
        img {
            max-width: 100%;
            height: auto;
            border-radius: 0.5rem;
            margin: 1rem 0;
            box-shadow: var(--shadow);
        }
        
        /* 列表 */
        ul, ol {
            margin: 1rem 0 1rem 2rem;
        }
        
        li {
            margin: 0.5rem 0;
        }
        
        /* 高亮搜索结果 */
        .highlight {
            background: #fef08a;
            color: #854d0e;
            padding: 0.1rem 0.2rem;
            border-radius: 0.2rem;
        }
        
        .dark .highlight {
            background: #713f12;
            color: #fef08a;
        }
        
        /* 响应式 */
        @media (max-width: 1024px) {
            .container {
                grid-template-columns: 1fr;
            }
            
            .sidebar {
                position: static;
            }
            
            .search-input {
                width: 200px;
            }
            
            .search-input:focus {
                width: 250px;
            }
        }
        
        @media (max-width: 640px) {
            .navbar h1 {
                font-size: 1.2rem;
            }
            
            .search-input {
                width: 150px;
            }
            
            .container {
                padding: 0 1rem;
            }
            
            .content {
                padding: 1rem;
            }
        }

        /* 滚动条美化 */
        ::-webkit-scrollbar {
            width: 10px;
        }

        ::-webkit-scrollbar-track {
            background: var(--bg-secondary);
        }

        ::-webkit-scrollbar-thumb {
            background: linear-gradient(135deg, var(--accent-purple), var(--accent-blue));
            border-radius: 5px;
        }

        ::-webkit-scrollbar-thumb:hover {
            background: var(--accent-purple);
        }

        /* 步骤数字标记 */
        .step-number {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 2rem;
            height: 2rem;
            background: linear-gradient(135deg, var(--accent-purple), var(--accent-blue));
            color: white;
            border-radius: 50%;
            font-weight: 700;
            margin-right: 0.5rem;
        }
    </style>
</head>
<body x-data="tutorial()" :class="darkMode ? 'dark' : ''" x-init="init()">
    <!-- 导航栏 -->
    <nav class="navbar">
        <div class="navbar-content">
            <h1>🎨 Z-Image-Turbo 完整实践指南</h1>
            <div class="navbar-actions">
                <div class="search-box">
                    <input type="text" 
                           class="search-input" 
                           placeholder="搜索内容..." 
                           x-model="searchQuery"
                           @input="searchContent()">
                    <span class="search-icon">🔍</span>
                </div>
                <button class="theme-toggle" @click="toggleTheme()" x-text="darkMode ? '☀️' : '🌙'"></button>
            </div>
        </div>
    </nav>

    <!-- 进度条 -->
    <div class="progress-bar">
        <div class="progress-fill" :style="`width: ${scrollProgress}%`"></div>
    </div>

    <div class="container">
        <!-- 侧边导航 -->
        <aside class="sidebar">
            <div class="nav-title">📑 目录导航</div>
            <template x-for="(section, index) in sections" :key="index">
                <div class="nav-item" 
                     :class="activeSection === index ? 'active' : ''"
                     @click="scrollToSection(index)">
                    <span class="nav-icon" x-text="section.icon"></span>
                    <span x-text="section.title"></span>
                </div>
            </template>
        </aside>

        <!-- 主内容 -->
        <main class="content">
            <!-- 前言 -->
            <section class="section" data-section="0">
                <div class="section-header">
                    <span class="section-icon">📖</span>
                    <h2 class="section-title">前言</h2>
                </div>
                <div class="section-content">
                    <div class="warning-card">
                        <strong>⚠️ 给自己叠个甲：</strong><br>
                        全文都是作者的实践,内容是作者自己写的,文档一开始写了很多份,想着发到 L 站直接就整合一份完整的文档,就用了 CC 帮整合了,没曾想有个 AIGC 的限制,只能手工调回来再重新发一遍了! 😭
                    </div>
                    <p><strong>文档版本</strong>: v4.0 - 基于实际实践内容调整,去除 AI 润色内容</p>
                    <p><strong>最后更新</strong>: 2025-12-05</p>
                    <p><strong>适用设备</strong>: Mac Mini M4 32GB RAM</p>
                    <p><strong>实测模型</strong>: Z-Image-Turbo (阿里巴巴通义实验室)</p>
                </div>
            </section>

            <!-- 什么是 Z-Image-Turbo -->
            <section class="section" data-section="1">
                <div class="section-header">
                    <span class="section-icon">🚀</span>
                    <h2 class="section-title">什么是 Z-Image-Turbo</h2>
                </div>
                <div class="section-content">
                    <h3 class="section-subtitle">核心特性</h3>
                    <p><strong>Z-Image-Turbo</strong> 是阿里巴巴通义实验室于 2025 年底发布的高效图像生成模型。</p>
                    <ul>
                        <li>💎 <strong>60 亿参数</strong>高效图像生成模型</li>
                        <li>⚡ <strong>8 步采样</strong>即可生成高质量图像</li>
                        <li>📜 <strong>Apache 2.0 开源协议</strong>(完全商用友好)</li>
                        <li>🌏 支持<strong>中英文双语</strong>文本渲染(中文表现优异)</li>
                        <li>💻 专为<strong>消费级硬件</strong>优化(16GB+ RAM 即可运行)</li>
                        <li>🏗️ 基于 <strong>S3-DiT 单流 Transformer</strong> 架构</li>
                    </ul>

                    <h3 class="section-subtitle">对普通硬件消费者的意义</h3>
                    <div class="info-card">
                        <strong>🎯 三大优势:</strong>
                        <ol>
                            <li><strong>蒸馏优化</strong>: 从大模型蒸馏到 8 步推理,大大减少生成时间</li>
                            <li><strong>中文理解能力强</strong>: 基于 Qwen 3 4B 文本编码器</li>
                            <li><strong>Apple Silicon 友好</strong>: 支持 MPS (Metal Performance Shaders) 后端</li>
                        </ol>
                    </div>
                </div>
            </section>

            <!-- 硬件要求 -->
            <section class="section" data-section="2">
                <div class="section-header">
                    <span class="section-icon">💻</span>
                    <h2 class="section-title">硬件要求与性能</h2>
                </div>
                <div class="section-content">
                    <h3 class="section-subtitle">官方推荐配置</h3>
                    <table>
                        <thead>
                            <tr>
                                <th>硬件</th>
                                <th>最低要求</th>
                                <th>推荐配置</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td><strong>GPU</strong></td>
                                <td>8GB VRAM</td>
                                <td>12GB+ VRAM</td>
                            </tr>
                            <tr>
                                <td><strong>内存</strong></td>
                                <td>16GB RAM</td>
                                <td>32GB+ RAM</td>
                            </tr>
                            <tr>
                                <td><strong>磁盘</strong></td>
                                <td>40GB 可用空间</td>
                                <td>60GB+ 可用空间</td>
                            </tr>
                            <tr>
                                <td><strong>系统</strong></td>
                                <td colspan="2">Windows 10/11, macOS 12.3+, Ubuntu 20.04+</td>
                            </tr>
                        </tbody>
                    </table>

                    <h3 class="section-subtitle">🖥️ NVIDIA GPU 配置(最佳性能,推荐)</h3>
                    <table>
                        <thead>
                            <tr>
                                <th>配置等级</th>
                                <th>GPU 型号</th>
                                <th>VRAM</th>
                                <th>预期性能 @ 768×768</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td>入门级</td>
                                <td>RTX 3080 / RTX 4060 / RTX 4070</td>
                                <td>8-12GB</td>
                                <td>15-30秒</td>
                            </tr>
                            <tr>
                                <td>主流级</td>
                                <td>RTX 4070 Ti / RTX 5060 Ti</td>
                                <td>12-16GB</td>
                                <td>10-20秒</td>
                            </tr>
                            <tr>
                                <td>专业级</td>
                                <td>RTX 4080 / RTX 4090</td>
                                <td>16-32GB</td>
                                <td>5-15秒</td>
                            </tr>
                            <tr>
                                <td>工作站</td>
                                <td>A6000 / H100</td>
                                <td>48-80GB</td>
                                <td>&lt;5秒</td>
                            </tr>
                        </tbody>
                    </table>
                    <p><strong>特点</strong>: CUDA 优化最好,社区支持最完善,<strong>支持 FP8/INT4/INT8 等多种量化模型</strong></p>

                    <h3 class="section-subtitle">🍎 Apple Silicon 配置(Mac 用户)</h3>
                    <table>
                        <thead>
                            <tr>
                                <th>配置等级</th>
                                <th>芯片型号</th>
                                <th>统一内存</th>
                                <th>预期性能 @ 768×768</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td>入门级</td>
                                <td>M系列芯片即可</td>
                                <td>16GB</td>
                                <td>60-120秒</td>
                            </tr>
                            <tr>
                                <td>主流级</td>
                                <td>M系列芯片即可</td>
                                <td>24-32GB</td>
                                <td>40-80秒</td>
                            </tr>
                            <tr>
                                <td>专业级</td>
                                <td>M系列芯片即可</td>
                                <td>36-48GB</td>
                                <td>30-60秒</td>
                            </tr>
                            <tr>
                                <td>顶配</td>
                                <td>M系列芯片即可</td>
                                <td>64-128GB</td>
                                <td>20-50秒</td>
                            </tr>
                        </tbody>
                    </table>
                    <p><strong>特点</strong>: 笔记本也能运行,功耗低,噪音小,GPU 和 CPU 统一共享内存,<strong>但是仅支持 BF16 和 UINT4</strong></p>

                    <h3 class="section-subtitle">Mac Mini M4 32GB RAM 实测数据</h3>
                    <div class="info-card">
                        <ul>
                            <li><strong>内存</strong>: 32GB 超过官方建议(16GB+)</li>
                            <li><strong>MPS 后端</strong>: Metal 3 / Metal 4 支持</li>
                            <li><strong>量化支持</strong>: 支持 BF16 和 UINT4(不支持 FP8)</li>
                            <li><strong>实测速度</strong>: 214-471 秒/张(取决于方案和配置)</li>
                        </ul>
                    </div>

                    <h3 class="section-subtitle">📊 真实性能数据(完整测试)</h3>
                    <ul>
                        <li>✅ <strong>ComfyUI Desktop + LoRA</strong>: <strong>214秒</strong> @ 1024×1024 (最快方案)</li>
                        <li>⚡ <strong>ComfyUI Desktop(无 LoRA)</strong>: 300-400秒 @ 1024×1024</li>
                        <li>🔧 <strong>Gradio 量化版(无 LoRA)</strong>: 255秒 @ 1024×1024</li>
                        <li>⏱️ <strong>Gradio + LoRA</strong>: 417秒 @ 1024×1024 (不推荐)</li>
                    </ul>
                </div>
            </section>

            <!-- 方案选择 -->
            <section class="section" data-section="3">
                <div class="section-header">
                    <span class="section-icon">🎯</span>
                    <h2 class="section-title">方案选择建议</h2>
                </div>
                <div class="section-content">
                    <h3 class="section-subtitle">方案对比(基于完整实测数据)</h3>
                    <table>
                        <thead>
                            <tr>
                                <th>方案</th>
                                <th>界面</th>
                                <th>不带 LoRA</th>
                                <th>带 LoRA</th>
                                <th>安装难度</th>
                                <th>实测评价</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td><strong>方案 1: ComfyUI Desktop</strong></td>
                                <td>节点工作流</td>
                                <td>300-400秒</td>
                                <td><strong>214秒</strong></td>
                                <td>非常简单</td>
                                <td><strong>最推荐</strong></td>
                            </tr>
                            <tr>
                                <td><strong>方案 2: Gradio Web UI</strong></td>
                                <td>Web UI</td>
                                <td><strong>278秒</strong></td>
                                <td>417秒</td>
                                <td>简单</td>
                                <td>仅限无 LoRA 场景</td>
                            </tr>
                        </tbody>
                    </table>

                    <div class="info-card">
                        <h4><strong>🏆 最终推荐: 方案 1 (ComfyUI Desktop)</strong></h4>
                        <p><strong>推荐理由:</strong></p>
                        <ol>
                            <li>✅ <strong>加载 LoRA 时最快</strong>: 214秒(唯一低于 4 分钟的方案)</li>
                            <li>⚡ <strong>不加 LoRA 也很快</strong>: 400秒左右,与 Gradio 量化版相当</li>
                            <li>🎨 <strong>节点工作流灵活</strong>: 适合复杂工作流组合</li>
                            <li>📦 <strong>安装简单</strong>: 官方应用,双击安装</li>
                            <li>🔧 <strong>LoRA 管理方便</strong>: 节点化操作,社区资源丰富</li>
                            <li>💻 <strong>跨平台支持</strong>: 同时支持 CUDA 显卡 & Apple Silicon 芯片 & AMD 显卡(仅限 Linux 系统)</li>
                        </ol>
                    </div>
                </div>
            </section>

            <!-- 安装步骤 -->
            <section class="section" data-section="4">
                <div class="section-header">
                    <span class="section-icon">⚙️</span>
                    <h2 class="section-title">推荐安装方案</h2>
                </div>
                <div class="section-content">
                    <h3 class="section-subtitle">方案 1: ComfyUI Desktop (推荐)</h3>
                    <p><strong>官网</strong>: <a href="https://www.comfy.org/" target="_blank">https://www.comfy.org/</a></p>

                    <h4><span class="step-number">1</span>下载 ComfyUI Desktop</h4>
                    <p><strong>官网下载</strong>: <a href="https://www.comfy.org/download" target="_blank">Download ComfyUI</a></p>
                    <div class="info-card">
                        <strong>系统要求:</strong>
                        <ul>
                            <li>macOS 12.3 或更高版本 / Windows 10, Windows 11</li>
                            <li>Apple Silicon(M1/M2/M3/M4) / CUDA 显卡</li>
                            <li>至少 5GB 磁盘空间</li>
                            <li>16GB+ 内存(推荐 32GB)</li>
                        </ul>
                    </div>

                    <h4><span class="step-number">2</span>安装应用</h4>
                    <ol>
                        <li>下载 .dmg 文件(Mac) 或 .exe 文件(Windows)</li>
                        <li>双击打开安装包</li>
                        <li>拖动 ComfyUI 到 Applications 文件夹(Mac)</li>
                        <li>首次打开需要在「系统设置 > 隐私与安全性」中允许</li>
                    </ol>

                    <h4><span class="step-number">3</span>启动 ComfyUI Desktop</h4>
                    <ol>
                        <li>打开应用后会自动启动本地服务器</li>
                        <li>启动后需要选择模型目录,建议选择 <code>~/ComfyUI</code></li>
                        <li>显示节点编辑器图形界面</li>
                    </ol>

                    <h4><span class="step-number">4</span>下载 Z-Image-Turbo 模型文件</h4>
                    <p>需要手动下载 <strong>3 个文件</strong>(共约 18GB):</p>
                    <table>
                        <thead>
                            <tr>
                                <th>文件名</th>
                                <th>大小</th>
                                <th>存放路径</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td>z_image_turbo_bf16.safetensors</td>
                                <td>11.46GB</td>
                                <td><code>~/ComfyUI/models/diffusion_models/</code></td>
                            </tr>
                            <tr>
                                <td>qwen_3_4b.safetensors</td>
                                <td>6.8GB</td>
                                <td><code>~/ComfyUI/models/text_encoders/</code></td>
                            </tr>
                            <tr>
                                <td>ae.safetensors</td>
                                <td>335MB</td>
                                <td><code>~/ComfyUI/models/vae/</code></td>
                            </tr>
                        </tbody>
                    </table>

                    <p><strong>使用 Shell 命令下载:</strong></p>
                    <div class="code-block">
                        <button class="copy-button" @click="copyCode($event)">复制</button>
                        <pre><code># 创建模型目录
mkdir -p ~/ComfyUI/models/diffusion_models
mkdir -p ~/ComfyUI/models/text_encoders
mkdir -p ~/ComfyUI/models/vae

# 下载主模型(11.46GB)
cd ~/ComfyUI/models/diffusion_models
wget https://huggingface.co/Comfy-Org/z_image_turbo/resolve/main/split_files/diffusion_models/z_image_turbo_bf16.safetensors

# 下载文本编码器(6.8GB)
cd ~/ComfyUI/models/text_encoders
wget https://huggingface.co/Comfy-Org/z_image_turbo/resolve/main/split_files/text_encoders/qwen_3_4b.safetensors

# 下载 VAE(335MB)
cd ~/ComfyUI/models/vae
wget https://huggingface.co/Comfy-Org/z_image_turbo/resolve/main/split_files/vae/ae.safetensors</code></pre>
                    </div>

                    <h4><span class="step-number">5</span>下载官方工作流</h4>
                    <div class="code-block">
                        <button class="copy-button" @click="copyCode($event)">复制</button>
                        <pre><code>cd ~/Downloads
wget https://raw.githubusercontent.com/Comfy-Org/workflow_templates/main/templates/image_z_image_turbo.json</code></pre>
                    </div>

                    <h4><span class="step-number">6</span>加载工作流</h4>
                    <p><strong>在 ComfyUI Desktop 界面中:</strong></p>
                    <ol>
                        <li>打开应用后,点击左侧的<strong>工作流</strong>,浏览工作流文件</li>
                        <li>直接拖拽 <code>image_z_image_turbo.json</code> 文件到画布中</li>
                        <li>或者点击右上角 <strong>Load</strong> → 选择 JSON 文件</li>
                        <li>所有节点会自动加载</li>
                    </ol>

                    <h4><span class="step-number">7</span>配置关键参数</h4>
                    <p><strong>在工作流节点中设置:</strong></p>
                    <ul>
                        <li><strong>CLIP Type</strong>: <code>Lumina 2</code> (必须!否则无法工作)</li>
                        <li><strong>Steps</strong>: <code>8</code> (官方推荐)</li>
                        <li><strong>CFG Scale</strong>: <code>1.0</code> (蒸馏模型推荐值)</li>
                        <li><strong>Resolution</strong>: <code>1024×1024</code> 或 <code>768×768</code></li>
                    </ul>

                    <h4><span class="step-number">8</span>生成第一张图片</h4>
                    <p><strong>输入提示词</strong>(中英文均可):</p>
                    <div class="code-block">
                        <button class="copy-button" @click="copyCode($event)">复制</button>
                        <pre><code>一位身穿泳装的亚洲美女站在泳池边,阳光明媚,专业摄影,8k高清

A young Asian woman in swimsuit by the pool, sunny day, professional photography, 8k</code></pre>
                    </div>
                    <p><strong>点击</strong>: 右上角 <strong>Queue Prompt</strong> 按钮(或快捷键 <code>Ctrl+Enter</code>)</p>

                    <h4><span class="step-number">9</span>查看生成结果</h4>
                    <ul>
                        <li>图片显示在界面右侧预览区</li>
                        <li>自动保存到 <code>~/ComfyUI/output/</code> 目录</li>
                    </ul>
                </div>
            </section>

            <!-- LoRA 使用 -->
            <section class="section" data-section="5">
                <div class="section-header">
                    <span class="section-icon">🎨</span>
                    <h2 class="section-title">LoRA 使用指南</h2>
                </div>
                <div class="section-content">
                    <h3 class="section-subtitle">什么是 LoRA?</h3>
                    <p><strong>LoRA = Low-Rank Adaptation(低秩适配)</strong></p>
                    <div class="info-card">
                        <strong>🎯 通俗解释:</strong>
                        <ul>
                            <li>基础模型 = 通用画家(会画各种风格)</li>
                            <li>LoRA = 风格插件(让画家学会特定风格)</li>
                            <li>不修改原始模型,只添加小文件(100-500MB)作为"风格调整层"</li>
                        </ul>
                    </div>

                    <p><strong>常见用途:</strong></p>
                    <ul>
                        <li>🎨 <strong>艺术风格</strong>: 像素风、油画风、水彩风、胶片风</li>
                        <li>🏛️ <strong>主题强化</strong>: 建筑细节、人像优化、风景增强</li>
                        <li>👤 <strong>特定角色</strong>: 动漫角色、特定 IP</li>
                    </ul>

                    <h3 class="section-subtitle">下载 LoRA 资源</h3>
                    <h4>推荐网站</h4>
                    <table>
                        <thead>
                            <tr>
                                <th>网站</th>
                                <th>免费</th>
                                <th>下载速度(国内)</th>
                                <th>资源量</th>
                                <th>推荐度</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td><strong>Civitai</strong></td>
                                <td>✅ 完全免费</td>
                                <td>中等</td>
                                <td>最多</td>
                                <td>⭐⭐⭐⭐⭐ 首选</td>
                            </tr>
                            <tr>
                                <td><strong>Hugging Face</strong></td>
                                <td>✅ 免费</td>
                                <td>慢(可用镜像)</td>
                                <td>一般</td>
                                <td>⭐⭐⭐ 备选</td>
                            </tr>
                            <tr>
                                <td><strong>GitHub</strong></td>
                                <td>✅ 免费</td>
                                <td>中等</td>
                                <td>最少</td>
                                <td>⭐⭐ 最后搜索</td>
                            </tr>
                        </tbody>
                    </table>

                    <h4>Civitai 下载步骤</h4>
                    <ol>
                        <li>访问 Civitai: <a href="https://civitai.com/" target="_blank">https://civitai.com/</a></li>
                        <li>搜索兼容的 LoRA:
                            <ul>
                                <li>搜索: <code>Flux LoRA</code></li>
                                <li>筛选: Base Model = <code>Flux.1</code> (重要!)</li>
                                <li>排序: 按下载量或评分</li>
                            </ul>
                        </li>
                        <li>直达链接(已筛选 Flux LoRA): <a href="https://civitai.com/models?modelType=LORA&baseModel=Flux.1" target="_blank">Civitai Flux LoRA</a></li>
                        <li>下载文件:
                            <ul>
                                <li>点击 <strong>Download</strong> 按钮</li>
                                <li>无需登录,直接下载 <code>.safetensors</code> 文件</li>
                            </ul>
                        </li>
                    </ol>

                    <div class="warning-card">
                        <strong>⚠️ 兼容性检查清单</strong><br>
                        在下载前,<strong>务必确认</strong>:
                        <ul>
                            <li>✅ <strong>Base Model 标注为 <code>Flux.1</code></strong></li>
                            <li>✅ <strong>文件格式为 <code>.safetensors</code></strong></li>
                            <li>✅ <strong>文件大小合理</strong>: 50MB - 500MB</li>
                        </ul>
                    </div>

                    <h3 class="section-subtitle">在 ComfyUI Desktop 中使用 LoRA</h3>
                    <ol>
                        <li>下载 LoRA 文件到 <code>~/ComfyUI/models/loras/</code></li>
                        <li>在工作流中激活 <code>LoRA Input</code> 节点,然后左右切换选择你下载的 LoRA 名称</li>
                        <li>设置 LoRA 强度为 <code>0.8</code> (推荐值)</li>
                        <li>重新生成</li>
                    </ol>

                    <div class="info-card">
                        <strong>🏆 关键结论:</strong>
                        <ul>
                            <li>✅ ComfyUI Desktop + LoRA 是<strong>唯一推荐的 LoRA 使用方案</strong>(214秒)</li>
                            <li>❌ Gradio + LoRA 不推荐(417秒)</li>
                            <li>⚡ Gradio 量化版仅适合不使用 LoRA 的场景(278秒)</li>
                        </ul>
                    </div>
                </div>
            </section>

            <!-- 常见问题 -->
            <section class="section" data-section="6">
                <div class="section-header">
                    <span class="section-icon">❓</span>
                    <h2 class="section-title">常见问题解决</h2>
                </div>
                <div class="section-content">
                    <h3 class="section-subtitle">Q1: 启动时报错 <code>ModuleNotFoundError: No module named '_lzma'</code></h3>
                    <p><strong>原因</strong>: pyenv 安装 Python 时缺少 <code>xz</code> 库。</p>
                    <p><strong>解决方法</strong>:</p>
                    <div class="code-block">
                        <button class="copy-button" @click="copyCode($event)">复制</button>
                        <pre><code># 1. 安装 xz
brew install xz

# 2. 重新安装 Python
pyenv uninstall 3.11.14
pyenv install 3.11.14

# 3. 验证
python -c "import lzma; print('lzma OK')"

# 4. 重新创建虚拟环境
rm -rf venv
python -m venv venv
source venv/bin/activate
pip install -r requirements.txt</code></pre>
                    </div>

                    <h3 class="section-subtitle">Q2: 首次生成特别慢(5 分钟以上)</h3>
                    <p><strong>这是正常现象!</strong>首次生成需要:</p>
                    <ol>
                        <li>下载模型到缓存(3.5GB 量化版或 32GB 完整版)</li>
                        <li>加载模型到内存</li>
                        <li>编译 Metal 着色器</li>
                        <li>预热 MPS 后端</li>
                    </ol>
                    <div class="info-card">
                        <strong>后续生成速度(实测):</strong>
                        <ul>
                            <li>ComfyUI Desktop + LoRA: <strong>214秒</strong></li>
                            <li>ComfyUI Desktop(无 LoRA): 300-400秒</li>
                            <li>Gradio 量化版(无 LoRA): 278秒</li>
                        </ul>
                    </div>

                    <h3 class="section-subtitle">Q3: 如何获得最快的生成速度?</h3>
                    <div class="info-card">
                        <strong>🚀 最快方案: ComfyUI Desktop + LoRA (214秒)</strong>
                        <p><strong>操作步骤:</strong></p>
                        <ol>
                            <li>使用 ComfyUI Desktop</li>
                            <li>在工作流中加载 LoRA 文件</li>
                            <li>生成速度: <strong>214秒/张</strong>(最快)</li>
                        </ol>
                    </div>

                    <h3 class="section-subtitle">Q4: 生成的图片质量不够好</h3>
                    <p><strong>调整建议:</strong></p>
                    <ol>
                        <li><strong>提高分辨率</strong>: 768×768 → 1024×1024</li>
                        <li><strong>增加步数</strong>(轻微提升): 8 steps → 10 steps</li>
                        <li><strong>优化提示词</strong>(最重要):
                            <div class="code-block">
                                <button class="copy-button" @click="copyCode($event)">复制</button>
                                <pre><code># 不够详细
一只猫

# 详细描述
一只橘色短毛猫坐在月球表面的岩石上,穿着宇航服,背景是星空和地球,
摄影级真实感,8k 超高清,专业摄影,电影级光线</code></pre>
                            </div>
                        </li>
                        <li><strong>加载合适的 LoRA</strong>:
                            <ul>
                                <li>人像: 人像增强 LoRA</li>
                                <li>风景: 风景细节 LoRA</li>
                                <li>艺术风格: 对应风格 LoRA</li>
                            </ul>
                        </li>
                    </ol>

                    <h3 class="section-subtitle">Q5: 可以删除量化版本只保留完整版吗?</h3>
                    <p><strong>看情况!</strong></p>
                    <div class="info-card">
                        <strong>如果你只用 ComfyUI Desktop:</strong>
                        <ul>
                            <li>✅ <strong>可以删除</strong>量化版本</li>
                            <li>ComfyUI Desktop 不使用量化版本</li>
                            <li>只需保留完整版(18GB)</li>
                        </ul>
                    </div>
                    <div class="warning-card">
                        <strong>如果你也用 Gradio:</strong>
                        <ul>
                            <li>❌ 不建议删除</li>
                            <li>不加 LoRA 时,量化版速度和 ComfyUI 相当(278秒 vs 300-400秒)</li>
                            <li>量化版内存占用更小(5GB vs 12GB)</li>
                        </ul>
                    </div>
                </div>
            </section>

            <!-- 参考资料 -->
            <section class="section" data-section="7">
                <div class="section-header">
                    <span class="section-icon">📚</span>
                    <h2 class="section-title">参考资料</h2>
                </div>
                <div class="section-content">
                    <h3 class="section-subtitle">官方文档</h3>
                    <ul>
                        <li><a href="https://github.com/Tongyi-MAI/Z-Image" target="_blank">Z-Image Official GitHub</a></li>
                        <li><a href="https://huggingface.co/Tongyi-MAI/Z-Image-Turbo" target="_blank">Z-Image-Turbo on Hugging Face</a></li>
                        <li><a href="https://github.com/newideas99/Ultra-Fast-Image-Generation-Mac-Silicon-Z-Image" target="_blank">Ultra-Fast-Image-Generation GitHub</a></li>
                    </ul>

                    <h3 class="section-subtitle">社区资源</h3>
                    <ul>
                        <li><a href="https://civitai.com/" target="_blank">Civitai - LoRA 社区</a></li>
                        <li><a href="https://huggingface.co/models" target="_blank">Hugging Face - 官方模型库</a></li>
                        <li><a href="https://docs.comfy.org/" target="_blank">ComfyUI 官方文档</a></li>
                    </ul>

                    <h3 class="section-subtitle">技术论文</h3>
                    <ul>
                        <li><a href="https://github.com/Tongyi-MAI/Z-Image/blob/main/docs/technical_report.pdf" target="_blank">Z-Image Technical Report</a></li>
                        <li><a href="https://arxiv.org/abs/2407.04693" target="_blank">Flux Architecture Paper</a></li>
                    </ul>

                    <h3 class="section-subtitle">社区讨论</h3>
                    <ul>
                        <li><a href="https://huggingface.co/Tongyi-MAI/Z-Image-Turbo/discussions" target="_blank">Hugging Face Discussions</a></li>
                    </ul>
                </div>
            </section>
        </main>
    </div>

    <script>
        function tutorial() {
            return {
                darkMode: false,
                searchQuery: '',
                scrollProgress: 0,
                activeSection: 0,
                sections: [
                    { icon: '📖', title: '前言' },
                    { icon: '🚀', title: '什么是 Z-Image-Turbo' },
                    { icon: '💻', title: '硬件要求与性能' },
                    { icon: '🎯', title: '方案选择建议' },
                    { icon: '⚙️', title: '推荐安装方案' },
                    { icon: '🎨', title: 'LoRA 使用指南' },
                    { icon: '❓', title: '常见问题解决' },
                    { icon: '📚', title: '参考资料' }
                ],

                init() {
                    // 监听滚动事件
                    window.addEventListener('scroll', () => {
                        this.updateScrollProgress();
                        this.updateActiveSection();
                    });

                    // 检测系统主题偏好
                    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
                        this.darkMode = true;
                    }
                },

                updateScrollProgress() {
                    const winScroll = document.documentElement.scrollTop;
                    const height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
                    this.scrollProgress = (winScroll / height) * 100;
                },

                updateActiveSection() {
                    const sections = document.querySelectorAll('.section');
                    let current = 0;
                    
                    sections.forEach((section, index) => {
                        const rect = section.getBoundingClientRect();
                        if (rect.top <= 150) {
                            current = index;
                        }
                    });
                    
                    this.activeSection = current;
                },

                scrollToSection(index) {
                    const sections = document.querySelectorAll('.section');
                    if (sections[index]) {
                        sections[index].scrollIntoView({ behavior: 'smooth', block: 'start' });
                    }
                },

                toggleTheme() {
                    this.darkMode = !this.darkMode;
                },

                searchContent() {
                    // 移除之前的高亮
                    document.querySelectorAll('.highlight').forEach(el => {
                        el.outerHTML = el.innerHTML;
                    });

                    if (!this.searchQuery.trim()) return;

                    // 高亮搜索结果
                    const content = document.querySelector('.content');
                    const regex = new RegExp(`(${this.searchQuery})`, 'gi');
                    
                    const walk = document.createTreeWalker(
                        content,
                        NodeFilter.SHOW_TEXT,
                        null,
                        false
                    );

                    const textNodes = [];
                    while (walk.nextNode()) {
                        if (walk.currentNode.parentNode.nodeName !== 'SCRIPT' &&
                            walk.currentNode.parentNode.nodeName !== 'STYLE') {
                            textNodes.push(walk.currentNode);
                        }
                    }

                    textNodes.forEach(node => {
                        const text = node.nodeValue;
                        if (regex.test(text)) {
                            const span = document.createElement('span');
                            span.innerHTML = text.replace(regex, '<span class="highlight">$1</span>');
                            node.parentNode.replaceChild(span, node);
                        }
                    });
                },

                copyCode(event) {
                    const button = event.target;
                    const codeBlock = button.nextElementSibling;
                    const code = codeBlock.textContent;

                    navigator.clipboard.writeText(code).then(() => {
                        button.textContent = '✅ 已复制!';
                        button.classList.add('copied');
                        
                        setTimeout(() => {
                            button.textContent = '复制';
                            button.classList.remove('copied');
                        }, 2000);
                    });
                }
            }
        }
    </script>
</body>
</html>
""", "text/html; charset=utf-8"));

Console.WriteLine("🚀 Z-Image-Turbo 交互式教程已启动!");
Console.WriteLine("📖 访问: http://localhost:5000");
Console.WriteLine("✨ 按 Ctrl+C 停止服务器");

app.Run("http://localhost:5000");
