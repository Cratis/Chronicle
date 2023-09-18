// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;
using Orleans.Providers;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.New;

/// <summary>
/// Represents a supervisor for <see cref="IObserver"/>.
/// </summary>
[StorageProvider(ProviderName = ObserverState.StorageProvider)]
public class ObserverSupervisor : StateMachine<ObserverState>, IObserverSupervisor
{
    IStreamProvider _streamProvider = null!;

    ObserverId _observerId = Guid.Empty;
    ObserverKey _observerKey = ObserverKey.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverSupervisor"/> class.
    /// </summary>
    public ObserverSupervisor()
    {
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Keep the Grain alive forever: Confirmed here: https://github.com/dotnet/orleans/issues/1721#issuecomment-216566448
        DelayDeactivation(TimeSpan.MaxValue);

        _observerId = this.GetPrimaryKey(out var keyAsString);
        _observerKey = ObserverKey.Parse(keyAsString);

        _streamProvider = this.GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SetNameAndType(ObserverName name, ObserverType type)
    {
        State.Name = name;
        State.Type = type;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task Subscribe<TObserverSubscriber>(IEnumerable<EventType> eventTypes, object? subscriberArgs = null)
        where TObserverSubscriber : IObserverSubscriber
    {
        await TransitionTo<States.Subscribing>();
    }

    /// <inheritdoc/>
    public override IImmutableList<IState<ObserverState>> CreateStates() => new IState<ObserverState>[]
    {
        new States.Disconnected(),
        new States.CatchUp(),
        new States.Replay(),
        new States.Indexing(),
        new States.Observing(
            this,
            _streamProvider,
            _observerKey.MicroserviceId,
            _observerKey.TenantId,
            _observerKey.EventSequenceId)
    }.ToImmutableList();

    /// <inheritdoc/>
    public Task Unsubscribe() => TransitionTo<States.Disconnected>();

    /// <inheritdoc/>
    public Task Replay() => TransitionTo<States.Replay>();

    /// <inheritdoc/>
    public Task ReplayPartition(EventSourceId partition) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task ReplayPartitionTo(EventSourceId partition, EventSequenceNumber sequenceNumber) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task PartitionFailed(EventSourceId partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Handle(EventSourceId eventSourceId, IEnumerable<AppendedEvent> events)
    {
        // Info it needs:
        // - Current subscription information
        // - Failed Partitions
        // - Current Sequence Number

        // If observer is disconnected, we should not handle the event

        // If sequence number is greater than or equal to next event sequence number, we should not handle the event

        // If Partition is failed, we should not handle the event

        // For replaying or failed partition recovery of a specific partition, the NextSequenceNumber shouldn't be updated.

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task OnAfterEnteringState(IState<ObserverState> state)
    {
        if (state is States.BaseObserverState observerState)
        {
            State.RunningState = observerState.RunningState;
            await WriteStateAsync();
        }
    }
}
