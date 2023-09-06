// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;
using Orleans.Providers;

namespace Aksio.Cratis.Kernel.Grains.Observation.New;

/// <summary>
/// Represents a supervisor for <see cref="IObserver"/>.
/// </summary>
[StorageProvider(ProviderName = ObserverState.StorageProvider)]
public class ObserverSupervisor : StateMachine<ObserverState>, IObserverSupervisor
{
    ObserverId _observerId = Guid.Empty;
    ObserverKey _observerKey = ObserverKey.NotSet;


    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Keep the Grain alive forever: Confirmed here: https://github.com/dotnet/orleans/issues/1721#issuecomment-216566448
        DelayDeactivation(TimeSpan.MaxValue);

        _observerId = this.GetPrimaryKey(out var keyAsString);
        _observerKey = ObserverKey.Parse(keyAsString);

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
    public async Task Subscribe<TObserverSubscriber>(IEnumerable<EventType> eventTypes, object? subscriberArgs = null) where TObserverSubscriber : IObserverSubscriber
    {
        await TransitionTo<States.CatchUp>();
    }

    /// <inheritdoc/>
    public Task Unsubscribe() => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Replay() => TransitionTo<States.Replay>();

    /// <inheritdoc/>
    public Task ReplayPartition(EventSourceId partition) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task ReplayPartitionTo(EventSourceId partition, EventSequenceNumber sequenceNumber) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task PartitionFailed(EventSourceId partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace) => throw new NotImplementedException();
}
