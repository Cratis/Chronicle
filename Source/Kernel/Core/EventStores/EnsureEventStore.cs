// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the command for ensuring an event store exists and is initialized.
/// </summary>
/// <param name="Name">The name of the event store to ensure.</param>
[Command]
[BelongsTo(WellKnownServices.EventStores)]
public record EnsureEventStore(EventStoreName Name)
{
    /// <summary>
    /// Handles the command through the store's cluster-serialized initializer.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IGrainFactory grainFactory) => grainFactory.GetGrain<IEventStoreInitializer>(Name).Ensure();
}
