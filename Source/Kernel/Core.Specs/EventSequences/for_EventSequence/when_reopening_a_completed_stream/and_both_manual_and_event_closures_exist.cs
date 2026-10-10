// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reopening_a_completed_stream;

public class and_both_manual_and_event_closures_exist : given.a_repairable_sequence
{
    async Task Establish()
    {
        await _closures.Close(new(_scope, ClosedStreamOwner.Manual, EventSequenceNumber.First, DateTimeOffset.UtcNow));
        await _closures.Close(new(_scope, new ClosedStreamOwner("closing"), EventSequenceNumber.First, DateTimeOffset.UtcNow));
    }

    async Task Because() => _result = await _eventSequence.ReopenCompletedStream(_scope, "Repair", CorrelationId.NotSet, [], _actor);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_preserve_the_event_closure() => (await _closures.GetAll()).Single().Owner.ShouldEqual(new ClosedStreamOwner("closing"));
}
