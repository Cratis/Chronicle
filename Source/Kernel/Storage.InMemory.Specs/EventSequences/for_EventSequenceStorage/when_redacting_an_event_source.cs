// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

public class when_redacting_an_event_source : given.a_storage_with_appended_events
{
    IEnumerable<EventType> _affectedEventTypes;

    async Task Because() =>
        _affectedEventTypes = await _storage.Redact(_firstEventSourceId, "erasure", null, CorrelationId.New(), [], [], DateTimeOffset.UtcNow);

    [Fact] void should_report_the_redacted_event_type_as_affected() => _affectedEventTypes.Select(_ => _.Id).ShouldEqual([_eventType.Id]);
    [Fact] void should_redact_every_event_on_that_event_source() => _storage.Events.Count(_ => _.Context.EventSourceId == _firstEventSourceId && _.Context.EventType.Id == GlobalEventTypes.Redaction).ShouldEqual(2);
    [Fact] void should_not_redact_the_other_event_source() => _storage.Events.Single(_ => _.Context.EventSourceId == _secondEventSourceId).Context.EventType.Id.ShouldEqual(_eventType.Id);
}
