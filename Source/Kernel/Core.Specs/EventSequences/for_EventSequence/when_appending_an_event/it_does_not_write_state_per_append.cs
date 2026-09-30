// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class it_does_not_write_state_per_append : given.an_event_sequence
{
    async Task Establish()
    {
        await AppendAnEvent();
        _silo.StorageStats<EventSequence, EventSequenceState>().ResetCounts();
    }

    Task Because() => AppendAnEvent();

    [Fact] void should_not_write_state() => _silo.StorageStats<EventSequence, EventSequenceState>().Writes.ShouldEqual(0);
}
