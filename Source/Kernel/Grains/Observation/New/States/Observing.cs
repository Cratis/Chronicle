// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.New.States;

/// <summary>
/// Represents the observing state of an observer.
/// </summary>
public class Observing : BaseObserverState
{
    readonly IObserverEventHandler _eventHandler;
    readonly IStreamProvider _streamProvider;
    readonly ObserverId _observerId;
    readonly MicroserviceId _microserviceId;
    readonly TenantId _tenantId;
    readonly EventSequenceId _eventSequenceId;
    StreamSubscriptionHandle<AppendedEvent>? _streamSubscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observing"/> class.
    /// </summary>
    /// <param name="eventHandler"><see cref="IObserverEventHandler"/> for handling events.</param>
    /// <param name="streamProvider"><see cref="IStreamProvider"/> to use to work with streams.</param>
    /// <param name="observerId"><see cref="ObserverId"/> that identifies the observer.</param>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the state is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the state is for.</param>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> being observed.</param>
    public Observing(
        IObserverEventHandler eventHandler,
        IStreamProvider streamProvider,
        ObserverId observerId,
        MicroserviceId microserviceId,
        TenantId tenantId,
        EventSequenceId eventSequenceId)
    {
        _eventHandler = eventHandler;
        _streamProvider = streamProvider;
        _observerId = observerId;
        _microserviceId = microserviceId;
        _tenantId = tenantId;
        _eventSequenceId = eventSequenceId;
    }

    /// <inheritdoc/>
    public override StateName Name => "CatchUp";

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Active;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(CatchUp),
        typeof(Replay),
        typeof(Indexing)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        var microserviceAndTenant = new MicroserviceAndTenant(_microserviceId, _tenantId);
        var streamId = StreamId.Create(microserviceAndTenant, _eventSequenceId);
        var stream = _streamProvider.GetStream<AppendedEvent>(streamId);
        _streamSubscription = await stream.SubscribeAsync(
            async (@event, _) =>
            {
                if (state.EventTypes.Any(et => et == @event.Metadata.Type))
                {
                    await _eventHandler.Handle(_observerId, _microserviceId, _tenantId, @event);
                }
            },
            new EventSequenceNumberToken(state.NextEventSequenceNumber));

        return state;
    }

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state)
    {
        _streamSubscription?.UnsubscribeAsync();
        _streamSubscription = null;
        return Task.FromResult(state);
    }
}
