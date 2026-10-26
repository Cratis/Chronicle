// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ReleasedKernelConfiguration;

sealed class ReleasedConfigurationLoadContext(string runtimeDirectory) : AssemblyLoadContext(isCollectible: true)
{
    /// <summary>
    /// Only runtime and configuration contracts may cross the boundary. In particular, never share Cratis or Orleans.
    /// </summary>
    static readonly HashSet<string> _sharedContracts = new(StringComparer.Ordinal)
    {
        "Microsoft.Extensions.Configuration",
        "Microsoft.Extensions.Configuration.Abstractions",
        "Microsoft.Extensions.Configuration.Binder",
        "Microsoft.Extensions.Configuration.EnvironmentVariables",
        "Microsoft.Extensions.Configuration.FileExtensions",
        "Microsoft.Extensions.Configuration.Json",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.FileProviders.Abstractions",
        "Microsoft.Extensions.FileProviders.Physical",
        "Microsoft.Extensions.FileSystemGlobbing",
        "Microsoft.Extensions.Options",
        "Microsoft.Extensions.Options.ConfigurationExtensions",
        "Microsoft.Extensions.Options.DataAnnotations",
        "Microsoft.Extensions.Primitives"
    };

    static readonly HashSet<string> _runtimeAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(path => Path.GetDirectoryName(path) == Path.GetDirectoryName(typeof(object).Assembly.Location))
        .Select(path => Path.GetFileNameWithoutExtension(path))
        .ToHashSet(StringComparer.Ordinal);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name!;
        if (_runtimeAssemblies.Contains(name) || _sharedContracts.Contains(name))
        {
            return Default.LoadFromAssemblyName(assemblyName);
        }

        // A flat copy of the pinned image's /app is the only release-owned dependency source.
        var path = Path.Combine(runtimeDirectory, $"{name}.dll");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Released configuration dependency is missing: {name}", path);
        }

        return LoadFromAssemblyPath(path);
    }
}
