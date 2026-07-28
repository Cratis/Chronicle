// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class it_updates_each_constraint_index_with_its_own_appended_sequence_number : given.appending_many_events
{
    AppendManyResult _result;

    void Establish() =>
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(callInfo =>
            {
                var events = callInfo.Arg<IEnumerable<EventToAppendToStorage>>();
                return Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom(events)));
            });

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events,
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_update_the_constraint_index_once_per_event() => _constraintIndexSequenceNumbers.Count.ShouldEqual(3);
    [Fact] void should_index_the_first_event_with_its_own_number() => _constraintIndexSequenceNumbers[0].ShouldEqual((EventSequenceNumber)0);
    [Fact] void should_index_the_second_event_with_its_own_number() => _constraintIndexSequenceNumbers[1].ShouldEqual((EventSequenceNumber)1);
    [Fact] void should_index_the_third_event_with_its_own_number() => _constraintIndexSequenceNumbers[2].ShouldEqual((EventSequenceNumber)2);
}
