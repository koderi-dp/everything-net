using Everything.Net.Search.App;

var exitCode = await SearchApplication.RunAsync(args);
Environment.ExitCode = exitCode;
