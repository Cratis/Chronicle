// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_under_a_namespace_fence;

public class and_a_single_event_slot_is_occupied : given.a_fenced_append_storage
{
    Result<AppendedEvent, DuplicateEventSequenceNumber> _outcome;

    void Establish()
    {
        _writeFailure = Duplicate();
        _slotOccupied = true;
        _duringWrite = () => _foundEvents = _inserted.Select(item => item with { SequenceNumber = 99 }).ToArray();
    }

    async Task Because() => _outcome = await _storage.Append(_context, _event);

    [Fact] void should_report_the_next_available_slot() => _outcome.AsT1.NextAvailableSequenceNumber.Value.ShouldEqual(100UL);
    [Fact] void should_fence_the_conflict_reads() => _steps.SequenceEqual(["fence", "insert", "abort", "fence", "commit"]).ShouldBeTrue();
    [Fact] void should_read_both_the_collision_and_tail_on_the_recovery_session() => _lookupSessions.TrueForAll(session => session == _sessions[1]).ShouldBeTrue();
    [Fact] void should_read_the_collision_and_tail() => _lookupSessions.Count.ShouldEqual(2);
}
