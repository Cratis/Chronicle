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
    readonly AssemblyDependencyResolver _resolver = new(assemblyPath);

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (resolvedPath != null)
        {
            return LoadFromAssemblyPath(resolvedPath);
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
