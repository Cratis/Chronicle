// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_debouncing_progress_persistence;

/// <summary>
/// Proves the durability relaxation is safe: when a crash happens between debounced writes, the observer
/// resumes from the last persisted <see cref="Storage.Observation.ObserverState.NextEventSequenceNumber"/>,
/// re-processing the events it had only advanced past in memory. Because the debounced range holds only events
/// the observer skipped and observers are idempotent, re-processing loses nothing, and a subscribed event that
/// followed the unpersisted progress is still delivered — never skipped.
/// </summary>
public class and_recovering_after_a_crash_lost_unpersisted_progress : given.an_observer_debouncing_progress_persistence
{
    void Establish() =>
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(2UL));

    async Task Because()
    {
        // Advance past two events the observer is not subscribed to. Below the interval, so nothing is persisted.
        await _observer.Handle("p1", [AppendedEvent.EmptyWithEventSequenceNumber(0UL)]);
        await _observer.Handle("p1", [AppendedEvent.EmptyWithEventSequenceNumber(1UL)]);
        _unpersistedWrites = _storageStats.Writes;

        // Model the crash and reactivation: the grain reloads the last persisted state, which — since nothing was
        // written above — is still the point before the debounced advance. Catch-up resumes from there.
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 0UL };

        // Catch-up re-delivers the range from the persisted point, now with a subscribed event at the end.
        await _observer.Handle("p1",
        [
            AppendedEvent.EmptyWithEventSequenceNumber(0UL),
            AppendedEvent.EmptyWithEventSequenceNumber(1UL),
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 2UL)
        ]);
    }

    int _unpersistedWrites;

    [Fact] void should_not_have_persisted_the_debounced_progress_before_the_crash() => _unpersistedWrites.ShouldEqual(0);

    [Fact] void should_redeliver_and_handle_the_subscribed_event_after_recovery() => _subscriber
        .Received()
        .OnNext(
            Arg.Any<Key>(),
            Arg.Is<IEnumerable<AppendedEvent>>(events => events.Any(_ => _.Context.SequenceNumber == (EventSequenceNumber)2UL)),
            Arg.Any<ObserverSubscriberContext>());

    [Fact] void should_advance_past_the_reprocessed_range_without_skipping() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
}
