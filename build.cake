var target = Argument("target", "Package-Nuget");

// Directories
var output = Directory("build");
var outputNuGet = output + Directory("nuget");

var version = "0.0.0-test.0";
var copyright = $"Copyright (c) {System.DateTime.Now.Year} Jimifish";
var configuration = "Release";

Task("Clean")
.Does(() =>
{
    // Clean artifact directories.
    CleanDirectories(new DirectoryPath[] {
        output,
    });
    CleanDirectories("./**/bin/" + configuration);
    CleanDirectories("./**/obj/" + configuration);
});

Task("Update-Version")
.Does(() =>
{
    var csProjectFiles = GetFiles("./VeryTunnel*/*.csproj");
    foreach (var csProjectFile in csProjectFiles)
    {
        XmlPoke(csProjectFile, "//PropertyGroup/Version", version);
        XmlPoke(csProjectFile, "//PropertyGroup/Copyright", copyright);
    }
});


Task("Package-Nuget")
.IsDependentOn("Update-Version")
.IsDependentOn("Clean")
.Does(() =>
{
    var csProjectFiles = GetFiles("./VeryTunnel*/*.csproj");
    foreach (var csProjectFile in csProjectFiles)
    {
        var setting = new DotNetPackSettings
        {
            Configuration = configuration,
            OutputDirectory = outputNuGet,
        };
        DotNetPack(csProjectFile.FullPath, setting);
    }
});

RunTarget(target);