// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing_an_isolated_rebuild;

public class and_the_range_is_empty : given.a_performing_isolated_rebuild
{
    void Establish()
    {
        _performState.EndEventSequenceNumber = EventSequenceNumber.Max;
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)50UL);
        _eventCursor.Current.Returns(Array.Empty<AppendedEvent>());
        _eventCursor.MoveNext().Returns(Task.FromResult(false));
        // A live write at 51 is newer than the captured range, even though the partition had no events.
        _rebuilder.Publish((EventSequenceNumber)50UL).Returns(false);
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_publish_only_through_the_captured_tail() => _rebuilder.Received(1).Publish((EventSequenceNumber)50UL);
    [Fact] void should_never_publish_a_sentinel() => _rebuilder.DidNotReceive().Publish(Arg.Is<EventSequenceNumber>(_ => !_.IsActualValue));
    [Fact] void should_not_claim_to_have_replaced_the_newer_document() => StepResult.TryGetError(out _).ShouldBeTrue();
}
