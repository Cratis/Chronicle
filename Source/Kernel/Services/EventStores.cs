// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services;

/// <summary>.
/// Represents an implementation of <see cref="IEventStores"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage">The <see cref="IStorage"/> for working with the storage.</param>
internal sealed class EventStores(IGrainFactory grainFactory, IStorage storage) : IEventStores
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
        _ = storage.GetEventStore(command.Name);
        var namespaces = grainFactory.GetGrain<Grains.Namespaces.INamespaces>(command.Name);
        await namespaces.EnsureDefault();

        if (!string.IsNullOrWhiteSpace(command.Description))
        {
            var @event = new Grains.Events.EventStoreDomainSpecified(command.Name, new DomainSpecification(command.Description));
            var eventLog = grainFactory.GetEventLog();
            await eventLog.Append(command.Name, @event);
        }
    }

    /// <inheritdoc/>
    public async Task SetDomainSpecification(SetEventStoreDomainSpecification command)
    {
        var @event = new Grains.Events.EventStoreDomainSpecified(command.EventStore, new DomainSpecification(command.Specification));
        var eventLog = grainFactory.GetEventLog();
        await eventLog.Append(command.EventStore, @event);
    }
}
