#:sdk Cake.Sdk@6.0.0

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

Task("Clean")
    .Does(context =>
{
    context.CleanDirectories("./src/**/bin");
    context.CleanDirectories("./src/**/obj");
    context.CleanDirectories("./examples/**/bin");
    context.CleanDirectories("./examples/**/obj");
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

Task("Default")
    .IsDependentOn("Build");

RunTarget(target);
