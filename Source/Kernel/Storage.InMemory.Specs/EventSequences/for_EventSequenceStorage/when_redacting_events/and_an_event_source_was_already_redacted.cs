// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_redacting_events;

public class and_an_event_source_was_already_redacted : given.a_storage_with_appended_events
{
    IEnumerable<EventType> _affectedEventTypes;

    async Task Establish() => await _storage.Redact(_firstEventSourceId, "first reason", null, CorrelationId.New(), [], [], DateTimeOffset.UtcNow);

    async Task Because() => _affectedEventTypes = await _storage.Redact(_firstEventSourceId, "second reason", null, CorrelationId.New(), [], [], DateTimeOffset.UtcNow);

    [Fact] void should_report_no_affected_event_types() => _affectedEventTypes.ShouldBeEmpty();
    [Fact] void should_keep_the_first_redaction_reason() => ((IDictionary<string, object?>)_storage.Events.Single(_ => _.Context.SequenceNumber == (EventSequenceNumber)0UL).Content)["reason"].ShouldEqual("first reason");
}
