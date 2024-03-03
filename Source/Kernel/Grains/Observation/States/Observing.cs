// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Kernel.EventSequences;
using Cratis.Kernel.Storage.Observation;
using Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;

namespace Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the observing state of an observer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observing"/> class.
/// </remarks>
/// <param name="streamProvider"><see cref="IStreamProvider"/> to use to work with streams.</param>
/// <param name="microserviceId"><see cref="MicroserviceId"/> the state is for.</param>
/// <param name="tenantId"><see cref="TenantId"/> the state is for.</param>
/// <param name="eventSequenceId"><see cref="EventSequenceId"/> being observed.</param>
/// <param name="logger">Logger for logging.</param>
public class Observing(
    IStreamProvider streamProvider,
    MicroserviceId microserviceId,
    TenantId tenantId,
    EventSequenceId eventSequenceId,
    ILogger<Observing> logger) : BaseObserverState
{
    StreamSubscriptionHandle<AppendedEvent>? _streamSubscription;

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Active;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Routing),
        typeof(CatchUp),
        typeof(Replay),
        typeof(Indexing),
        typeof(Disconnected)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var scope = logger.BeginObservingScope(state, microserviceId, tenantId, eventSequenceId);
        logger.Entering();

        var microserviceAndTenant = new MicroserviceAndTenant(microserviceId, tenantId);
        var streamId = StreamId.Create(microserviceAndTenant, eventSequenceId);
        var stream = streamProvider.GetStream<AppendedEvent>(streamId);

        logger.SubscribingToStream(state.NextEventSequenceNumber);

        _streamSubscription = await stream.SubscribeAsync(
            async (@event, _) => await Observer.Handle(@event.Context.EventSourceId, new[] { @event }),
            new EventSequenceNumberToken(state.NextEventSequenceNumber));

        await stream.WarmUp();

        return state;
    }

    /// <inheritdoc/>
    public override async Task<ObserverState> OnLeave(ObserverState state)
    {
        await (_streamSubscription?.UnsubscribeAsync() ?? Task.CompletedTask);
        _streamSubscription = null;
        return state;
    }
}
