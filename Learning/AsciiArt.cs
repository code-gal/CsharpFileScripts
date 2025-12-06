#!/usr/local/share/dotnet/dotnet run

#:package Colorful.Console@1.2.15
#:package System.CommandLine@2.0.0

using System.CommandLine;
using System.CommandLine.Parsing;

// 定义命令行选项: --delay，控制每行输出之间的延迟（毫秒），默认100ms
Option<int> delayOption = new("--delay")
{
    Description = "Delay between lines, specified as milliseconds.",
    DefaultValueFactory = parseResult => 100
};

// 定义命令行参数: Messages，表示要渲染的文本内容（可以多个）
Argument<string[]> messagesArgument = new("Messages")
{
    Description = "Text to render."
};

// 创建根命令，描述程序用途
RootCommand rootCommand = new("Ascii Art file-based program sample");

// 将选项和参数添加到根命令
rootCommand.Options.Add(delayOption);
rootCommand.Arguments.Add(messagesArgument);

// 解析命令行参数
ParseResult result = rootCommand.Parse(args);
foreach (ParseError parseError in result.Errors)
{
    // 如果解析出错，输出错误信息
    Console.Error.WriteLine(parseError.Message);
}
if (result.Errors.Count > 0)
{
    // 有错误则退出程序，返回1
    return 1;
}

// 处理解析结果，获取参数
var parsedArgs = await ProcessParseResults(result);

// 根据参数输出 ASCII 艺术字
await WriteAsciiArt(parsedArgs);
return 0;

// 处理参数解析结果，返回消息和延迟
async Task<AsciiMessageOptions> ProcessParseResults(ParseResult result)
{
    int delay = result.GetValue(delayOption); // 获取延迟参数
    List<string> messages = [.. result.GetValue(messagesArgument) ?? Array.Empty<string>()]; // 获取消息参数，..C# 12 引入的新语法，叫做集合展开表达式

    // 如果没有传递消息参数，则从标准输入读取文本
    if (messages.Count == 0)
    {
        while (Console.ReadLine() is string line && line.Length > 0)
        {
            // <WriteAscii>
            Colorful.Console.WriteAscii(line); // 输出 ASCII 艺术字
            // </WriteAscii>
            await Task.Delay(delay); // 延迟
        }
    }
    // 返回消息和延迟
    return new([.. messages], delay);
}

// 根据参数输出 ASCII 艺术字， Colorful.Console不支持中文
async Task WriteAsciiArt(AsciiMessageOptions options)
{
    foreach (string message in options.Messages)
    {
        Colorful.Console.WriteAscii(message); // 输出 ASCII 艺术字
        await Task.Delay(options.Delay);      // 延迟
    }
}

// 用于存储消息和延迟的记录类型
public record AsciiMessageOptions(string[] Messages, int Delay);