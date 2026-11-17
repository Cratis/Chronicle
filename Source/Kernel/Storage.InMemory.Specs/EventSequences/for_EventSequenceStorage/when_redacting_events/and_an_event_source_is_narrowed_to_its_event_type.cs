// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_redacting_events;

public class and_an_event_source_is_narrowed_to_its_event_type : given.a_storage_with_appended_events
{
    IEnumerable<EventType> _affectedEventTypes;

    async Task Because() => _affectedEventTypes = await _storage.Redact(_firstEventSourceId, "erasure", [_eventType], CorrelationId.New(), [], [], DateTimeOffset.UtcNow);

    [Fact] void should_report_the_event_type_as_affected() => _affectedEventTypes.Select(_ => _.Id).ShouldEqual([_eventType.Id]);
    [Fact] void should_redact_the_events_of_that_event_source() => _storage.Events.Count(_ => _.Context.EventType.Id == GlobalEventTypes.Redaction).ShouldEqual(2);
}
