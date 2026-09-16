// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_storage_returns_an_incomplete_acknowledgment : given.an_event_sequence
{
    Exception _error;

    void Establish() => _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
        .Returns(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success([]));

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _eventSequence.AppendManyToStorage([ValidatedEvent()], CorrelationId.NotSet, [], []));

    [Fact] void should_not_report_a_successful_batch_with_missing_receipts() => _error.ShouldBeOfExactType<InvalidAppendAcknowledgment>();
    [Fact] void should_not_update_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
}
