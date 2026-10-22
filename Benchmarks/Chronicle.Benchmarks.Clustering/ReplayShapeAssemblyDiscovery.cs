// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Types;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Keeps the diagnostic projection out of every other benchmark's artifact registration.
/// </summary>
/// <param name="assembly">The benchmark assembly.</param>
/// <param name="includeReplayShape">Whether this is the replay-shape scenario.</param>
internal sealed class ReplayShapeAssemblyDiscovery(Assembly assembly, bool includeReplayShape) : ICanProvideAssembliesForDiscovery
{
    /// <inheritdoc/>
    public IEnumerable<Assembly> Assemblies => [assembly];

    /// <inheritdoc/>
    public IEnumerable<Type> DefinedTypes => assembly.DefinedTypes.Select(type => type.AsType())
        .Where(type => includeReplayShape || !type.Name.StartsWith("ReplayShape", StringComparison.Ordinal));

    /// <inheritdoc/>
    public void Initialize()
    {
    }
}
