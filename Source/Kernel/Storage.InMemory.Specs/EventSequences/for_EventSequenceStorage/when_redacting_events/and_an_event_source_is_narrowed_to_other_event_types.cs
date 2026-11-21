// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_redacting_events;

public class and_an_event_source_is_narrowed_to_other_event_types : given.a_storage_with_appended_events
{
    IEnumerable<EventType> _affectedEventTypes;

    async Task Because() =>
        _affectedEventTypes = await _storage.Redact(
            _firstEventSourceId,
            "erasure",
            [new EventType("11111111-2222-3333-4444-555555555555", EventTypeGeneration.First)],
            CorrelationId.New(),
            [],
            [],
            DateTimeOffset.UtcNow);

    [Fact] void should_report_no_affected_event_types() => _affectedEventTypes.ShouldBeEmpty();
    [Fact] void should_not_redact_any_event() => _storage.Events.Count(_ => _.Context.EventType.Id == GlobalEventTypes.Redaction).ShouldEqual(0);
}
