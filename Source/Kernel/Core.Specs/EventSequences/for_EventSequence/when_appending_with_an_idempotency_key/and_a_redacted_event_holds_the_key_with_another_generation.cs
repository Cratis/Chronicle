// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_a_redacted_event_holds_the_key_with_another_generation : given.an_event_sequence_with_idempotency_keys
{
    void Establish()
    {
        Durable();
        _request = _request with { EventType = new(_eventType.Id, 2) };
        _eventSequenceStorage.GetEventAt(Arg.Any<EventSequenceNumber>()).Throws(new Exception("A redacted row must not supply the original type"));
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_report_the_original_type() => _result.Conflicts.Single().StoredEventType.ShouldEqual(_eventType);
    [Fact] async Task should_not_read_the_redacted_row() => await _eventSequenceStorage.DidNotReceive().GetEventAt(Arg.Any<EventSequenceNumber>());
}
