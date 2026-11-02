// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Security.Cryptography;

namespace Cratis.Chronicle.MongoDB.Integration.Setup.for_ReleasedAdmissionCompatibility;

/// <summary>
/// Collectible load context that resolves every assembly strictly from one extracted image closure.
/// </summary>
/// <remarks>
/// The closure is the union of explicitly supplied roots (for example the flattened <c>/app</c> publish and the
/// selected shared framework directories of one image). Only the finite <see cref="SharedBcl"/> list crosses into the
/// host context; everything else, including Microsoft.Extensions, Orleans, Mongo and Cratis assemblies, must resolve
/// from the closure. Anything unresolved, conflicting or ambiguous throws; there is never a host fallback.
/// </remarks>
sealed class AdmissionLoadContext : AssemblyLoadContext
{
    /// <summary>
    /// The finite, explicit set of CLR/BCL assemblies shared with the host. No Microsoft.Extensions, ASP.NET,
    /// Orleans, Mongo or Cratis assembly may ever be added here.
    /// </summary>
    public static readonly IReadOnlySet<string> SharedBcl = new HashSet<string>(StringComparer.Ordinal)
    {
        "mscorlib",
        "netstandard",
        "System.Private.CoreLib",
        "System.Runtime",
        "System.Runtime.InteropServices",
        "System.Collections",
        "System.Collections.Concurrent",
        "System.Collections.Immutable",
        "System.Linq",
        "System.Memory",
        "System.Threading",
        "System.Threading.Tasks",
        "System.Threading.Tasks.Extensions",
        "System.Private.Uri"
    };

    readonly IReadOnlyList<string> _imageRoots;
    readonly Dictionary<string, string> _managed = new(StringComparer.Ordinal);
    readonly Dictionary<string, string> _native = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="AdmissionLoadContext"/> class.
    /// </summary>
    /// <param name="name">Name of the context, for diagnostics.</param>
    /// <param name="imageRoots">Absolute, existing, non-overlapping directories that together form one image closure.</param>
    public AdmissionLoadContext(string name, IReadOnlyList<string> imageRoots)
        : base(name, isCollectible: true)
    {
        _imageRoots = ValidateRoots(imageRoots);
        Discover();
    }

    /// <summary>
    /// Verifies that the requested type is either owned by this context or is a shared BCL type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    public void EnsureContained(Type type)
    {
        var assembly = type.Assembly;
        var context = GetLoadContext(assembly);
        if (ReferenceEquals(context, this))
        {
            return;
        }

        if (ReferenceEquals(context, Default) && SharedBcl.Contains(assembly.GetName().Name!))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Type '{type.FullName}' from '{assembly.GetName().Name}' escaped context '{Name}' (owner: '{context?.Name ?? "unknown"}').");
    }

    /// <summary>
    /// Verifies that the static AssemblyRef closure of a root assembly resolves entirely inside this image closure
    /// or the shared BCL list, without loading anything.
    /// </summary>
    /// <param name="rootAssemblyName">Simple name of the root assembly, which must be in the closure.</param>
    public void EnsureClosed(string rootAssemblyName)
    {
        if (!_managed.ContainsKey(rootAssemblyName))
        {
            throw new FileNotFoundException($"Closure root '{rootAssemblyName}' is not in image closure '{Name}'.", rootAssemblyName);
        }

        var problems = new SortedSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>();
        pending.Push(rootAssemblyName);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var reference in ReadReferences(_managed[current]))
            {
                if (SharedBcl.Contains(reference.Name!))
                {
                    continue;
                }

                if (!_managed.TryGetValue(reference.Name!, out var path))
                {
                    problems.Add($"{reference.Name} (referenced by {current}) is unresolved");
                    continue;
                }

                var found = AssemblyName.GetAssemblyName(path);
                if (found.Version < reference.Version)
                {
                    problems.Add($"{reference.Name} requires {reference.Version} but closure has {found.Version}");
                    continue;
                }

                pending.Push(reference.Name!);
            }
        }

        if (problems.Count > 0)
        {
            throw new FileNotFoundException($"Image closure '{Name}' is not closed: {string.Join("; ", problems)}", rootAssemblyName);
        }
    }

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name ?? throw new InvalidOperationException("Assembly reference has no simple name.");
        if (SharedBcl.Contains(name))
        {
            return LoadShared(assemblyName);
        }

        if (!_managed.TryGetValue(name, out var path))
        {
            throw new FileNotFoundException($"Assembly '{name}' is not part of image closure '{Name}'; host fallback is not allowed.", name);
        }

        var found = AssemblyName.GetAssemblyName(path);
        if (assemblyName.Version is not null && found.Version < assemblyName.Version)
        {
            throw new InvalidOperationException($"Assembly '{name}' requires {assemblyName.Version} but closure has {found.Version}.");
        }

        var requestedToken = assemblyName.GetPublicKeyToken();
        if (requestedToken is { Length: > 0 } && !requestedToken.AsSpan().SequenceEqual(found.GetPublicKeyToken()))
        {
            throw new InvalidOperationException($"Assembly '{name}' public key token does not match the closure.");
        }

        return LoadFromAssemblyPath(path);
    }

    /// <inheritdoc/>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var matches = _native
            .Where(entry => NativeNameMatches(entry.Key, unmanagedDllName))
            .Select(entry => entry.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return matches.Length switch
        {
            1 => LoadUnmanagedDllFromPath(matches[0]),
            0 => throw new DllNotFoundException($"Native asset '{unmanagedDllName}' is not part of image closure '{Name}'."),
            _ => throw new InvalidOperationException($"Native asset '{unmanagedDllName}' is ambiguous in image closure '{Name}'.")
        };
    }

    static bool NativeNameMatches(string fileName, string requested)
    {
        var stem = Path.GetFileNameWithoutExtension(requested);
        return string.Equals(fileName, requested, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, $"lib{requested}.so", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, $"lib{requested}.dylib", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, $"{requested}.so", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, $"{requested}.dylib", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, $"{requested}.dll", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, $"lib{stem}.so", StringComparison.OrdinalIgnoreCase);
    }

    static List<string> ValidateRoots(IReadOnlyList<string> imageRoots)
    {
        if (imageRoots.Count == 0)
        {
            throw new InvalidOperationException("An image closure needs at least one explicit root.");
        }

        var roots = new List<string>();
        foreach (var root in imageRoots)
        {
            if (!Path.IsPathRooted(root))
            {
                throw new InvalidOperationException($"Image root '{root}' must be absolute.");
            }

            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException($"Image root '{root}' does not exist.");
            }

            var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            if (new DirectoryInfo(full).LinkTarget is not null)
            {
                throw new InvalidOperationException($"Image root '{root}' must not be a link.");
            }

            roots.Add(full);
        }

        for (var first = 0; first < roots.Count; first++)
        {
            for (var second = first + 1; second < roots.Count; second++)
            {
                var a = roots[first];
                var b = roots[second];
                if (a.Equals(b, StringComparison.Ordinal)
                    || a.StartsWith(b + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    || b.StartsWith(a + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Image roots '{a}' and '{b}' overlap; selection is ambiguous.");
                }
            }
        }

        return roots;
    }

    static IEnumerable<AssemblyName> ReadReferences(string path)
    {
        using var stream = File.OpenRead(path);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        return reader.AssemblyReferences
            .Select(handle => reader.GetAssemblyReference(handle).GetAssemblyName())
            .ToArray();
    }

    static bool IsManaged(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var peReader = new PEReader(stream);
            return peReader.HasMetadata;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    static void Register(Dictionary<string, string> index, string key, string path, string kind)
    {
        if (index.TryGetValue(key, out var existing) && !Hash(existing).Equals(Hash(path), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Conflicting {kind} asset '{key}': '{existing}' and '{path}' differ.");
        }

        index.TryAdd(key, path);
    }

    Assembly LoadShared(AssemblyName requested)
    {
        var assembly = Default.LoadFromAssemblyName(requested);
        var host = assembly.GetName();
        if (requested.Version is not null && host.Version < requested.Version)
        {
            throw new InvalidOperationException(
                $"Shared BCL '{host.Name}' in the host is {host.Version} but the image requires {requested.Version}.");
        }

        return assembly;
    }

    void Discover()
    {
        var options = new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = false };
        foreach (var root in _imageRoots)
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", options).Order(StringComparer.Ordinal))
            {
                var info = new FileInfo(file);
                if (info.LinkTarget is not null)
                {
                    throw new InvalidOperationException($"Closure asset '{file}' is a link and may escape its root.");
                }

                var extension = info.Extension.ToLowerInvariant();
                if (extension is ".so" or ".dylib" || (extension == ".dll" && !IsManaged(file)))
                {
                    Register(_native, info.Name, file, "native");
                }
                else if (extension == ".dll")
                {
                    Register(_managed, Path.GetFileNameWithoutExtension(file), file, "managed");
                }
            }
        }
    }
}
