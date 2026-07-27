// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_a_duplicate_sequence_number_is_encountered : given.appending_many_events
{
    static readonly EventSequenceNumber _nextAvailableSequenceNumber = 5;

    readonly List<IReadOnlyList<EventSequenceNumber>> _sequenceNumbersPerAttempt = [];
    AppendManyResult _result;

    void Establish()
    {
        var attempt = 0;
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(callInfo =>
            {
                var events = callInfo.Arg<IEnumerable<EventToAppendToStorage>>().ToArray();
                _sequenceNumbersPerAttempt.Add(events.Select(_ => _.SequenceNumber).ToArray());
                attempt++;
                return attempt == 1
                    ? Task.FromResult<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>>(new DuplicateEventSequenceNumber(_nextAvailableSequenceNumber))
                    : Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom(events)));
            });
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events,
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_retry_the_batch_once() => _sequenceNumbersPerAttempt.Count.ShouldEqual(2);
    [Fact] void should_first_attempt_with_the_original_numbers() => _sequenceNumbersPerAttempt[0].ShouldContainOnly((EventSequenceNumber)0, (EventSequenceNumber)1, (EventSequenceNumber)2);
    [Fact] void should_renumber_the_first_event_from_the_next_available_number() => _sequenceNumbersPerAttempt[1][0].ShouldEqual(_nextAvailableSequenceNumber);
    [Fact] void should_renumber_the_second_event_contiguously() => _sequenceNumbersPerAttempt[1][1].ShouldEqual((EventSequenceNumber)6);
    [Fact] void should_renumber_the_third_event_contiguously() => _sequenceNumbersPerAttempt[1][2].ShouldEqual((EventSequenceNumber)7);
    [Fact] void should_report_the_renumbered_sequence_numbers() => _result.SequenceNumbers.ShouldContainOnly((EventSequenceNumber)5, (EventSequenceNumber)6, (EventSequenceNumber)7);
    [Fact] void should_update_each_constraint_index_with_its_renumbered_number() => _constraintIndexSequenceNumbers.ShouldContainOnly((EventSequenceNumber)5, (EventSequenceNumber)6, (EventSequenceNumber)7);
}
