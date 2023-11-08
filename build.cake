var target = Argument("target", "Package-Nuget");

// Directories
var output = Directory("build");
var outputNuGet = output + Directory("nuget");

var version = "1.0.0";
var versionPostfix = "";
var revision = DateTime.Now.ToString("HHmm");
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


Task("Package-Nuget")
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
            MSBuildSettings = new DotNetMSBuildSettings()
            .SetVersion($"{version}{versionPostfix}")
            .SetAssemblyVersion($"{version}.{revision}")
            .SetFileVersion($"{version}.{revision}")

        };
        DotNetPack(csProjectFile.FullPath, setting);
    }
});

RunTarget(target);