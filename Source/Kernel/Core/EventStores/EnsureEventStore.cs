// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the command for ensuring an event store exists, creating it if it does not.
/// </summary>
/// <param name="Name">The name of the event store to ensure.</param>
[Command]
[BelongsTo(WellKnownServices.EventStores)]
public record EnsureEventStore(string Name)
{
    /// <summary>
    /// Handles the command by registering server event types and reactors, and if new, appending an <see cref="EventStoreAdded"/> event.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get grain references with.</param>
    /// <param name="storage">The <see cref="IStorage"/> to check and provision the event store in.</param>
    /// <param name="eventTypes">The <see cref="IEventTypes"/> to discover and register event types with.</param>
    /// <param name="reactors">The <see cref="IReactors"/> to discover and register kernel reactors with.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(IGrainFactory grainFactory, IStorage storage, IEventTypes eventTypes, IReactors reactors)
    {
        var eventStoreName = new EventStoreName(Name);
        var exists = await storage.HasEventStore(eventStoreName);
        _ = storage.GetEventStore(eventStoreName);

        // Always register server event types, even if the store already exists, so that
        // built-in types such as EventRedactionRequested are always up to date.
        await eventTypes.DiscoverAndRegister(eventStoreName);

        // Always register kernel reactors in the default namespace.
        // A store can exist without having emitted EventStoreAdded (for example if it was
        // implicitly created), in which case ReactorsReactor would not have run.
        await reactors.DiscoverAndRegister(eventStoreName, Concepts.EventStoreNamespaceName.Default);

        if (!exists)
        {
            var systemEventSequence = grainFactory.GetSystemEventSequence(EventStoreName.System);
            await systemEventSequence.Append(eventStoreName.Value, new EventStoreAdded(eventStoreName));
        }

        var namespaces = grainFactory.GetGrain<INamespaces>(eventStoreName);
        await namespaces.EnsureDefault();
    }
}
