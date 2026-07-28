// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// An event sequence that snapshots its state on every append and fails every one of those writes. It carries the
/// batch helpers too, so the single-append and batch paths can be exercised from the same setup.
/// </summary>
public class an_event_sequence_that_cannot_persist_state : appending_many_events
{
    protected override int StatePersistenceInterval => 1;

    protected override async Task<EventSequence> CreateEventSequence() =>
        await _silo.CreateGrainAsync<EventSequenceThatCannotPersistState>(_eventSequenceKey.ToString());
}
