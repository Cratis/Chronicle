// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Cratis.Types;

/// <summary>
/// Represents an implementation of <see cref="ICanProvideAssembliesForDiscovery"/> that provides package referenced assemblies.
/// </summary>
public class PackageReferencedAssemblies : ICanProvideAssembliesForDiscovery
{
    /// <summary>
    /// Gets the global instance of <see cref="PackageReferencedAssemblies"/>.
    /// </summary>
    /// <remarks>
    /// Its recommended to use the singleton defined here, rather than building your own instance.
    /// This is due to the performance impact of scanning all assemblies in the application.
    /// </remarks>
    public static readonly PackageReferencedAssemblies Instance = new();

    static readonly object _lock = new();

    readonly List<string> _assemblyPrefixesToInclude =
    [
        "Aksio"
    ];

    readonly List<Assembly> _assemblies = [];

    bool _initialized;

    /// <inheritdoc/>
    public IEnumerable<Assembly> Assemblies => _assemblies;

    /// <inheritdoc/>
    public IEnumerable<Type> DefinedTypes { get; private set; } = Enumerable.Empty<Type>();

    /// <inheritdoc/>
    public void Initialize()
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            var entryAssembly = Assembly.GetEntryAssembly();
            var dependencyModel = DependencyContext.Load(entryAssembly!);

            var assemblies = dependencyModel!.RuntimeLibraries
                                .Where(_ => !_.Type.Equals("project") &&
                                            _.RuntimeAssemblyGroups.Count > 0 &&
                                            _assemblyPrefixesToInclude.Exists(asm => _.Name.StartsWith(asm)))
                                .Select(_ => AssemblyHelpers.Resolve(_.Name)!)
                                .Where(_ => _ is not null)
                                .Distinct()
                                .ToArray();
            _assemblies.AddRange(assemblies);
            DefinedTypes = _assemblies.SelectMany(_ => _.DefinedTypes).ToArray();

            _initialized = true;
        }
    }

    /// <summary>
    /// Add an assembly prefix to exclude from type discovery.
    /// </summary>
    /// <param name="prefixes">Prefixes to add.</param>
    public void AddAssemblyPrefixesToInclude(params string[] prefixes) => _assemblyPrefixesToInclude.AddRange(prefixes);
}
