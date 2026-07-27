// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_appending_many_and_storage_throws_a_non_duplicate_error : given.an_event_sequence
{
    static readonly EventSequenceNumber _nextSequenceNumber = 5UL;
    static readonly EventSequenceNumber _existingTail = 4UL;
    Exception _exception;

    void Establish()
    {
        _stateStorage.State.SequenceNumber = _nextSequenceNumber;
        _eventSequenceStorage
            .AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .ThrowsAsync(new given.SimulatedStorageError());
    }

    async Task Because() => _exception = await Catch.Exception(() => _eventSequence.AppendManyToStorage(
        [ValidatedEvent()],
        Cratis.Execution.CorrelationId.New(),
        [],
        []));

    [Fact] void should_fail_with_the_storage_error() => _exception.ShouldBeOfExactType<given.SimulatedStorageError>();
    [Fact] async Task should_not_advance_the_next_sequence_number() =>
        (await _eventSequence.GetNextSequenceNumber()).ShouldEqual(_nextSequenceNumber);
    [Fact] async Task should_not_advance_the_tail_sequence_number() =>
        (await _eventSequence.GetTailSequenceNumber()).ShouldEqual(_existingTail);
}
