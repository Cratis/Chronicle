// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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

        _unitOfWork.GetEvents().Returns(_events.ToImmutableList<object>());

        _causationManager
            .When(_ => _.Add(AggregateRootMutation.CausationType, Arg.Any<IDictionary<string, string>>()))
            .Do(_ => _causations = _.ArgAt<IDictionary<string, string>>(1));
    }

    async Task Because() => _result = await _mutation.Commit();

    [Fact] void should_return_a_successful_commit_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_the_correct_events_in_the_commit_result() => _result.Events.ShouldContainOnly(_events);
    [Fact] void should_commit_the_unit_of_work() => _unitOfWork.Received(1).Commit();
    [Fact] void should_clear_the_uncommitted_events() => _mutation.UncommittedEvents.ShouldBeEmpty();
}
