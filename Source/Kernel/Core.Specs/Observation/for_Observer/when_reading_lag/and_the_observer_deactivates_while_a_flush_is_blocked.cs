// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_reading_lag;

public class and_the_observer_deactivates_while_a_flush_is_blocked : given.an_observer_publishing_a_stalled_sample
{
    readonly for_Observer.given.blockable_observer_state_write _stateWrite = new();
    Task _deactivation;
    ObserverLagSample _publishedWhileBlocked;

    public and_the_observer_deactivates_while_a_flush_is_blocked() =>
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _stateWrite : null!;

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, StallWindow = TimeSpan.FromSeconds(30), StatePersistenceBatchInterval = 3 };

    async Task Because()
    {
        await _observer.Handle("p1", [AppendedEvent.EmptyWithEventSequenceNumber(0UL)]);
        _stateWrite.Block = true;
        _deactivation = _observer.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ShuttingDown, string.Empty), CancellationToken.None);
        await _stateWrite.Writing.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        _publishedWhileBlocked = Published;
        _stateWrite.Release.SetResult();
        await _deactivation;
    }

    [Fact] void should_have_withdrawn_the_sample_before_the_flush_finished() => _publishedWhileBlocked.ShouldBeNull();
}
