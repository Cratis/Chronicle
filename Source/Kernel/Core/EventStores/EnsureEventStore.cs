// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.Reactors.Kernel;

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
    /// Refreshes per-connection artifacts and ensures first-time store initialization has completed.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="initialization">The event store initialization service.</param>
    /// <param name="eventTypes">The server event types to refresh.</param>
    /// <param name="reactors">The kernel reactors to refresh.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Handle(IGrainFactory grainFactory, IEventStoreInitialization initialization, IEventTypes eventTypes, IReactors reactors)
    {
        // Preserve per-connection refresh without serializing all reconnects behind one initializer grain.
        await eventTypes.DiscoverAndRegister(Name);
        await reactors.DiscoverAndRegister(Name, EventStoreNamespaceName.Default);
        await grainFactory.GetGrain<INamespaces>(Name).EnsureDefault();

        await initialization.Ensure(Name);
    }
}
