// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the observing state of an observer.
/// </summary>
public class Observing : BaseObserverState
{
    readonly IObserver _observer;
    readonly IStreamProvider _streamProvider;
    readonly MicroserviceId _microserviceId;
    readonly TenantId _tenantId;
    readonly EventSequenceId _eventSequenceId;
    readonly ILogger<Observing> _logger;
    StreamSubscriptionHandle<AppendedEvent>? _streamSubscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observing"/> class.
    /// </summary>
    /// <param name="observer"><see cref="IObserver"/> for handling events.</param>
    /// <param name="streamProvider"><see cref="IStreamProvider"/> to use to work with streams.</param>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the state is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the state is for.</param>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> being observed.</param>
    /// <param name="logger">Logger for logging.</param>
    public Observing(
        IObserver observer,
        IStreamProvider streamProvider,
        MicroserviceId microserviceId,
        TenantId tenantId,
        EventSequenceId eventSequenceId,
        ILogger<Observing> logger)
    {
        _observer = observer;
        _streamProvider = streamProvider;
        _microserviceId = microserviceId;
        _tenantId = tenantId;
        _eventSequenceId = eventSequenceId;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override StateName Name => "Observing";

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Active;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(CatchUp),
        typeof(Replay),
        typeof(Indexing),
        typeof(Disconnected)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var scope = _logger.BeginObservingScope(state, _microserviceId, _tenantId, _eventSequenceId);
        _logger.Entering();

        var microserviceAndTenant = new MicroserviceAndTenant(_microserviceId, _tenantId);
        var streamId = StreamId.Create(microserviceAndTenant, _eventSequenceId);
        var stream = _streamProvider.GetStream<AppendedEvent>(streamId);

        _logger.SubscribingToStream(state.NextEventSequenceNumber);

        _streamSubscription = await stream.SubscribeAsync(
            async (@event, _) => await _observer.Handle(@event.Context.EventSourceId, new[] { @event }),
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
