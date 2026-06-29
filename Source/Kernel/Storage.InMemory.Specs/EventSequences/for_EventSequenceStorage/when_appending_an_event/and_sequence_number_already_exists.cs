// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_an_event;

public class and_sequence_number_already_exists : given.an_event_sequence_storage
{
    static readonly EventSourceId _eventSourceId = "some-source";
    Result<AppendedEvent, DuplicateEventSequenceNumber> _result;

    void Establish() => Append(EventSequenceNumber.First, _eventSourceId).GetAwaiter().GetResult();

    async Task Because() => _result = await Append(EventSequenceNumber.First, _eventSourceId);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_a_duplicate_event_sequence_number() => _result.AsT1.ShouldNotBeNull();
}
