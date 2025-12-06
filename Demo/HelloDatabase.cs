#!/usr/bin/env dotnet
#:package Npgsql@10.0.0

using Npgsql;
using System.Text.RegularExpressions;

// 获取连接字符串
string? connectionString;

if (args.Length > 0)
{
    // 从命令行参数获取
    connectionString = args[0];
}
else
{
    // 从控制台输入获取
    Console.Write("请输入 PostgreSQL 连接字符串 (例如: postgresql://username:password@ip:port/postgres): ");
    connectionString = Console.ReadLine();
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("❌ 连接字符串不能为空");
    return 1;
}

// 转换 PostgreSQL URI 格式到 Npgsql 连接字符串
string npgsqlConnectionString = ConvertToNpgsqlConnectionString(connectionString);

// 测试数据库连接
try
{
    await using var connection = new NpgsqlConnection(npgsqlConnectionString);

    Console.WriteLine("正在连接数据库...");
    await connection.OpenAsync();

    // 执行简单查询验证连接
    await using var command = new NpgsqlCommand("SELECT 1", connection);
    var result = await command.ExecuteScalarAsync();

    if (result?.ToString() == "1")
    {
        Console.WriteLine("✅ 数据库连接成功!");
        return 0;
    }
    else
    {
        Console.WriteLine("⚠️ 连接已建立,但查询结果异常");
        return 1;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ 数据库连接失败: {ex.Message}");
    return 1;
}

// 转换 PostgreSQL URI 格式到 Npgsql 连接字符串格式
static string ConvertToNpgsqlConnectionString(string connectionString)
{
    // 如果已经是标准格式,直接返回
    if (!connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) &&
        !connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    // 解析 PostgreSQL URI: postgresql://username:password@host:port/database
    var regex = new Regex(@"postgres(?:ql)?://(?<user>[^:]+):(?<password>[^@]+)@(?<host>[^:]+):(?<port>\d+)/(?<database>.+)");
    var match = regex.Match(connectionString);

    if (!match.Success)
    {
        return connectionString; // 无法解析,返回原字符串
    }

    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = match.Groups["host"].Value,
        Port = int.Parse(match.Groups["port"].Value),
        Username = match.Groups["user"].Value,
        Password = match.Groups["password"].Value,
        Database = match.Groups["database"].Value,
        SslMode = SslMode.Prefer
    };

    return builder.ToString();
}