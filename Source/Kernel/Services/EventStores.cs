// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services;

/// <summary>.
/// Represents an implementation of <see cref="IEventStores"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage">The <see cref="IStorage"/> for working with the storage.</param>
/// <param name="eventTypes">The <see cref="IEventTypes"/> for managing event types.</param>
internal sealed class EventStores(IGrainFactory grainFactory, IStorage storage, IEventTypes eventTypes) : IEventStores
{
    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetEventStores()
    {
        var eventStores = await storage.GetEventStores();
        return eventStores.Select(_ => _.Value).ToArray();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<string>> ObserveEventStores(CallContext callContext = default) =>
        storage
            .ObserveEventStores()
            .CompletedBy(callContext.CancellationToken)
            .Select(_ => _.Select(e => e.Value).ToArray());

    /// <inheritdoc/>
    public async Task Ensure(EnsureEventStore command)
    {
        var eventStoreName = new Concepts.EventStoreName(command.Name);
        var exists = await storage.HasEventStore(eventStoreName);
        _ = storage.GetEventStore(eventStoreName);

        if (!exists)
        {
            await eventTypes.DiscoverAndRegister(eventStoreName);

            var systemEventSequence = grainFactory.GetSystemEventSequence(Concepts.EventStoreName.System);
            var eventStoreAdded = new EventStoreAdded(eventStoreName);
            await systemEventSequence.Append(eventStoreName.Value, eventStoreAdded);
        }

        var namespaces = grainFactory.GetGrain<Chronicle.Namespaces.INamespaces>(eventStoreName);
        await namespaces.EnsureDefault();
    }
}
