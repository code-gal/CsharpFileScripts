#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net10.0
#:property LangVersion=preview

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 首页 - 完整的交互式简历
app.MapGet("/", () => Results.Content("""
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>张晓明的交互式简历</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
            color: #333;
        }
        
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: rgba(255, 255, 255, 0.95);
            border-radius: 20px;
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
            overflow: hidden;
        }
        
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 60px 40px;
            text-align: center;
            position: relative;
            overflow: hidden;
        }
        
        .header::before {
            content: '';
            position: absolute;
            top: -50%;
            left: -50%;
            width: 200%;
            height: 200%;
            background: radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%);
            animation: pulse 4s ease-in-out infinite;
        }
        
        @keyframes pulse {
            0%, 100% { transform: scale(1); }
            50% { transform: scale(1.1); }
        }
        
        .header h1 {
            font-size: 3em;
            margin-bottom: 10px;
            position: relative;
            z-index: 1;
        }
        
        .header p {
            font-size: 1.3em;
            opacity: 0.9;
            position: relative;
            z-index: 1;
        }
        
        .nav-tabs {
            display: flex;
            background: #f8f9fa;
            border-bottom: 2px solid #e9ecef;
            overflow-x: auto;
        }
        
        .nav-tab {
            padding: 15px 30px;
            cursor: pointer;
            border: none;
            background: none;
            font-size: 16px;
            font-weight: 600;
            color: #666;
            transition: all 0.3s;
            border-bottom: 3px solid transparent;
            white-space: nowrap;
        }
        
        .nav-tab:hover {
            background: rgba(102, 126, 234, 0.1);
            color: #667eea;
        }
        
        .nav-tab.active {
            color: #667eea;
            border-bottom-color: #667eea;
        }
        
        .content {
            padding: 40px;
        }
        
        .tab-content {
            display: none;
            animation: fadeIn 0.5s;
        }
        
        .tab-content.active {
            display: block;
        }
        
        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(20px); }
            to { opacity: 1; transform: translateY(0); }
        }
        
        .info-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }
        
        .info-card {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 25px;
            border-radius: 15px;
            color: white;
            box-shadow: 0 5px 15px rgba(102, 126, 234, 0.3);
            transition: transform 0.3s;
        }
        
        .info-card:hover {
            transform: translateY(-5px);
        }
        
        .info-card h3 {
            font-size: 0.9em;
            opacity: 0.9;
            margin-bottom: 10px;
        }
        
        .info-card p {
            font-size: 1.3em;
            font-weight: bold;
        }
        
        .skill-chart {
            background: white;
            padding: 30px;
            border-radius: 15px;
            box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
            margin-bottom: 30px;
        }
        
        .skill-item {
            margin-bottom: 20px;
        }
        
        .skill-name {
            display: flex;
            justify-content: space-between;
            margin-bottom: 8px;
            font-weight: 600;
        }
        
        .skill-bar {
            background: #e9ecef;
            height: 12px;
            border-radius: 10px;
            overflow: hidden;
        }
        
        .skill-progress {
            height: 100%;
            background: linear-gradient(90deg, #667eea 0%, #764ba2 100%);
            border-radius: 10px;
            transition: width 1s ease-out;
            width: 0;
        }
        
        .timeline {
            position: relative;
            padding-left: 30px;
        }
        
        .timeline::before {
            content: '';
            position: absolute;
            left: 0;
            top: 0;
            bottom: 0;
            width: 3px;
            background: linear-gradient(180deg, #667eea 0%, #764ba2 100%);
        }
        
        .timeline-item {
            position: relative;
            margin-bottom: 40px;
            padding: 20px;
            background: white;
            border-radius: 15px;
            box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
            transition: transform 0.3s;
        }
        
        .timeline-item:hover {
            transform: translateX(10px);
        }
        
        .timeline-item::before {
            content: '';
            position: absolute;
            left: -37px;
            top: 25px;
            width: 15px;
            height: 15px;
            background: #667eea;
            border: 3px solid white;
            border-radius: 50%;
            box-shadow: 0 0 0 3px #667eea;
        }
        
        .timeline-date {
            color: #667eea;
            font-weight: bold;
            margin-bottom: 10px;
        }
        
        .timeline-title {
            font-size: 1.3em;
            font-weight: bold;
            margin-bottom: 10px;
        }
        
        .timeline-desc {
            color: #666;
            line-height: 1.6;
        }
        
        .tools-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 30px;
        }
        
        .tool-card {
            background: white;
            padding: 25px;
            border-radius: 15px;
            box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
        }
        
        .tool-card h3 {
            color: #667eea;
            margin-bottom: 20px;
            font-size: 1.3em;
        }
        
        .calculator-grid {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 10px;
            margin-top: 15px;
        }
        
        .calculator-display {
            grid-column: 1 / -1;
            background: #f8f9fa;
            padding: 20px;
            border-radius: 10px;
            text-align: right;
            font-size: 1.8em;
            font-weight: bold;
            margin-bottom: 10px;
            min-height: 60px;
            display: flex;
            align-items: center;
            justify-content: flex-end;
        }
        
        .calc-btn {
            padding: 20px;
            font-size: 1.2em;
            border: none;
            background: #f8f9fa;
            border-radius: 10px;
            cursor: pointer;
            transition: all 0.2s;
            font-weight: 600;
        }
        
        .calc-btn:hover {
            background: #e9ecef;
            transform: scale(1.05);
        }
        
        .calc-btn.operator {
            background: #667eea;
            color: white;
        }
        
        .calc-btn.operator:hover {
            background: #5568d3;
        }
        
        .calc-btn.equals {
            background: #764ba2;
            color: white;
            grid-column: span 2;
        }
        
        .calc-btn.equals:hover {
            background: #63408a;
        }
        
        .converter {
            display: flex;
            flex-direction: column;
            gap: 15px;
        }
        
        .converter input, .converter select {
            padding: 12px;
            border: 2px solid #e9ecef;
            border-radius: 10px;
            font-size: 1em;
            transition: border-color 0.3s;
        }
        
        .converter input:focus, .converter select:focus {
            outline: none;
            border-color: #667eea;
        }
        
        .color-picker-tool {
            display: flex;
            flex-direction: column;
            gap: 15px;
        }
        
        .color-preview {
            height: 100px;
            border-radius: 10px;
            border: 3px solid #e9ecef;
            transition: all 0.3s;
        }
        
        .color-input {
            padding: 12px;
            border: 2px solid #e9ecef;
            border-radius: 10px;
            font-size: 1.1em;
            font-family: monospace;
        }
        
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
        }
        
        .stat-box {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 30px;
            border-radius: 15px;
            color: white;
            text-align: center;
            animation: countUp 2s ease-out;
        }
        
        .stat-number {
            font-size: 3em;
            font-weight: bold;
            margin-bottom: 10px;
        }
        
        .stat-label {
            font-size: 1em;
            opacity: 0.9;
        }
        
        @keyframes countUp {
            from { opacity: 0; transform: scale(0.5); }
            to { opacity: 1; transform: scale(1); }
        }
        
        .btn-primary {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 12px 30px;
            border: none;
            border-radius: 10px;
            font-size: 1em;
            font-weight: 600;
            cursor: pointer;
            transition: transform 0.2s;
        }
        
        .btn-primary:hover {
            transform: scale(1.05);
        }
        
        .result-box {
            margin-top: 15px;
            padding: 15px;
            background: #f8f9fa;
            border-radius: 10px;
            border-left: 4px solid #667eea;
        }
        
        @media (max-width: 768px) {
            .header h1 { font-size: 2em; }
            .header p { font-size: 1em; }
            .content { padding: 20px; }
            .nav-tab { padding: 12px 20px; font-size: 14px; }
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>👨‍💻 张晓明</h1>
            <p>全栈开发工程师 | .NET & 前端技术专家</p>
        </div>
        
        <div class="nav-tabs">
            <button class="nav-tab active" onclick="switchTab('about')">📋 关于我</button>
            <button class="nav-tab" onclick="switchTab('skills')">🎯 技能</button>
            <button class="nav-tab" onclick="switchTab('projects')">💼 项目经历</button>
            <button class="nav-tab" onclick="switchTab('tools')">🛠️ 实用工具</button>
            <button class="nav-tab" onclick="switchTab('stats')">📊 数据看板</button>
        </div>
        
        <div class="content">
            <!-- 关于我 -->
            <div id="about" class="tab-content active">
                <div class="info-grid">
                    <div class="info-card">
                        <h3>📧 邮箱</h3>
                        <p>zhang@example.com</p>
                    </div>
                    <div class="info-card">
                        <h3>📱 电话</h3>
                        <p>138-0000-0000</p>
                    </div>
                    <div class="info-card">
                        <h3>📍 位置</h3>
                        <p>北京·朝阳区</p>
                    </div>
                    <div class="info-card">
                        <h3>💼 经验</h3>
                        <p>5年+</p>
                    </div>
                </div>
                
                <div class="skill-chart">
                    <h2 style="margin-bottom: 25px; color: #667eea;">🎓 个人简介</h2>
                    <p style="line-height: 1.8; color: #666; font-size: 1.1em;">
                        热爱技术的全栈开发工程师，拥有5年以上的开发经验。精通 .NET 生态系统，
                        熟悉前端现代化框架。擅长构建高性能、可扩展的企业级应用。
                        对新技术充满热情，喜欢通过代码解决实际问题。
                    </p>
                </div>
            </div>
            
            <!-- 技能 -->
            <div id="skills" class="tab-content">
                <div class="skill-chart">
                    <h2 style="margin-bottom: 25px; color: #667eea;">💻 技术技能</h2>
                    <div class="skill-item">
                        <div class="skill-name">
                            <span>C# / .NET</span>
                            <span>95%</span>
                        </div>
                        <div class="skill-bar">
                            <div class="skill-progress" data-width="95%"></div>
                        </div>
                    </div>
                    <div class="skill-item">
                        <div class="skill-name">
                            <span>JavaScript / TypeScript</span>
                            <span>90%</span>
                        </div>
                        <div class="skill-bar">
                            <div class="skill-progress" data-width="90%"></div>
                        </div>
                    </div>
                    <div class="skill-item">
                        <div class="skill-name">
                            <span>ASP.NET Core</span>
                            <span>93%</span>
                        </div>
                        <div class="skill-bar">
                            <div class="skill-progress" data-width="93%"></div>
                        </div>
                    </div>
                    <div class="skill-item">
                        <div class="skill-name">
                            <span>React / Vue.js</span>
                            <span>85%</span>
                        </div>
                        <div class="skill-bar">
                            <div class="skill-progress" data-width="85%"></div>
                        </div>
                    </div>
                    <div class="skill-item">
                        <div class="skill-name">
                            <span>SQL / Entity Framework</span>
                            <span>88%</span>
                        </div>
                        <div class="skill-bar">
                            <div class="skill-progress" data-width="88%"></div>
                        </div>
                    </div>
                    <div class="skill-item">
                        <div class="skill-name">
                            <span>Docker / Kubernetes</span>
                            <span>80%</span>
                        </div>
                        <div class="skill-bar">
                            <div class="skill-progress" data-width="80%"></div>
                        </div>
                    </div>
                    <div class="skill-item">
                        <div class="skill-name">
                            <span>Azure / AWS</span>
                            <span>75%</span>
                        </div>
                        <div class="skill-bar">
                            <div class="skill-progress" data-width="75%"></div>
                        </div>
                    </div>
                </div>
            </div>
            
            <!-- 项目经历 -->
            <div id="projects" class="tab-content">
                <div class="timeline">
                    <div class="timeline-item">
                        <div class="timeline-date">2023.06 - 至今</div>
                        <div class="timeline-title">🏢 企业级ERP系统</div>
                        <div class="timeline-desc">
                            负责核心业务模块的架构设计与开发。使用 .NET 8 + React 构建，
                            支持高并发场景，日均处理订单10万+。实现了模块化设计，
                            系统响应时间优化至200ms以内。
                        </div>
                    </div>
                    
                    <div class="timeline-item">
                        <div class="timeline-date">2022.03 - 2023.05</div>
                        <div class="timeline-title">📱 智能物联网平台</div>
                        <div class="timeline-desc">
                            开发物联网设备管理平台，接入设备数量达50000+。
                            使用 ASP.NET Core + SignalR 实现实时数据推送，
                            构建了完善的设备监控、告警、数据分析系统。
                        </div>
                    </div>
                    
                    <div class="timeline-item">
                        <div class="timeline-date">2020.09 - 2022.02</div>
                        <div class="timeline-title">🛒 电商平台后台系统</div>
                        <div class="timeline-desc">
                            参与大型电商平台的后台管理系统开发。负责商品管理、
                            订单处理、库存管理等核心模块。优化数据库查询性能，
                            将复杂查询响应时间降低60%。
                        </div>
                    </div>
                    
                    <div class="timeline-item">
                        <div class="timeline-date">2019.07 - 2020.08</div>
                        <div class="timeline-title">📊 数据可视化分析平台</div>
                        <div class="timeline-desc">
                            构建企业级数据可视化分析平台，支持多维度数据分析和报表生成。
                            使用 .NET + ECharts 实现丰富的图表展示，
                            为管理层决策提供数据支持。
                        </div>
                    </div>
                </div>
            </div>
            
            <!-- 实用工具 -->
            <div id="tools" class="tab-content">
                <div class="tools-grid">
                    <!-- 计算器 -->
                    <div class="tool-card">
                        <h3>🧮 计算器</h3>
                        <div class="calculator-display" id="calcDisplay">0</div>
                        <div class="calculator-grid">
                            <button class="calc-btn" onclick="appendCalc('7')">7</button>
                            <button class="calc-btn" onclick="appendCalc('8')">8</button>
                            <button class="calc-btn" onclick="appendCalc('9')">9</button>
                            <button class="calc-btn operator" onclick="appendCalc('/')">÷</button>
                            <button class="calc-btn" onclick="appendCalc('4')">4</button>
                            <button class="calc-btn" onclick="appendCalc('5')">5</button>
                            <button class="calc-btn" onclick="appendCalc('6')">6</button>
                            <button class="calc-btn operator" onclick="appendCalc('*')">×</button>
                            <button class="calc-btn" onclick="appendCalc('1')">1</button>
                            <button class="calc-btn" onclick="appendCalc('2')">2</button>
                            <button class="calc-btn" onclick="appendCalc('3')">3</button>
                            <button class="calc-btn operator" onclick="appendCalc('-')">-</button>
                            <button class="calc-btn" onclick="appendCalc('0')">0</button>
                            <button class="calc-btn" onclick="appendCalc('.')">.</button>
                            <button class="calc-btn operator" onclick="clearCalc()">C</button>
                            <button class="calc-btn operator" onclick="appendCalc('+')">+</button>
                            <button class="calc-btn equals" onclick="calculateResult()">=</button>
                        </div>
                    </div>
                    
                    <!-- 单位转换器 -->
                    <div class="tool-card">
                        <h3>📏 长度转换器</h3>
                        <div class="converter">
                            <input type="number" id="lengthInput" placeholder="输入数值" value="1">
                            <select id="lengthFrom">
                                <option value="m">米 (m)</option>
                                <option value="km">千米 (km)</option>
                                <option value="cm">厘米 (cm)</option>
                                <option value="mm">毫米 (mm)</option>
                                <option value="ft">英尺 (ft)</option>
                                <option value="in">英寸 (in)</option>
                            </select>
                            <div style="text-align: center; font-size: 1.5em; color: #667eea;">⬇️</div>
                            <select id="lengthTo">
                                <option value="m">米 (m)</option>
                                <option value="km">千米 (km)</option>
                                <option value="cm" selected>厘米 (cm)</option>
                                <option value="mm">毫米 (mm)</option>
                                <option value="ft">英尺 (ft)</option>
                                <option value="in">英寸 (in)</option>
                            </select>
                            <button class="btn-primary" onclick="convertLength()">转换</button>
                            <div id="lengthResult" class="result-box" style="display: none;"></div>
                        </div>
                    </div>
                    
                    <!-- 颜色选择器 -->
                    <div class="tool-card">
                        <h3>🎨 颜色选择器</h3>
                        <div class="color-picker-tool">
                            <div class="color-preview" id="colorPreview" style="background: #667eea;"></div>
                            <input type="color" id="colorPicker" value="#667eea" 
                                   style="width: 100%; height: 50px; border: none; border-radius: 10px; cursor: pointer;"
                                   onchange="updateColor()">
                            <input type="text" class="color-input" id="colorHex" value="#667eea" readonly>
                            <button class="btn-primary" onclick="copyColor()">复制颜色代码</button>
                        </div>
                    </div>
                    
                    <!-- 随机密码生成器 -->
                    <div class="tool-card">
                        <h3>🔐 密码生成器</h3>
                        <div class="converter">
                            <label style="display: flex; align-items: center; gap: 10px;">
                                <input type="checkbox" id="pwdUpper" checked style="width: 20px; height: 20px;">
                                <span>包含大写字母</span>
                            </label>
                            <label style="display: flex; align-items: center; gap: 10px;">
                                <input type="checkbox" id="pwdLower" checked style="width: 20px; height: 20px;">
                                <span>包含小写字母</span>
                            </label>
                            <label style="display: flex; align-items: center; gap: 10px;">
                                <input type="checkbox" id="pwdNumbers" checked style="width: 20px; height: 20px;">
                                <span>包含数字</span>
                            </label>
                            <label style="display: flex; align-items: center; gap: 10px;">
                                <input type="checkbox" id="pwdSymbols" checked style="width: 20px; height: 20px;">
                                <span>包含符号</span>
                            </label>
                            <input type="number" id="pwdLength" value="16" min="4" max="32" placeholder="密码长度">
                            <button class="btn-primary" onclick="generatePassword()">生成密码</button>
                            <div id="passwordResult" class="result-box" style="display: none;"></div>
                        </div>
                    </div>
                </div>
            </div>
            
            <!-- 数据看板 -->
            <div id="stats" class="tab-content">
                <h2 style="margin-bottom: 30px; color: #667eea; text-align: center;">📊 职业数据统计</h2>
                <div class="stats-grid">
                    <div class="stat-box">
                        <div class="stat-number">50+</div>
                        <div class="stat-label">完成项目</div>
                    </div>
                    <div class="stat-box">
                        <div class="stat-number">100K+</div>
                        <div class="stat-label">代码行数</div>
                    </div>
                    <div class="stat-box">
                        <div class="stat-number">15+</div>
                        <div class="stat-label">技术栈</div>
                    </div>
                    <div class="stat-box">
                        <div class="stat-number">99.9%</div>
                        <div class="stat-label">项目成功率</div>
                    </div>
                </div>
                
                <div class="skill-chart" style="margin-top: 30px;">
                    <h3 style="margin-bottom: 20px; color: #667eea;">⏱️ 实时时钟</h3>
                    <div style="text-align: center; font-size: 3em; font-weight: bold; color: #667eea; padding: 30px;" id="liveClock">
                        --:--:--
                    </div>
                </div>
                
                <div class="skill-chart" style="margin-top: 30px;">
                    <h3 style="margin-bottom: 20px; color: #667eea;">🎲 随机引语</h3>
                    <div style="text-align: center; font-size: 1.3em; line-height: 1.8; color: #666; padding: 20px;" id="randomQuote">
                        点击下方按钮获取灵感
                    </div>
                    <div style="text-align: center; margin-top: 20px;">
                        <button class="btn-primary" onclick="getRandomQuote()">获取新引语</button>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <script>
        // 标签页切换
        function switchTab(tabName) {
            document.querySelectorAll('.tab-content').forEach(tab => {
                tab.classList.remove('active');
            });
            document.querySelectorAll('.nav-tab').forEach(btn => {
                btn.classList.remove('active');
            });
            document.getElementById(tabName).classList.add('active');
            event.target.classList.add('active');
            
            // 技能进度条动画
            if (tabName === 'skills') {
                setTimeout(() => {
                    document.querySelectorAll('.skill-progress').forEach(bar => {
                        bar.style.width = bar.getAttribute('data-width');
                    });
                }, 100);
            }
        }
        
        // 初始化技能进度条
        window.addEventListener('load', () => {
            document.querySelectorAll('.skill-progress').forEach(bar => {
                bar.style.width = bar.getAttribute('data-width');
            });
        });
        
        // 计算器
        let calcValue = '0';
        let calcOperator = '';
        let calcPrevious = '';
        
        function appendCalc(value) {
            if (calcValue === '0' && value !== '.') {
                calcValue = value;
            } else {
                calcValue += value;
            }
            document.getElementById('calcDisplay').textContent = calcValue;
        }
        
        function clearCalc() {
            calcValue = '0';
            calcOperator = '';
            calcPrevious = '';
            document.getElementById('calcDisplay').textContent = calcValue;
        }
        
        function calculateResult() {
            try {
                calcValue = eval(calcValue).toString();
                document.getElementById('calcDisplay').textContent = calcValue;
            } catch (e) {
                document.getElementById('calcDisplay').textContent = '错误';
                calcValue = '0';
            }
        }
        
        // 长度转换
        function convertLength() {
            const input = parseFloat(document.getElementById('lengthInput').value);
            const from = document.getElementById('lengthFrom').value;
            const to = document.getElementById('lengthTo').value;
            
            const units = {
                m: 1,
                km: 1000,
                cm: 0.01,
                mm: 0.001,
                ft: 0.3048,
                in: 0.0254
            };
            
            const meters = input * units[from];
            const result = meters / units[to];
            
            const resultDiv = document.getElementById('lengthResult');
            resultDiv.style.display = 'block';
            resultDiv.innerHTML = `<strong>${input} ${from}</strong> = <strong style="color: #667eea; font-size: 1.2em;">${result.toFixed(4)} ${to}</strong>`;
        }
        
        // 颜色选择器
        function updateColor() {
            const color = document.getElementById('colorPicker').value;
            document.getElementById('colorPreview').style.background = color;
            document.getElementById('colorHex').value = color;
        }
        
        function copyColor() {
            const colorHex = document.getElementById('colorHex');
            colorHex.select();
            document.execCommand('copy');
            alert('颜色代码已复制: ' + colorHex.value);
        }
        
        // 密码生成器
        function generatePassword() {
            const length = parseInt(document.getElementById('pwdLength').value);
            const upper = document.getElementById('pwdUpper').checked;
            const lower = document.getElementById('pwdLower').checked;
            const numbers = document.getElementById('pwdNumbers').checked;
            const symbols = document.getElementById('pwdSymbols').checked;
            
            let chars = '';
            if (upper) chars += 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
            if (lower) chars += 'abcdefghijklmnopqrstuvwxyz';
            if (numbers) chars += '0123456789';
            if (symbols) chars += '!@#$%^&*()_+-=[]{}|;:,.<>?';
            
            if (chars === '') {
                alert('请至少选择一种字符类型！');
                return;
            }
            
            let password = '';
            for (let i = 0; i < length; i++) {
                password += chars.charAt(Math.floor(Math.random() * chars.length));
            }
            
            const resultDiv = document.getElementById('passwordResult');
            resultDiv.style.display = 'block';
            resultDiv.innerHTML = `<strong style="color: #667eea; font-size: 1.2em; font-family: monospace;">${password}</strong>
                <br><button class="btn-primary" style="margin-top: 10px;" onclick="copyPassword('${password}')">复制密码</button>`;
        }
        
        function copyPassword(pwd) {
            navigator.clipboard.writeText(pwd);
            alert('密码已复制！');
        }
        
        // 实时时钟
        function updateClock() {
            const now = new Date();
            const hours = String(now.getHours()).padStart(2, '0');
            const minutes = String(now.getMinutes()).padStart(2, '0');
            const seconds = String(now.getSeconds()).padStart(2, '0');
            document.getElementById('liveClock').textContent = `${hours}:${minutes}:${seconds}`;
        }
        setInterval(updateClock, 1000);
        updateClock();
        
        // 随机引语
        const quotes = [
            "代码是诗歌，程序员是诗人。",
            "优秀的代码是最好的文档。",
            "简单是可靠的前提。",
            "过早的优化是万恶之源。",
            "任何傻瓜都能写出计算机能理解的代码，优秀的程序员写出人类能理解的代码。",
            "测试无法证明程序没有错误,只能证明程序有错误。",
            "程序必须为人而写,顺便能在机器上运行。",
            "计算机科学中只有两个难题:缓存失效和命名。",
            "调试的难度是写代码的两倍。因此,如果你写代码时已经竭尽所能,那你的智商就不够调试了。",
            "好的程序员用脑子思考,而不是用手指编码。"
        ];
        
        function getRandomQuote() {
            const quote = quotes[Math.floor(Math.random() * quotes.length)];
            document.getElementById('randomQuote').textContent = `"${quote}"`;
        }
    </script>
</body>
</html>
""", "text/html"));

app.Run("http://localhost:5000");
