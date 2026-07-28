// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

public class when_redacting_a_single_event : given.a_storage_with_appended_events
{
    AppendedEvent _returned;
    AppendedEvent _stored;

    async Task Because()
    {
        _returned = await _storage.Redact(1, "no longer needed", CorrelationId.New(), [], [], DateTimeOffset.UtcNow);
        _stored = _storage.Events.Single(_ => _.Context.SequenceNumber == (EventSequenceNumber)1UL);
    }

    [Fact] void should_return_the_event_as_it_was_before_redaction() => _returned.Context.EventType.Id.ShouldEqual(_eventType.Id);
    [Fact] void should_replace_the_stored_event_type_with_the_redaction_marker() => _stored.Context.EventType.Id.ShouldEqual(GlobalEventTypes.Redaction);
    [Fact] void should_keep_the_stored_event_at_its_sequence_number() => _stored.Context.SequenceNumber.ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_keep_the_stored_event_on_its_event_source() => _stored.Context.EventSourceId.ShouldEqual(_secondEventSourceId);
    [Fact] void should_replace_the_stored_payload_with_the_redaction_reason() => ((IDictionary<string, object?>)_stored.Content)["reason"].ShouldEqual("no longer needed");
    [Fact] void should_leave_other_events_untouched() => _storage.Events.Count(_ => _.Context.EventType.Id == GlobalEventTypes.Redaction).ShouldEqual(1);
}
