// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_redacting_events;

public class and_the_event_is_already_redacted : given.a_storage_with_appended_events
{
    AppendedEvent _returned;
    AppendedEvent _stored;

    async Task Establish() => await _storage.Redact(1, "first reason", CorrelationId.New(), [], [], DateTimeOffset.UtcNow);

    async Task Because()
    {
        _returned = await _storage.Redact(1, "second reason", CorrelationId.New(), [], [], DateTimeOffset.UtcNow);
        _stored = _storage.Events.Single(_ => _.Context.SequenceNumber == (EventSequenceNumber)1UL);
    }

    [Fact] void should_return_the_already_redacted_event() => _returned.Context.EventType.Id.ShouldEqual(GlobalEventTypes.Redaction);
    [Fact] void should_keep_the_first_redaction_reason() => ((IDictionary<string, object?>)_stored.Content)["reason"].ShouldEqual("first reason");
}
