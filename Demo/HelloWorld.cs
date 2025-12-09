var message = args.FirstOrDefault() ?? "Hello, World!";
Console.WriteLine(message);

Console.WriteLine($"Current direcotory: {Environment.CurrentDirectory}");

Console.WriteLine($"Base directory: {AppContext.BaseDirectory}");

Console.WriteLine($"File path:{AppContext.GetData("EntryPointFilePath")}");