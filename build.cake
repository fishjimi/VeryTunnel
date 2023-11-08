var target = Argument("target", "Package-Nuget");

// Directories
var nuget = Directory("tools");
var output = Directory("build");
var outputNuGet = output + Directory("nuget");

var version = "0.0.0-test.0";
var configuration = "Release";
var source = "";

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
            OutputDirectory = outputNuGet
        };
        DotNetPack(csProjectFile.FullPath, setting);
    }

});

RunTarget(target);