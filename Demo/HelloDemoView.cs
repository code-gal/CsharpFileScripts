#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk.Web

using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
});

var app = builder.Build();

// 主页路由
app.MapGet("/", () => Results.Content(TaskHub.HTML_CONTENT, "text/html"));

// API 路由
// Use Results.Ok to avoid RequiresUnreferencedCode/RequiresDynamicCode warnings
// that arise from Results.Json overloads which rely on reflection/runtime
// JSON serialization. Let the framework's formatters handle serialization
// to be more trimming/AOT friendly.
app.MapGet("/api/tasks", () => Results.Ok(TaskManager.GetAllTasks()));

app.MapPost("/api/tasks", async (HttpContext context) =>
{
    var task = await context.Request.ReadFromJsonAsync<TaskItem>();
    if (task != null)
    {
        TaskManager.AddTask(task);
        return Results.Ok(task);
    }
    return Results.BadRequest();
});

app.MapPut("/api/tasks/{id}/status", (string id, string status) =>
{
    TaskManager.UpdateTaskStatus(id, status);
    return Results.Ok();
});

app.MapDelete("/api/tasks/{id}", (string id) =>
{
    TaskManager.DeleteTask(id);
    return Results.Ok();
});

app.MapHub<TaskHub>("/taskHub");

await app.RunAsync();

// ========== 类型声明必须在顶级语句之后 ==========

// 任务模型
record TaskItem(string Id, string Title, string Description, string Status, string Priority, DateTime CreatedAt);

// 任务管理器
static class TaskManager
{
    private static readonly ConcurrentDictionary<string, TaskItem> tasks = new();

    public static void AddTask(TaskItem task)
    {
        tasks[task.Id] = task;
    }

    public static void UpdateTaskStatus(string id, string status)
    {
        if (tasks.TryGetValue(id, out var task))
        {
            tasks[id] = task with { Status = status };
        }
    }

    public static void DeleteTask(string id)
    {
        tasks.TryRemove(id, out _);
    }

    public static List<TaskItem> GetAllTasks() => tasks.Values.ToList();
}

// SignalR Hub
class TaskHub : Hub
{
    public async Task NotifyTaskAdded(TaskItem task)
    {
        try
        {
            TaskManager.AddTask(task);
            await Clients.All.SendAsync("TaskAdded", task);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"添加任务失败: {ex.Message}");
            throw;
        }
    }

    public async Task NotifyTaskMoved(string id, string newStatus)
    {
        try
        {
            TaskManager.UpdateTaskStatus(id, newStatus);
            await Clients.All.SendAsync("TaskMoved", id, newStatus);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"移动任务失败: {ex.Message}");
            throw;
        }
    }

    public async Task NotifyTaskDeleted(string id)
    {
        try
        {
            TaskManager.DeleteTask(id);
            await Clients.All.SendAsync("TaskDeleted", id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除任务失败: {ex.Message}");
            throw;
        }
    }

    // HTML 内容移到这里作为静态字段
    public const string HTML_CONTENT = """
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>实时任务看板 - C# 14 & .NET 10 Demo</title>
    <script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }

        .container {
            max-width: 1400px;
            margin: 0 auto;
        }

        header {
            text-align: center;
            color: white;
            margin-bottom: 30px;
        }

        header h1 {
            font-size: 2.5rem;
            margin-bottom: 10px;
            text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
        }

        header p {
            font-size: 1.1rem;
            opacity: 0.9;
        }

        .add-task-section {
            background: white;
            padding: 20px;
            border-radius: 10px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
            margin-bottom: 30px;
        }

        .add-task-form {
            display: grid;
            grid-template-columns: 2fr 3fr 1fr auto;
            gap: 10px;
            align-items: end;
        }

        .form-group {
            display: flex;
            flex-direction: column;
        }

        .form-group label {
            font-weight: 600;
            margin-bottom: 5px;
            color: #333;
        }

        input, textarea, select {
            padding: 10px;
            border: 2px solid #e0e0e0;
            border-radius: 5px;
            font-size: 14px;
            transition: border-color 0.3s;
        }

        input:focus, textarea:focus, select:focus {
            outline: none;
            border-color: #667eea;
        }

        textarea {
            resize: vertical;
            min-height: 60px;
        }

        button {
            padding: 10px 20px;
            background: #667eea;
            color: white;
            border: none;
            border-radius: 5px;
            cursor: pointer;
            font-size: 16px;
            font-weight: 600;
            transition: background 0.3s;
        }

        button:hover {
            background: #5568d3;
        }

        .board {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 20px;
        }

        .column {
            background: rgba(255, 255, 255, 0.95);
            border-radius: 10px;
            padding: 20px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }

        .column-header {
            display: flex;
            align-items: center;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 3px solid;
        }

        .column-header h2 {
            font-size: 1.3rem;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .column-header .count {
            background: #f0f0f0;
            padding: 2px 8px;
            border-radius: 12px;
            font-size: 0.9rem;
        }

        .todo .column-header {
            border-color: #3b82f6;
        }

        .in-progress .column-header {
            border-color: #f59e0b;
        }

        .done .column-header {
            border-color: #10b981;
        }

        .task-list {
            min-height: 400px;
        }

        .task-card {
            background: white;
            padding: 15px;
            margin-bottom: 10px;
            border-radius: 8px;
            border-left: 4px solid;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            cursor: move;
            transition: transform 0.2s, box-shadow 0.2s;
        }

        .task-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 4px 8px rgba(0,0,0,0.15);
        }

        .task-card.dragging {
            opacity: 0.5;
        }

        .task-card.priority-high {
            border-left-color: #ef4444;
        }

        .task-card.priority-medium {
            border-left-color: #f59e0b;
        }

        .task-card.priority-low {
            border-left-color: #10b981;
        }

        .task-header {
            display: flex;
            justify-content: space-between;
            align-items: start;
            margin-bottom: 8px;
        }

        .task-title {
            font-weight: 600;
            font-size: 1rem;
            color: #1f2937;
        }

        .task-priority {
            padding: 2px 8px;
            border-radius: 4px;
            font-size: 0.75rem;
            font-weight: 600;
            text-transform: uppercase;
        }

        .priority-high {
            background: #fee2e2;
            color: #991b1b;
        }

        .priority-medium {
            background: #fef3c7;
            color: #92400e;
        }

        .priority-low {
            background: #d1fae5;
            color: #065f46;
        }

        .task-description {
            color: #6b7280;
            font-size: 0.9rem;
            margin-bottom: 10px;
        }

        .task-footer {
            display: flex;
            justify-content: space-between;
            align-items: center;
            font-size: 0.8rem;
            color: #9ca3af;
        }

        .delete-btn {
            background: #ef4444;
            padding: 4px 12px;
            font-size: 0.8rem;
        }

        .delete-btn:hover {
            background: #dc2626;
        }

        .status-indicator {
            width: 12px;
            height: 12px;
            border-radius: 50%;
            display: inline-block;
            margin-right: 5px;
        }

        .status-todo { background: #3b82f6; }
        .status-in-progress { background: #f59e0b; }
        .status-done { background: #10b981; }

        @media (max-width: 768px) {
            .board {
                grid-template-columns: 1fr;
            }

            .add-task-form {
                grid-template-columns: 1fr;
            }
        }

        .drop-zone {
            border: 2px dashed #cbd5e0;
            min-height: 100px;
            border-radius: 8px;
            display: flex;
            align-items: center;
            justify-content: center;
            color: #a0aec0;
        }

        .drop-zone.drag_over {
            background: #edf2f7;
            border-color: #667eea;
        }
    </style>
</head>
<body>
    <div class="container">
        <header>
            <h1>🎯 实时任务看板</h1>
            <p>基于 C# 14 & .NET 10 的单文件 Web 应用演示</p>
        </header>

        <div class="add-task-section">
            <form id="addTaskForm" class="add-task-form">
                <div class="form-group">
                    <label for="taskTitle">任务标题</label>
                    <input type="text" id="taskTitle" placeholder="输入任务标题" required>
                </div>
                <div class="form-group">
                    <label for="taskDescription">任务描述</label>
                    <textarea id="taskDescription" placeholder="输入任务描述"></textarea>
                </div>
                <div class="form-group">
                    <label for="taskPriority">优先级</label>
                    <select id="taskPriority">
                        <option value="low">低</option>
                        <option value="medium" selected>中</option>
                        <option value="high">高</option>
                    </select>
                </div>
                <button type="submit">➕ 添加任务</button>
            </form>
        </div>

        <div class="board">
            <div class="column todo">
                <div class="column-header">
                    <h2>
                        <span class="status-indicator status-todo"></span>
                        待办事项
                        <span class="count" id="todoCount">0</span>
                    </h2>
                </div>
                <div class="task-list" id="todoList" data-status="todo"></div>
            </div>

            <div class="column in-progress">
                <div class="column-header">
                    <h2>
                        <span class="status-indicator status-in-progress"></span>
                        进行中
                        <span class="count" id="inProgressCount">0</span>
                    </h2>
                </div>
                <div class="task-list" id="inProgressList" data-status="in-progress"></div>
            </div>

            <div class="column done">
                <div class="column-header">
                    <h2>
                        <span class="status-indicator status-done"></span>
                        已完成
                        <span class="count" id="doneCount">0</span>
                    </h2>
                </div>
                <div class="task-list" id="doneList" data-status="done"></div>
            </div>
        </div>
    </div>

    <script>
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/taskHub")
            .withAutomaticReconnect()
            .build();

        let draggedElement = null;

        connection.start().then(() => {
            console.log('✅ SignalR 连接成功');
            loadTasks();
        }).catch(err => console.error('❌ SignalR 连接失败:', err));

        connection.on("TaskAdded", task => {
            addTaskToBoard(task);
            updateCounts();
        });

        connection.on("TaskMoved", (id, newStatus) => {
            const taskCard = document.querySelector(`[data-task-id="${id}"]`);
            if (taskCard) {
                const targetList = document.querySelector(`[data-status="${newStatus}"]`);
                targetList.appendChild(taskCard);
                updateCounts();
            }
        });

        connection.on("TaskDeleted", id => {
            const taskCard = document.querySelector(`[data-task-id="${id}"]`);
            if (taskCard) {
                taskCard.remove();
                updateCounts();
            }
        });

        async function loadTasks() {
            const response = await fetch('/api/tasks');
            const tasks = await response.json();
            tasks.forEach(task => addTaskToBoard(task, false));
            updateCounts();
        }

        document.getElementById('addTaskForm').addEventListener('submit', async (e) => {
            e.preventDefault();

            const task = {
                Id: crypto.randomUUID(),
                Title: document.getElementById('taskTitle').value,
                Description: document.getElementById('taskDescription').value,
                Status: 'todo',
                Priority: document.getElementById('taskPriority').value,
                CreatedAt: new Date().toISOString()
            };

            try {
                await connection.invoke("NotifyTaskAdded", task);
                e.target.reset();
            } catch (error) {
                console.error('添加任务失败:', error);
                alert('添加任务失败: ' + error.message);
            }
        });

        function addTaskToBoard(task, animate = true) {
            const taskCard = document.createElement('div');
            taskCard.className = `task-card priority-${task.Priority}`;
            taskCard.setAttribute('data-task-id', task.Id);
            taskCard.setAttribute('draggable', 'true');

            const priorityLabel = {
                'high': '高',
                'medium': '中',
                'low': '低'
            };

            taskCard.innerHTML = `
                <div class="task-header">
                    <div class="task-title">${task.Title}</div>
                    <span class="task-priority priority-${task.Priority}">${priorityLabel[task.Priority]}</span>
                </div>
                <div class="task-description">${task.Description || '无描述'}</div>
                <div class="task-footer">
                    <span>${new Date(task.CreatedAt).toLocaleDateString('zh-CN')}</span>
                    <button class="delete-btn" onclick="deleteTask('${task.Id}')">删除</button>
                </div>
            `;

            taskCard.addEventListener('dragstart', (e) => {
                draggedElement = taskCard;
                taskCard.classList.add('dragging');
            });

            taskCard.addEventListener('dragend', () => {
                taskCard.classList.remove('dragging');
            });

            const targetList = document.getElementById(`${task.Status}List`);
            targetList.appendChild(taskCard);

            if (animate) {
                taskCard.style.animation = 'slideIn 0.3s ease-out';
            }
        }

        async function deleteTask(id) {
            if (confirm('确定要删除这个任务吗？')) {
                await connection.invoke("NotifyTaskDeleted", id);
            }
        }

        document.querySelectorAll('.task-list').forEach(list => {
            list.addEventListener('dragover', (e) => {
                e.preventDefault();
                list.classList.add('drag-over');
            });

            list.addEventListener('dragleave', () => {
                list.classList.remove('drag-over');
            });

            list.addEventListener('drop', async (e) => {
                e.preventDefault();
                list.classList.remove('drag-over');

                if (draggedElement) {
                    const newStatus = list.getAttribute('data-status');
                    const taskId = draggedElement.getAttribute('data-task-id');
                    
                    list.appendChild(draggedElement);
                    
                    await connection.invoke("NotifyTaskMoved", taskId, newStatus);
                }
            });
        });

        function updateCounts() {
            document.getElementById('todoCount').textContent = 
                document.getElementById('todoList').children.length;
            document.getElementById('inProgressCount').textContent = 
                document.getElementById('inProgressList').children.length;
            document.getElementById('doneCount').textContent = 
                document.getElementById('doneList').children.length;
        }
    </script>
</body>
</html>
""";
}