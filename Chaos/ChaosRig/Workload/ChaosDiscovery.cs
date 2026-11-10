// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Chaos;

internal sealed class ChaosDiscovery(string role) : ICanProvideAssembliesForDiscovery
{
    public IEnumerable<Assembly> Assemblies => [typeof(ChaosEvent).Assembly];

    public IEnumerable<Type> DefinedTypes => role switch
    {
        "producer" => [typeof(ChaosEvent)],
        "consumer" => [typeof(ChaosEvent), typeof(ChaosReactor), typeof(ChaosReducer), typeof(ChaosProjection), typeof(ChaosProjectionModel), typeof(ChaosReductionModel)],
        _ => [typeof(ChaosEvent), typeof(ChaosReducer), typeof(ChaosProjection), typeof(ChaosProjectionModel), typeof(ChaosReductionModel)]
    };

    public void Initialize() { }
}
