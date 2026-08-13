namespace BorealBoost.Tests.System;

public sealed class FoundationSafetyTests
{
    [Theory]
    [InlineData("ExecuteCommand(")]
    [InlineData("ExecutePowerShell(")]
    [InlineData("ExecuteProcess(")]
    [InlineData("Registry.SetValue")]
    [InlineData("ServiceController")]
    [InlineData("powercfg")]
    [InlineData("pnputil")]
    [InlineData("dism")]
    [InlineData("sfc")]
    public void Source_does_not_contain_destructive_or_arbitrary_execution_entrypoints(string forbiddenText)
    {
        var root = FindRepositoryRoot();
        var sourceFiles = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

        foreach (var sourceFile in sourceFiles)
        {
            var content = File.ReadAllText(sourceFile);
            Assert.DoesNotContain(forbiddenText, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Process_start_is_limited_to_known_agent_bootstrap()
    {
        var root = FindRepositoryRoot();
        var sourceFiles = Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories);
        var occurrences = sourceFiles
            .Where(path => File.ReadAllText(path).Contains("Process.Start", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        Assert.Equal(["src\\BorealBoost.App\\Agent\\AgentBootstrapService.cs"], occurrences);
    }

    [Fact]
    public void App_manifest_uses_as_invoker_for_foundation_dashboard()
    {
        var root = FindRepositoryRoot();
        var manifest = File.ReadAllText(Path.Combine(root, "src", "BorealBoost.App", "app.manifest"));

        Assert.Contains("requestedExecutionLevel level=\"asInvoker\"", manifest, StringComparison.Ordinal);
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
