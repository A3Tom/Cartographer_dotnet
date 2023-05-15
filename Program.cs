using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

var baseDir = "C:\\TestFiles\\FrogService";
var csprojFiles = Directory.GetFiles(baseDir, "ALBS.*.*.csproj", SearchOption.AllDirectories);

var pkgInfos = csprojFiles.Select(Scan);
var groupedInfos = pkgInfos
    .SelectMany(x => x.PackageReferences)
    .GroupBy(g => g)
    .Select(g => new { Package = g.Key, Count = g.Count() });
var internalWiring = pkgInfos
    .Select(x => new { 
        ProjectName = x.ProjectName.Replace(".csproj", string.Empty),
        ServiceId = GetUngildedProjectName(x.ProjectName),
        FriendlyName = GlimpseFriendlyNameFromProjectName(x.ProjectName),
        InternalServices = x.PackageReferences
            .Where(p => p.ReferenceType == PackageReferenceType.Internal)
            .Select(p => new {
                PackageName = p.PackageName, 
                ServiceId = GetUngildedProjectName(p.PackageName),
                FriendlyName = GlimpseFriendlyNameFromProjectName(p.PackageName),
                Version = p.Version
            }).ToList()
    })
    .Where(x => x.InternalServices.Any())
    .ToList();

var hing = JsonConvert.SerializeObject(internalWiring);
File.WriteAllText(Path.Combine(baseDir, "internalServices.json"), hing);

foreach (var pkgInfo in pkgInfos)
{
    Console.WriteLine($"{pkgInfo.ProjectName} ({pkgInfo.TargetFramework}) (output type: {pkgInfo.ProjectType})");

    foreach (var pkgRef in pkgInfo.PackageReferences)
        Console.WriteLine($" - {pkgRef.ReferenceType} Package Reference : {pkgRef.PackageName} ({pkgRef.Version})");

    foreach (var pkgRef in pkgInfo.ProjectReferences)
        Console.WriteLine($" = Project Reference --> {pkgRef}");

    Console.WriteLine();
}

static string GetUngildedProjectName(string projectName)
    => Regex.Match(projectName, @"ALBS\.([^.]+)\.").Groups[1].Value;


static string GlimpseFriendlyNameFromProjectName(string projectName)
{
    var initial = Regex.Match(projectName, @"ALBS\.([^.]+)\.");

    return Regex.Replace(initial.Groups[1].Value, @"(?<=\p{Ll})(?=\p{Lu})", " ");
}

static ProjectPackageInfo Scan(string projectFilePath)
{
    var scannedProjectName = Path.GetFileName(projectFilePath);
    var packageReferences = new List<PackageReference>();
    var projectReferences = new List<string>();
    string targetFramework = string.Empty;
    var projectType = ProjectPackageType.Library;

    var xdocProps = new List<string>()
    {
        "TargetFramework",
        "PackageReference",
        "ProjectReference"
    };

    if (File.Exists(projectFilePath))
    {
        var projectFile = XDocument.Load(projectFilePath);

        if (projectFile.Root != null && projectFile.Root.Name.LocalName == "Project")
        {
            var pkgRefs = projectFile.Root.Descendants().Where(x => xdocProps.Contains(x.Name.LocalName));

            if (Regex.IsMatch(scannedProjectName, @"(?:\.[tT]est[\.s]*.)"))
                projectType = ProjectPackageType.Test;

            if (projectFile.Root.Attribute("Sdk")?.Value == "Microsoft.NET.Sdk.Web")
            {
                projectType = Regex.IsMatch(scannedProjectName, @"(?:\.[aA]pi\.)") ? 
                    ProjectPackageType.Api : 
                    ProjectPackageType.Web;
            }

            if (projectFile.Root.Descendants().FirstOrDefault(x => x.Name.LocalName == "OutputType")?.Value == "Exe")
                projectType = ProjectPackageType.Console;

            foreach (var pkgRef in pkgRefs)
            {
                if (pkgRef.Name.LocalName == "TargetFramework")
                {
                    targetFramework = pkgRef.Value;
                    continue;
                }

                if (pkgRef.Name.LocalName == "PackageReference")
                {
                    var packageName = pkgRef.Attribute("Include")?.Value;

                    if (!string.IsNullOrWhiteSpace(packageName))
                    {
                        var version = pkgRef.Attribute("Version")?.Value ?? string.Empty;
                        var referenceType = Regex.IsMatch(packageName, @"ALBS\.(?:.*)\.[cC]lient") ? 
                            PackageReferenceType.Internal : 
                            PackageReferenceType.External;
                        packageReferences.Add(new(packageName, version, referenceType));
                    }

                    continue;
                }

                if (pkgRef.Name.LocalName == "ProjectReference")
                {
                    var projectName = pkgRef.Attribute("Include")?.Value;

                    if (!string.IsNullOrWhiteSpace(projectName))
                        projectReferences.Add(Path.GetFileName(projectName));

                    continue;
                }

            }
        }
    }
    return new(scannedProjectName, targetFramework, packageReferences, projectReferences, projectType);
}

record ProjectPackageInfo(string ProjectName, string TargetFramework, List<PackageReference> PackageReferences, List<string> ProjectReferences, ProjectPackageType ProjectType);

record PackageReference(string PackageName, string Version, PackageReferenceType ReferenceType);


public enum ProjectPackageType
{
    Web,
    Api,
    Console,
    Library,
    Test
}

public enum PackageReferenceType
{
    Internal,
    External
}