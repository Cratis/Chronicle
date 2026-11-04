// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_using_a_reserved_name;

public class and_the_sequence_is_new_when_reading_a_tail : given.an_event_sequence_with_a_reserved_name
{
    EventSequenceNumber _next;
    EventSequenceNumber _tail;
    EventSequenceNumber _tailForTypes;
    bool _notFound;
    bool _completed;

    async Task Because()
    {
        await _eventSequence.Rehydrate();
        await _eventSequence.RefreshConstraints();
        await _eventSequence.ConstraintIndexRebuildStarting(null!, []);
        await _eventSequence.ConstraintIndexRebuilt(null!, [], null!);
        await _eventSequence.DrainAppends();
        _next = await _eventSequence.GetNextSequenceNumber();
        _tail = await _eventSequence.GetTailSequenceNumber();
        _tailForTypes = await _eventSequence.GetTailSequenceNumberForEventTypes([]);
        _notFound = !(await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(0, [], null)).IsSuccess;
        _completed = await _eventSequence.IsStreamCompleted(new ClosedStreamScope(EventSourceId: "source"));
        await _eventSequence.OnDeactivateAsync(new(DeactivationReasonCode.ShuttingDown, string.Empty), CancellationToken.None);
    }

    [Fact] void should_read_the_next_position() => _next.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_read_an_empty_tail() => _tail.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_read_an_empty_event_type_tail() => _tailForTypes.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_find_a_sequence_number() => _notFound.ShouldBeTrue();
    [Fact] void should_not_find_a_completed_stream() => _completed.ShouldBeFalse();
    [Fact] void should_not_open_sequence_storage() => _namespaceStorage.DidNotReceive().GetEventSequence(Arg.Any<EventSequenceId>());
    [Fact] void should_not_write_state() => StateWrites.ShouldEqual(0);
    [Fact] void should_not_prepare_constraints() => _constraintValidationFactory.DidNotReceive().Create(Arg.Any<EventSequenceKey>());
}
