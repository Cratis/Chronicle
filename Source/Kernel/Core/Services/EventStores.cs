// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Represents an implementation of <see cref="IEventStores"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage">The <see cref="IStorage"/> for working with the storage.</param>
/// <param name="eventTypes">The <see cref="IEventTypes"/> for managing event types.</param>
/// <param name="reactors">The <see cref="IReactors"/> for discovering and registering kernel reactors.</param>
internal sealed class EventStores(IGrainFactory grainFactory, IStorage storage, IEventTypes eventTypes, IReactors reactors) : IEventStores
{
    /// <inheritdoc/>
    public Task<CommandResult> EnsureEventStore(EnsureEventStoreRequest request, CallContext callContext = default) =>
        CommandExecutor.Execute(
            new Chronicle.EventStores.EnsureEventStore(request.Name),
            command => command.Handle(grainFactory, storage, eventTypes, reactors));

    /// <inheritdoc/>
    public IObservable<QueryResult<IEnumerable<string>>> AllEventStores(CallContext callContext = default) =>
        QueryExecutor.Execute(() =>
            Chronicle.EventStores.EventStoreNames.AllEventStores(storage)
                .CompletedBy(callContext.CancellationToken));
}
