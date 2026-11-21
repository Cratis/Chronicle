// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_tracking_last_seen_at;

public class with_existing_observers : given.an_observer_state_storage
{
    static readonly DateTimeOffset _seen = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset _started = new(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);

    IReadOnlyDictionary<ObserverId, DateTimeOffset> _first;
    IReadOnlyDictionary<ObserverId, DateTimeOffset> _second;
    IReadOnlyDictionary<ObserverId, DateTimeOffset> _renamed;
    bool _savedForOtherOwner;

    async Task Establish()
    {
        await _storage.Save(new KernelObserverState { Identifier = "seen" }, EventSequenceId.Log);
        await _storage.Save(new KernelObserverState { Identifier = "never-seen" }, EventSequenceId.Log);
        await _storage.SaveLastSeenAt("seen", _seen, EventSequenceId.Log);
        await _storage.Save(new KernelObserverState { Identifier = "to-rename" }, EventSequenceId.Log);
        await _storage.SaveLastSeenAt("to-rename", _seen, EventSequenceId.Log);
    }

    async Task Because()
    {
        _savedForOtherOwner = await _storage.SaveLastSeenAt("seen", _started, EventSequenceId.Outbox);
        _first = await _storage.GetLastSeenAt(["seen", "never-seen", "no-record"], _started);
        _second = await _storage.GetLastSeenAt(["seen", "never-seen"], _started.AddDays(3));

        // A save of the observer's own state must not clear the side field.
        await _storage.Save(new KernelObserverState { Identifier = "seen", RunningState = ObserverRunningState.Active }, EventSequenceId.Log);
        await _storage.Rename("to-rename", "renamed");
        _renamed = await _storage.GetLastSeenAt(["renamed"], _started);
    }

    [Fact] void should_not_save_for_an_observer_owned_by_another_sequence() => _savedForOtherOwner.ShouldBeFalse();
    [Fact] void should_keep_the_recorded_time() => _first["seen"].ShouldEqual(_seen);
    [Fact] void should_start_the_clock_for_an_observer_without_a_time() => _first["never-seen"].ShouldEqual(_started);
    [Fact] void should_not_report_an_observer_without_a_record() => _first.ContainsKey("no-record").ShouldBeFalse();
    [Fact] void should_keep_the_earliest_time_when_asked_again() => _second["never-seen"].ShouldEqual(_started);
    [Fact] async Task should_keep_the_time_when_the_state_is_saved() => (await _storage.GetLastSeenAt(["seen"], _started))["seen"].ShouldEqual(_seen);
    [Fact] void should_keep_the_time_when_the_observer_is_renamed() => _renamed["renamed"].ShouldEqual(_seen);
}
