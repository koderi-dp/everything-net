#:sdk Cake.Sdk@6.0.0
// Run with: dotnet run .\build.cs -- --target Pack --configuration Release
// Optional env vars:
//   PACKAGE_VERSION=0.1.1
//   NUGET_API_KEY=...
// Optional CLI args:
//   --package-version 0.1.1
//   --nuget-api-key ...

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");
var packageVersion = Argument("package-version", EnvironmentVariable("PACKAGE_VERSION") ?? string.Empty);
var nugetApiKey = Argument("nuget-api-key", EnvironmentVariable("NUGET_API_KEY") ?? string.Empty);
var nugetSource = "https://api.nuget.org/v3/index.json";
var packageProject = "./src/Everything.Net/Everything.Net.csproj";
var packageOutput = "./artifacts/nuget";

Task("Clean")
    .Does(context =>
{
    context.CleanDirectories("./src/**/bin");
    context.CleanDirectories("./src/**/obj");
    context.CleanDirectories("./examples/**/bin");
    context.CleanDirectories("./examples/**/obj");
    context.CleanDirectory(packageOutput);
});

Task("Build")
    .Does(context =>
{
    DotNetBuild("./src/Everything.Net.slnx", new DotNetBuildSettings
    {
        Configuration = configuration,
        NoLogo = true,
        Verbosity = DotNetVerbosity.Minimal,
    });
});

Task("Pack")
    .IsDependentOn("Build")
    .Does(context =>
{
    context.CleanDirectory(packageOutput);
    context.EnsureDirectoryExists(packageOutput);

    var settings = new DotNetPackSettings
    {
        Configuration = configuration,
        NoLogo = true,
        OutputDirectory = packageOutput,
        Verbosity = DotNetVerbosity.Minimal,
    };

    if (!string.IsNullOrWhiteSpace(packageVersion))
    {
        settings.MSBuildSettings = new DotNetMSBuildSettings()
            .WithProperty("Version", packageVersion);
    }

    DotNetPack(packageProject, settings);
});

Task("Publish")
    .IsDependentOn("Pack")
    .Does(context =>
{
    if (string.IsNullOrWhiteSpace(nugetApiKey))
    {
        throw new Exception("NuGet API key is required. Set NUGET_API_KEY.");
    }

    var packageFiles = context.GetFiles($"{packageOutput}/*.nupkg")
        .Where(path => !path.FullPath.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    if (packageFiles.Length == 0)
    {
        throw new Exception("No NuGet package was produced to publish.");
    }

    context.Log.Information($"Publishing {packageFiles.Length} NuGet packages to {nugetSource}...");

    foreach (var packageFile in packageFiles)
    {
        DotNetNuGetPush(packageFile.FullPath, new DotNetNuGetPushSettings
        {
            ApiKey = nugetApiKey,
            Source = nugetSource,
            SkipDuplicate = true
        });
    }
});

Task("Default")
    .IsDependentOn("Build");

RunTarget(target);
