// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents the command for ensuring a namespace exists within an event store.
/// </summary>
/// <param name="EventStore">The name of the event store.</param>
/// <param name="Namespace">The name of the namespace to ensure.</param>
[Command]
public record EnsureNamespace(string EventStore, string Namespace)
{
    /// <summary>
    /// Handles the command by invoking <see cref="INamespaces.Ensure"/> on the target namespaces grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get namespace grains with.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(IGrainFactory grainFactory)
    {
        var namespaces = grainFactory.GetGrain<INamespaces>((EventStoreName)EventStore);
        await namespaces.Ensure((EventStoreNamespaceName)Namespace);
    }
}
