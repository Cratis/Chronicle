// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_appending_many_and_storage_reports_duplicate_sequence_numbers : given.an_event_sequence
{
    const int SafetyCap = 20;
    static readonly EventSequenceNumber _staleSequenceNumber = 5UL;
    static readonly EventSequenceNumber _nextAvailableSequenceNumber = 10UL;
    readonly HashSet<ulong> _usedSequenceNumbers = [5, 6, 7, 8, 9];

    int _callCount;
    bool _safetyCapReached;
    IReadOnlyList<ulong> _lastSubmittedSequenceNumbers = [];
    AppendManyResult _result;

    void Establish()
    {
        // The grain reactivated with a stale sequence number (its advanced state never got persisted before a
        // crash), so the first submission collides with numbers already present in storage.
        _stateStorage.State.SequenceNumber = _staleSequenceNumber;

        _eventSequenceStorage
            .AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(callInfo =>
            {
                _callCount++;
                _lastSubmittedSequenceNumbers = callInfo.Arg<IEnumerable<EventToAppendToStorage>>()
                    .Select(_ => _.SequenceNumber.Value)
                    .ToArray();

                // Safety valve: without the re-numbering fix the same colliding numbers are resubmitted forever;
                // this bounds that regression to a failed assertion instead of an infinite loop / hung test.
                if (_callCount >= SafetyCap)
                {
                    _safetyCapReached = true;
                    return Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success([]);
                }

                return _lastSubmittedSequenceNumbers.Any(_usedSequenceNumbers.Contains)
                    ? (Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>)new DuplicateEventSequenceNumber(_nextAvailableSequenceNumber)
                    : Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success([]);
            });
    }

    async Task Because() => _result = await _grain.AppendManyToStorage(
        [ValidatedEvent(), ValidatedEvent()],
        Cratis.Execution.CorrelationId.New(),
        [],
        []);

    [Fact] void should_terminate_before_the_safety_cap() => _safetyCapReached.ShouldBeFalse();
    [Fact] void should_resubmit_with_non_colliding_sequence_numbers() => _lastSubmittedSequenceNumbers.Any(_usedSequenceNumbers.Contains).ShouldBeFalse();
    [Fact] void should_have_succeeded() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_advance_next_sequence_number_past_the_renumbered_batch() =>
        (await _grain.GetNextSequenceNumber()).ShouldEqual((EventSequenceNumber)12UL);
}
