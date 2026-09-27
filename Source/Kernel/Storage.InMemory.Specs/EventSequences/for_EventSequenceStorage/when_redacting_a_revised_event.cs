// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

public class when_redacting_a_revised_event : given.a_storage_with_appended_events
{
    const string Secret = "sensitive-revised-payload";
    AppendedEvent _stored;

    async Task Establish()
    {
        var revised = new ExpandoObject();
        ((IDictionary<string, object?>)revised)["secret"] = Secret;
        await _storage.Revise(1, _eventType, CorrelationId.New(), [], [], DateTimeOffset.UtcNow, revised, new EventHash("original-payload-hash"));
    }

    async Task Because()
    {
        await _storage.Redact(1, "contains sensitive data", CorrelationId.New(), [], [], DateTimeOffset.UtcNow);
        _stored = _storage.Events.Single(_ => _.Context.SequenceNumber == (EventSequenceNumber)1UL);
    }

    [Fact] void should_clear_the_revision_history() => _stored.Revisions.ShouldBeEmpty();
    [Fact] void should_clear_the_original_content() => _stored.OriginalContent.ShouldBeEmpty();
    [Fact] void should_clear_the_content_hash() => _stored.Context.Hash.ShouldEqual(EventHash.NotSet);
    [Fact] void should_not_retain_the_revised_payload() => System.Text.Json.JsonSerializer.Serialize(_stored).ShouldNotContain(Secret);
}
