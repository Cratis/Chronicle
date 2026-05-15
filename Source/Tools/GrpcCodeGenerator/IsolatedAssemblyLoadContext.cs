// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Custom assembly load context for isolating loaded assemblies.
/// </summary>
/// <param name="assemblyPath">The path to the assembly to load.</param>
sealed class IsolatedAssemblyLoadContext(string assemblyPath) : AssemblyLoadContext(isCollectible: true)
{
    /// <summary>Additional probe paths for NuGet packages that the resolver may not find.</summary>
    static readonly string[] _nugetProbePaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"),
        Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? string.Empty,
    ];

    readonly AssemblyDependencyResolver _resolver = new(assemblyPath);

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (resolvedPath != null)
        {
            return LoadFromAssemblyPath(resolvedPath);
        }

        // Fall back to probing the NuGet package cache for packages whose assembly version
        // does not match the NuGet package version (common in Arc and other packages).
        if (assemblyName.Name is not null)
        {
            foreach (var root in _nugetProbePaths)
            {
                if (string.IsNullOrEmpty(root))
                {
                    continue;
                }

                var packageDir = Path.Combine(root, assemblyName.Name.ToLowerInvariant());
                if (!Directory.Exists(packageDir))
                {
                    continue;
                }

                // Take the latest (alphabetically last) version folder.
                foreach (var versionDir in Directory.GetDirectories(packageDir).OrderDescending())
                {
                    // Probe typical TFM library paths.
                    foreach (var tfm in new[] { "net10.0", "net9.0", "net8.0", "netstandard2.0" })
                    {
                        var candidate = Path.Combine(versionDir, "lib", tfm, $"{assemblyName.Name}.dll");
                        if (File.Exists(candidate))
                        {
                            return LoadFromAssemblyPath(candidate);
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <inheritdoc/>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
