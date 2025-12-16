# AI Prompt for .NET 10 File-Based Apps

如果你希望 AI 帮你编写基于文件的 C# 程序，请将以下内容复制并发送给 AI：

---

**Context:**
I need you to write a **File-based C# program**.
**IMPORTANT**: Assume I am using **.NET 10** and **C# 14**. Since your training data might only cover up to .NET 9, please strictly follow the "New Syntax & Rules" section below to understand how to write this specific type of application.

**New Syntax & Rules (The "File-based" Model):**

1.  **No Project File**: The entire application lives in a single `.cs` file. Do NOT generate a `.csproj` file.
2.  **Directives (The Magic Part)**:
    In .NET 10, we can define project metadata directly in the C# file using special comments starting with `#:` or `#!`.
    *   **Must be at the top**: These lines must appear before any code (including `using` statements).
    *   **`#:package <Name>@<Version>`**: Replaces `<PackageReference>`. Use this to add NuGet packages.
        *   *Example*: `#:package Newtonsoft.Json@13.0.3`
    *   **`#:sdk <SdkName>`**: Replaces the top-level SDK attribute in a csproj.
        *   *Example*: `#:sdk Microsoft.NET.Sdk.Web` (For Web APIs/Minimal APIs)
        *   *Default*: If omitted, it behaves like a Console app.
    *   **`#:property <Key>=<Value>`**: Replaces MSBuild properties inside `<PropertyGroup>`.
        *   *Example*: `#:property TargetFramework=net10.0`
        *   *Example*: `#:property LangVersion=preview`
    *   **`#:project <Path>`**: References another project file.
3.  **Unix Shebang**:
    *   Always include `#!/usr/bin/env dotnet` as the very first line to make it executable on Unix-like systems.
4.  **Code Structure**:
    *   Use **Top-level statements**. Do not wrap the main logic in a `class Program { static void Main... }`.
    *   You can define classes, records, and methods at the bottom of the file or interspersed (local functions).
5.  **Arguments**:
    *   Command-line arguments are accessible via the global `args` variable (string array).

**Example Template (Strictly follow this pattern):**

```csharp
#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk.Web
#:package System.CommandLine@2.0.0-beta4.22272.1
#:property TargetFramework=net10.0

using System.CommandLine;
using Microsoft.AspNetCore.Builder;

// --- Your Main Logic Here ---
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello from a single file!");

app.Run();

// --- Definitions (Classes/Records) Here ---
record MyData(string Name);
```

**Task:**
[在此处描述你想要生成的脚本功能，例如：写一个脚本，读取当前目录下的所有图片并调整大小...]
