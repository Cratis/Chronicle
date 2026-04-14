// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Observation.Reactors.Kernel;
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
/// <param name="reactors">The <see cref="IReactors"/> for discovering and registering kernel reactors.</param>
internal sealed class EventStores(IGrainFactory grainFactory, IStorage storage, IEventTypes eventTypes, IReactors reactors) : IEventStores
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

        // Always register server event types, even if the store already exists.
        // Storage.GetEventStore() upserts the record on every call, so HasEventStore may return
        // true for a store that was implicitly created (e.g. by an observable query) before
        // Ensure was called — causing DiscoverAndRegister to be skipped and leaving server
        // types such as EventRedactionRequested unregistered.
        await eventTypes.DiscoverAndRegister(eventStoreName);

        // Always register kernel reactors in the default namespace.
        // A store can exist without having emitted EventStoreAdded (for example if it was
        // implicitly created), in which case ReactorsReactor would not have run.
        await reactors.DiscoverAndRegister(eventStoreName, Concepts.EventStoreNamespaceName.Default);

        if (!exists)
        {
            var systemEventSequence = grainFactory.GetSystemEventSequence(Concepts.EventStoreName.System);
            var eventStoreAdded = new EventStoreAdded(eventStoreName);
            await systemEventSequence.Append(eventStoreName.Value, eventStoreAdded);
        }

        var namespaces = grainFactory.GetGrain<Chronicle.Namespaces.INamespaces>(eventStoreName);
        await namespaces.EnsureDefault();
    }
}
