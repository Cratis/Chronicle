// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Chronicle.XUnit.Integration.Packaging.for_the_embedded_kernel_closure;

/// <summary>
/// The package ships the kernel assemblies inside itself, so it has to ship their package dependencies too.
/// </summary>
/// <remarks>
/// Nothing about a project reference marked <c>PrivateAssets=all</c> makes that happen: the assembly is embedded
/// and its own dependencies are suppressed, so a consumer restores a package that cannot boot. What that costs is
/// out of proportion to the omission, because it fails at fixture initialization - every spec in the project
/// fails at once, identically, before a single spec body runs, and the exception names one assembly. It reads as
/// "the harness is broken", and the distance between where it presents and where it lives is most of the cost.
/// <para>
/// A list of dependencies fixes one instance and leaves the class open: the closure grows whenever the kernel
/// takes a new dependency, including on a routine patch bump, and every consumer rediscovers it the same way. So
/// the list is not what is checked here - the invariant is, by walking the embedded projects and asking whether
/// this package covers what they need. The next kernel dependency fails this spec instead.
/// </para>
/// </remarks>
public class when_checking_that_every_dependency_travels_with_it : Specification
{
    IReadOnlyCollection<string> _uncovered;

    void Because()
    {
        var package = ProjectFile("Source/Clients/XUnit.Integration/XUnit.Integration.csproj");
        var (declared, references) = Read(package);

        var embedded = ClosureOf(references.Where(_ => _.IsEmbedded).Select(_ => Resolve(package, _.Include)));
        var flowing = ClosureOf(references.Where(_ => !_.IsEmbedded).Select(_ => Resolve(package, _.Include)));

        // A dependency of a project reference that is not embedded arrives at the consumer on its own, through
        // that project's own package. Only the embedded ones have nothing carrying them.
        var covered = declared.Concat(flowing).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _uncovered = [.. embedded.Where(_ => !covered.Contains(_)).Order(StringComparer.OrdinalIgnoreCase)];
    }

    [Fact]
    void should_declare_every_package_the_embedded_kernel_needs() =>
        string.Join(", ", _uncovered).ShouldEqual(string.Empty);

    static FileInfo ProjectFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath)))
        {
            directory = directory.Parent;
        }

        return new FileInfo(Path.Combine(directory!.FullName, relativePath));
    }

    static FileInfo Resolve(FileInfo from, string include) =>
        new(Path.GetFullPath(Path.Combine(from.DirectoryName!, include.Replace('\\', '/'))));

    static (HashSet<string> Packages, IReadOnlyCollection<(string Include, bool IsEmbedded)> References) Read(FileInfo project)
    {
        var content = File.ReadAllText(project.FullName);

        var packages = Regex.Matches(content, "PackageReference\\s+Include=\"([^\"]+)\"")
            .Select(_ => _.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var references = Regex.Matches(content, "<ProjectReference\\s+Include=\"([^\"]+)\"\\s*(/?)>((?:(?!</ProjectReference>|<ProjectReference).)*)", RegexOptions.Singleline)
            .Select(_ => (
                Include: _.Groups[1].Value,
                IsEmbedded: _.Groups[2].Value.Length == 0 &&
                            _.Groups[3].Value.Contains("<PrivateAssets>all</PrivateAssets>", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        return (packages, references);
    }

    static HashSet<string> ClosureOf(IEnumerable<FileInfo> roots)
    {
        var packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<FileInfo>(roots);

        while (pending.Count > 0)
        {
            var project = pending.Pop();
            if (!project.Exists || !visited.Add(project.FullName))
            {
                continue;
            }

            var (declared, references) = Read(project);
            packages.UnionWith(declared);
            foreach (var reference in references)
            {
                pending.Push(Resolve(project, reference.Include));
            }
        }

        return packages;
    }
}
