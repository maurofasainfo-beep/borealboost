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
    public void App_references_only_allowed_dependencies_in_phase_4()
    {
        var project = LoadProject("src", "BorealBoost.App", "BorealBoost.App.csproj");
        var refs = ProjectReferences(project).ToArray();

        Assert.Contains(@"..\BorealBoost.Analysis\BorealBoost.Analysis.csproj", refs);
        Assert.Contains(@"..\BorealBoost.Core\BorealBoost.Core.csproj", refs);
        Assert.Contains(@"..\BorealBoost.Infrastructure\BorealBoost.Infrastructure.csproj", refs);
        Assert.Contains(@"..\BorealBoost.Optimization\BorealBoost.Optimization.csproj", refs);
        Assert.Contains(@"..\BorealBoost.Restore\BorealBoost.Restore.csproj", refs);
        Assert.Contains(@"..\BorealBoost.System\BorealBoost.System.csproj", refs);
        Assert.DoesNotContain(refs, item => item.Contains("Drivers", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, item => item.Contains("Benchmark", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, item => item.Contains("Reporting", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Agent_references_only_approved_phase_4_execution_dependencies()
    {
        var project = LoadProject("src", "BorealBoost.Agent", "BorealBoost.Agent.csproj");
        var refs = ProjectReferences(project).ToArray();

        Assert.Contains(@"..\BorealBoost.Core\BorealBoost.Core.csproj", refs);
        Assert.Contains(@"..\BorealBoost.Infrastructure\BorealBoost.Infrastructure.csproj", refs);
        Assert.Contains(@"..\BorealBoost.Optimization\BorealBoost.Optimization.csproj", refs);
        Assert.Contains(@"..\BorealBoost.System\BorealBoost.System.csproj", refs);
        Assert.DoesNotContain(refs, item => item.Contains("Drivers", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, item => item.Contains("Benchmark", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, item => item.Contains("Reporting", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Future_module_projects_reference_core_only()
    {
        var modules = new[]
        {
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

    [Fact]
    public void Optimization_and_restore_keep_core_as_only_project_reference()
    {
        foreach (var module in new[] { "BorealBoost.Optimization", "BorealBoost.Restore" })
        {
            var project = LoadProject("src", module, $"{module}.csproj");
            var refs = ProjectReferences(project).ToArray();

            Assert.Single(refs);
            Assert.Equal(@"..\BorealBoost.Core\BorealBoost.Core.csproj", refs[0]);
        }
    }

    [Fact]
    public void Analysis_project_remains_pure_and_depends_on_core_only()
    {
        var project = LoadProject("src", "BorealBoost.Analysis", "BorealBoost.Analysis.csproj");
        var refs = ProjectReferences(project).ToArray();

        Assert.Single(refs);
        Assert.Equal(@"..\BorealBoost.Core\BorealBoost.Core.csproj", refs[0]);
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
