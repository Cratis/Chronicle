// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_constraint_updates_precede_the_snapshot : given.an_event_sequence_with_a_failing_index
{
    int _snapshotsAtUpdate;
    int _deliveriesAtUpdate;
    void Establish() => _update = _ =>
    {
        _snapshotsAtUpdate = _silo.StorageStats<EventSequence, EventSequenceState>().Writes;
        _deliveriesAtUpdate = _appendedEventsQueues.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IAppendedEventsQueues.Enqueue));
        return Task.CompletedTask;
    };
    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_deliver_before_updating() => _deliveriesAtUpdate.ShouldEqual(1);
    [Fact] void should_not_snapshot_before_updating() => _snapshotsAtUpdate.ShouldEqual(0);
    [Fact] void should_snapshot_after_updating() => _silo.StorageStats<EventSequence, EventSequenceState>().Writes.ShouldEqual(1);
}
