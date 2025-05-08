// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootMutation.when_applying;

public class a_valid_event_and_auto_commit_not_enabled : given.an_aggregate_mutation
{
    [EventType]
    class SomeEvent;

    SomeEvent _event;
    Causation _causationResult;

    void Establish()
    {
        _event = new();
        _unitOfWork
            .When(_ => _.AddEvent(
                _eventSequenceId,
                _eventSourceId,
                _event,
                Arg.Any<Causation>(),
                _eventStreamType,
                _eventStreamId,
                _eventSourceType))
            .Do(callInfo => _causationResult = callInfo.Arg<Causation>());
    }

    Task Because() => _mutation.Apply(_event);

    [Fact] void should_call_mutator() => _mutator.Received().Mutate(Arg.Any<SomeEvent>());
    [Fact] void should_have_the_event_in_uncommitted_events() => _mutation.UncommittedEvents.ShouldContainOnly(_event);
    [Fact] void should_not_auto_commit() => _mutator.DidNotReceive().Dehydrate();
    [Fact] void should_add_to_unit_of_work() => _unitOfWork.Received(1).AddEvent(_eventSequenceId, _eventSourceId, _event, Arg.Any<Causation>(), _eventStreamType, _eventStreamId, _eventSourceType);
    [Fact] void should_set_aggregate_root_type_causation() => _causationResult.Properties[AggregateRootMutation.CausationAggregateRootTypeProperty].ShouldEqual(_aggregateRoot.GetType().AssemblyQualifiedName);
    [Fact] void should_set_event_sequence_id_causation() => _causationResult.Properties[AggregateRootMutation.CausationEventSequenceIdProperty].ShouldEqual(_eventSequenceId.Value);
}
