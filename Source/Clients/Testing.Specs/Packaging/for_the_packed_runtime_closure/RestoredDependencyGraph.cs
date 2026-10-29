// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cratis.Chronicle.Testing.Packaging.for_the_packed_runtime_closure;

/// <summary>
/// Reads what restore resolved for a packaging project, to see what its consumers will and will not receive.
/// </summary>
/// <param name="libraries">The resolved libraries of one target, keyed by name.</param>
/// <param name="received">The names of the libraries a consumer of the package receives as dependencies.</param>
sealed class RestoredDependencyGraph(IReadOnlyDictionary<string, JsonElement> libraries, IReadOnlySet<string> received)
{
    /// <summary>
    /// Reads the graph restore produced for a project.
    /// </summary>
    /// <param name="project">The project file.</param>
    /// <returns>The graph.</returns>
    internal static RestoredDependencyGraph Read(FileInfo project)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(project.DirectoryName!, "obj", "project.assets.json")));
        var root = document.RootElement.Clone();

        // The runtime-specific targets repeat the plain one with native assets; the plain one is what is packed.
        var target = root.GetProperty("targets").EnumerateObject().First(_ => !_.Name.Contains('/'));
        var libraries = target.Value.EnumerateObject()
            .ToDictionary(_ => NameOf(_.Name), _ => _.Value, StringComparer.OrdinalIgnoreCase);

        var roots = DeclaredPackages(root, target.Name).Concat(FlowingProjects(root, project));
        return new(libraries, ClosureOf(roots, libraries));
    }

    /// <summary>
    /// Finds the dependencies of packed packages that a consumer would have no way of getting.
    /// </summary>
    /// <param name="packed">Matches the file names of the runtime assemblies packed into the package.</param>
    /// <returns>Each uncovered dependency, as <c language="text">package -> dependency</c>.</returns>
    internal IReadOnlyCollection<string> UncoveredDependenciesOfPackedPackages(Regex packed)
    {
        var packedPackages = libraries
            .Where(_ => IsPackage(_.Value) && RuntimeAssemblies(_.Value).Any(packed.IsMatch))
            .Select(_ => _.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return
        [
            .. packedPackages
                .SelectMany(package => DependenciesOf(libraries[package]).Select(dependency => (package, dependency)))
                .Where(_ => !packedPackages.Contains(_.dependency) && !received.Contains(_.dependency) && HasRuntimeAssemblies(_.dependency))
                .Select(_ => $"{_.package} -> {_.dependency}")
                .Order(StringComparer.OrdinalIgnoreCase)
        ];
    }

    /// <summary>
    /// Reads the packed runtime assembly pattern a project declares.
    /// </summary>
    /// <param name="project">The project file.</param>
    /// <returns>The pattern; one that matches nothing when the project packs nothing.</returns>
    internal static Regex PackedRuntimeAssemblies(FileInfo project)
    {
        var pattern = XDocument.Load(project.FullName)
            .Descendants()
            .FirstOrDefault(_ => _.Name.LocalName.Equals("PrivatePackageRuntimeAssemblyRegex", StringComparison.OrdinalIgnoreCase))
            ?.Value;
        return new(string.IsNullOrWhiteSpace(pattern) ? "(?!)" : pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }

    bool HasRuntimeAssemblies(string name) => libraries.TryGetValue(name, out var library) && RuntimeAssemblies(library).Any();

    static string NameOf(string key) => key.Split('/')[0];

    static bool IsPackage(JsonElement library) =>
        library.TryGetProperty("type", out var type) && type.GetString() == "package";

    /// <summary>
    /// The runtime assemblies of a library. <c language="text">_._</c> marks a framework that needs no assembly from
    /// the package, which is nothing to carry.
    /// </summary>
    /// <param name="library">The resolved library.</param>
    /// <returns>The file names of its runtime assemblies.</returns>
    static IEnumerable<string> RuntimeAssemblies(JsonElement library) =>
        library.TryGetProperty("runtime", out var runtime)
            ? runtime.EnumerateObject().Select(_ => Path.GetFileName(_.Name)).Where(_ => _ != "_._")
            : [];

    static IEnumerable<string> DependenciesOf(JsonElement library) =>
        library.TryGetProperty("dependencies", out var dependencies)
            ? dependencies.EnumerateObject().Select(_ => _.Name)
            : [];

    /// <summary>
    /// The packages the project declares. A package reference marked PrivateAssets=all is suppressed from the
    /// nuspec; everything else is declared.
    /// </summary>
    /// <param name="root">The restore output.</param>
    /// <param name="framework">The target framework.</param>
    /// <returns>The names of the declared packages.</returns>
    static IEnumerable<string> DeclaredPackages(JsonElement root, string framework) =>
        root.GetProperty("project").GetProperty("frameworks").GetProperty(framework).TryGetProperty("dependencies", out var dependencies)
            ? dependencies.EnumerateObject()
                .Where(_ => !_.Value.TryGetProperty("suppressParent", out var suppress) || suppress.GetString() != "All")
                .Select(_ => _.Name)
            : [];

    static IEnumerable<string> FlowingProjects(JsonElement root, FileInfo project)
    {
        // A project reference that is not embedded ships as its own package and brings its dependencies with it.
        var flowing = root.GetProperty("project").GetProperty("restore").GetProperty("frameworks").EnumerateObject().First().Value
            .GetProperty("projectReferences").EnumerateObject()
            .Where(_ => !_.Value.TryGetProperty("privateAssets", out var assets) || assets.GetString() != "all")
            .Select(_ => Path.GetFullPath(_.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return root.GetProperty("libraries").EnumerateObject()
            .Where(_ => _.Value.TryGetProperty("path", out var path) &&
                        flowing.Contains(Path.GetFullPath(Path.Combine(project.DirectoryName!, path.GetString()!))))
            .Select(_ => NameOf(_.Name));
    }

    static HashSet<string> ClosureOf(IEnumerable<string> roots, IReadOnlyDictionary<string, JsonElement> libraries)
    {
        var closure = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<string>(roots);
        while (pending.Count > 0)
        {
            var name = pending.Pop();
            if (!closure.Add(name) || !libraries.TryGetValue(name, out var library))
            {
                continue;
            }

            foreach (var dependency in DependenciesOf(library))
            {
                pending.Push(dependency);
            }
        }

        return closure;
    }
}
