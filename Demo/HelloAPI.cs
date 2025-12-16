#!/usr/bin/env dotnet
#:sdk Microsoft.NET.Sdk.Web
#:property PublishAot=false

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:8500");

var app = builder.Build();
app.MapGet("/", (string? query) => $"你好,{query ?? ""}");

app.Run();