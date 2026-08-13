using System.Xml.Linq;

namespace BorealBoost.Tests.Integration;

public sealed class ArchitectureDependencyTests
{
    [Fact]
    public void Core_project_has_no_project_references()
    {
        var project = LoadProject("src", "BorealBoost.Core", "BorealBoost.Core.csproj");

        Assert.Empty(ProjectReferences(project));
    }

    [Fact]
    public void App_references_only_foundation_dependencies_in_phase_1()
    {
        var project = LoadProject("src", "BorealBoost.App", "BorealBoost.App.csproj");
        var refs = ProjectReferences(project).ToArray();

        Assert.Contains(@"..\BorealBoost.Core\BorealBoost.Core.csproj", refs);
        Assert.Contains(@"..\BorealBoost.Infrastructure\BorealBoost.Infrastructure.csproj", refs);
        Assert.Contains(@"..\BorealBoost.System\BorealBoost.System.csproj", refs);
        Assert.DoesNotContain(refs, item => item.Contains("Optimization", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, item => item.Contains("Drivers", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, item => item.Contains("Restore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Future_module_projects_reference_core_only()
    {
        var modules = new[]
        {
            "BorealBoost.Analysis",
            "BorealBoost.Optimization",
            "BorealBoost.Restore",
            "BorealBoost.Benchmark",
            "BorealBoost.Drivers",
            "BorealBoost.Reporting"
        };

        foreach (var module in modules)
        {
            var project = LoadProject("src", module, $"{module}.csproj");
            var refs = ProjectReferences(project).ToArray();

            Assert.Single(refs);
            Assert.Equal(@"..\BorealBoost.Core\BorealBoost.Core.csproj", refs[0]);
        }
    }

    private static XDocument LoadProject(params string[] pathParts)
    {
        return XDocument.Load(Path.Combine(FindRepositoryRoot(), Path.Combine(pathParts)));
    }

    private static IEnumerable<string> ProjectReferences(XDocument project)
    {
        return project
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => value is not null)
            .Select(value => value!);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BorealBoost.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
