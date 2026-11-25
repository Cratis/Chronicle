// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStateStorage;

public class when_tracking_last_seen_at : Specification
{
    static readonly DateTimeOffset _seen = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset _started = new(2026, 11, 19, 10, 0, 0, TimeSpan.Zero);

    ObserverStateStorage _storage;
    IReadOnlyDictionary<ObserverId, DateTimeOffset> _first;
    IReadOnlyDictionary<ObserverId, DateTimeOffset> _second;
    IReadOnlyDictionary<ObserverId, DateTimeOffset> _deleted;
    bool _savedForOtherOwner;

    async Task Establish()
    {
        _storage = new();
        await _storage.Save(new ObserverState { Identifier = "seen" }, EventSequenceId.Log);
        await _storage.Save(new ObserverState { Identifier = "never-seen" }, EventSequenceId.Log);
        await _storage.SaveLastSeenAt("seen", _seen, EventSequenceId.Log);
        await _storage.Save(new ObserverState { Identifier = "to-delete" }, EventSequenceId.Log);
        await _storage.SaveLastSeenAt("to-delete", _seen, EventSequenceId.Log);
    }

    async Task Because()
    {
        _savedForOtherOwner = await _storage.SaveLastSeenAt("seen", _started, EventSequenceId.Outbox);
        _first = await _storage.GetLastSeenAt(["seen", "never-seen", "no-record"], _started);
        _second = await _storage.GetLastSeenAt(["seen", "never-seen"], _started.AddDays(3));
        await _storage.Save(new ObserverState { Identifier = "seen", RunningState = ObserverRunningState.Active }, EventSequenceId.Log);
        await _storage.Delete("to-delete");
        _deleted = await _storage.GetLastSeenAt(["to-delete"], _started);
    }

    [Fact] void should_not_save_for_an_observer_owned_by_another_sequence() => _savedForOtherOwner.ShouldBeFalse();
    [Fact] void should_keep_the_recorded_time() => _first["seen"].ShouldEqual(_seen);
    [Fact] void should_start_the_clock_for_an_observer_without_a_time() => _first["never-seen"].ShouldEqual(_started);
    [Fact] void should_not_report_an_observer_without_a_record() => _first.ContainsKey("no-record").ShouldBeFalse();
    [Fact] void should_keep_the_earliest_time_when_asked_again() => _second["never-seen"].ShouldEqual(_started);
    [Fact] async Task should_keep_the_time_when_the_state_is_saved() => (await _storage.GetLastSeenAt(["seen"], _started))["seen"].ShouldEqual(_seen);
    [Fact] void should_forget_the_time_when_the_state_is_deleted() => _deleted.ShouldBeEmpty();
}
