// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cratis.Chronicle.XUnit.Integration.Packaging.for_the_embedded_kernel_closure;

/// <summary>
/// The package ships the kernel assemblies inside itself, so it has to ship their package dependencies too.
/// </summary>
/// <remarks>
/// Nothing about a project reference marked <c language="csharp">PrivateAssets=all</c> makes that happen: the assembly is embedded
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
    IReadOnlyCollection<string> _uncoveredV3;
    IReadOnlyCollection<string> _missingFromV3;

    void Because()
    {
        const string v2Project = "Source/Clients/XUnit.Integration/XUnit.Integration.csproj";
        const string v3Project = "Source/Clients/XUnit.Integration.v3/XUnit.Integration.v3.csproj";
        _uncovered = UncoveredDependencies(v2Project);
        _uncoveredV3 = UncoveredDependencies(v3Project);
        var (v2Packages, _) = ProjectFileDependencies.Read(ProjectFile(v2Project));
        var (v3Packages, _) = ProjectFileDependencies.Read(ProjectFile(v3Project));
        _missingFromV3 = v2Packages
            .Except(["xunit", "Cratis.Specifications.XUnit"], StringComparer.OrdinalIgnoreCase)
            .Except(v3Packages, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    [Fact]
    void should_declare_every_package_the_embedded_kernel_needs() =>
        string.Join(", ", _uncovered).ShouldEqual(string.Empty);

    [Fact]
    void should_declare_every_package_the_v3_embedded_kernel_needs() =>
        string.Join(", ", _uncoveredV3).ShouldEqual(string.Empty);

    [Fact]
    void should_preserve_the_v2_non_xunit_dependency_closure_in_v3() =>
        string.Join(", ", _missingFromV3).ShouldEqual(string.Empty);

    static IReadOnlyCollection<string> UncoveredDependencies(string projectPath)
    {
        var package = ProjectFile(projectPath);
        var (declared, references) = ProjectFileDependencies.Read(package);

        var embedded = ClosureOf(references.Where(_ => _.IsEmbedded).Select(_ => Resolve(package, _.Include)));
        var flowing = ClosureOf(references.Where(_ => !_.IsEmbedded).Select(_ => Resolve(package, _.Include)));

        // A dependency of a project reference that is not embedded arrives at the consumer on its own, through
        // that project's own package. Only the embedded ones have nothing carrying them.
        var covered = declared.Concat(flowing).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // One more way a dependency travels: packed into lib/ rather than declared, for the one that cannot be
        // declared at all. See PrivatePackageRuntimeAssemblyRegex in the packaging project.
        var packed = PackedRuntimeAssemblies(package);

        return
        [
            .. embedded
                .Where(_ => !covered.Contains(_) && !packed.IsMatch($"{_}.dll"))
                .Order(StringComparer.OrdinalIgnoreCase)
        ];
    }

    static Regex PackedRuntimeAssemblies(FileInfo project)
    {
        var pattern = XDocument.Load(project.FullName)
            .Descendants()
            .FirstOrDefault(_ => _.Name.LocalName.Equals("PrivatePackageRuntimeAssemblyRegex", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        // Nothing packed means nothing matches, rather than everything.
        return new(string.IsNullOrWhiteSpace(pattern) ? "(?!)" : pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }

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

            var (declared, references) = ProjectFileDependencies.Read(project);
            packages.UnionWith(declared);
            foreach (var reference in references)
            {
                pending.Push(Resolve(project, reference.Include));
            }
        }

        return packages;
    }
}
