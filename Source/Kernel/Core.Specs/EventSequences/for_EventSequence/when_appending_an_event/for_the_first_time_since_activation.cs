// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

/// <summary>
/// The persisted state is what lists a sequence among its namespace's sequences, so a sequence holding events must not
/// wait for the persistence interval or a deactivation before it is listed.
/// </summary>
public class for_the_first_time_since_activation : given.an_event_sequence
{
    void Establish() => _silo.StorageStats<EventSequence, EventSequenceState>().ResetCounts();

    Task Because() => AppendAnEvent();

    [Fact] void should_write_state_once() => _silo.StorageStats<EventSequence, EventSequenceState>().Writes.ShouldEqual(1);
}
