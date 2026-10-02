// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_a_duplicate_sequence_number_exists_in_the_batch : given.an_event_sequence_storage
{
    Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber> _result;
    DuplicateEventSequenceNumber _duplicate;

    async Task Because()
    {
        _result = await _storage.AppendMany([EventAt(EventSequenceNumber.First), EventAt(EventSequenceNumber.First)]);
        _result.TryGetError(out _duplicate);
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_surface_the_duplicate_sequence_number() => _duplicate.ShouldNotBeNull();
    [Fact] void should_preserve_an_empty_log() => _storage.Events.ShouldBeEmpty();

    EventToAppendToStorage EventAt(EventSequenceNumber sequenceNumber) => new(
        sequenceNumber,
        EventSourceType.Default,
        "source",
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        CorrelationId.New(),
        [],
        [],
        [],
        DateTimeOffset.UtcNow,
        new ExpandoObject(),
        EventHash.NotSet);
}
