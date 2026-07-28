// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Types;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Provides type discovery scoped to a single assembly, keeping artifact registration limited to the
/// benchmark assembly instead of everything the silo happens to have loaded.
/// </summary>
/// <param name="assembly">The <see cref="Assembly"/> to discover types from.</param>
public sealed class BenchmarkAssemblyDiscovery(Assembly assembly) : ICanProvideAssembliesForDiscovery
{
    /// <inheritdoc/>
    public IEnumerable<Assembly> Assemblies => [assembly];

    /// <inheritdoc/>
    public IEnumerable<Type> DefinedTypes => assembly.DefinedTypes.Select(definedType => definedType.AsType());

    /// <inheritdoc/>
    public void Initialize()
    {
    }
}
