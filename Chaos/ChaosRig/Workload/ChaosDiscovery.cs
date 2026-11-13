// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Types;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Restricts discovered workload artifacts to the process role.
/// </summary>
/// <param name="role">The workload process role.</param>
internal sealed class ChaosDiscovery(string role) : ICanProvideAssembliesForDiscovery
{
    /// <inheritdoc/>
    public IEnumerable<Assembly> Assemblies => [typeof(ChaosEvent).Assembly];

    /// <inheritdoc/>
    public IEnumerable<Type> DefinedTypes => role switch
    {
        "producer" => [typeof(ChaosEvent)],
        "consumer" => [typeof(ChaosEvent), typeof(ChaosReactor), typeof(ChaosReducer), typeof(ChaosProjection), typeof(ChaosProjectionModel), typeof(ChaosReductionModel)],
        _ => [typeof(ChaosEvent), typeof(ChaosReducer), typeof(ChaosProjection), typeof(ChaosProjectionModel), typeof(ChaosReductionModel)]
    };

    /// <inheritdoc/>
    public void Initialize()
    {
    }
}
