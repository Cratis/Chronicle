// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.given;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_controlled_persistence : an_observer
{
    protected readonly controlled_observer_state_storage _writes = new();
    protected readonly blockable_state_storage<ObserverDefinition> _definition = new();
    protected readonly DeliveryClock _clock = new();
    protected readonly DateTimeOffset _now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    public an_observer_with_controlled_persistence()
    {
        _clock.Advance(_now - DateTimeOffset.UnixEpoch);
        _timeProvider = _clock;
        _silo.Options.StorageFactory = type => type switch
        {
            _ when type == typeof(ObserverState) => _writes,
            _ when type == typeof(ObserverDefinition) => _definition,
            _ => null!
        };
    }

    async Task Establish()
    {
        await Subscribe();
        _writes.Stats.ResetCounts();
        _eventStoreNamespaceStorage.Observers.ClearReceivedCalls();
        _clock.Advance(TimeSpan.FromHours(1));
    }

    protected Task Subscribe() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero, automatic: true);

    protected void ShouldNotSaveTimestamp() => _eventStoreNamespaceStorage.Observers.DidNotReceive()
        .SaveRunningStateChangedAt(Arg.Any<ObserverId>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Concepts.EventSequences.EventSequenceId>());
}
