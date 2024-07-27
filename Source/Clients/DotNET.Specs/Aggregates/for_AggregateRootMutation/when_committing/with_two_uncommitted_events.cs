// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_AggregateRootMutation.when_committing;

public class with_two_uncommitted_events : given.an_aggregate_mutation
{
    [EventType]
    class SimpleEvent;

    IEnumerable<SimpleEvent> _events;
    SimpleEvent _firstEvent;
    SimpleEvent _secondEvent;
    AggregateRootCommitResult _result;
    IDictionary<string, string> _causations;

    async Task Establish()
    {
        _events =
        [
            _firstEvent = new SimpleEvent(),
            _secondEvent = new SimpleEvent()
        ];
        await _mutation.Apply(_firstEvent);
        await _mutation.Apply(_secondEvent);

        _causationManager
            .When(_ => _.Add(AggregateRootMutation.CausationType, Arg.Any<IDictionary<string, string>>()))
            .Do(_ => _causations = _.ArgAt<IDictionary<string, string>>(1));
    }

    async Task Because() => _result = await _mutation.Commit();

    [Fact] void should_return_a_successful_commit_result() => _result.Success.ShouldBeTrue();
    [Fact] void should_return_the_correct_events_in_the_commit_result() => _result.Events.ShouldContainOnly(_events);
    [Fact] void should_append_the_correct_events_to_the_event_sequence() => _eventSequence.Received(1).AppendMany(_eventSourceId, Arg.Is<IEnumerable<object>>(_ => _.ToList().SequenceEqual(_events)));
    [Fact] void should_add_causation_information_for_aggregate_root_type_to_the_causation_manager() => _causations.ShouldContain(kvp => kvp.Key == AggregateRootMutation.CausationAggregateRootTypeProperty && kvp.Value == _aggregateRoot.GetType().AssemblyQualifiedName);
    [Fact] void should_add_causation_information_for_event_sequence_to_the_causation_manager() => _causations.ShouldContain(kvp => kvp.Key == AggregateRootMutation.CausationEventSequenceIdProperty && kvp.Value == _eventSequenceId);
    [Fact] void should_clear_the_uncommitted_events() => _mutation.UncommittedEvents.ShouldBeEmpty();
}
