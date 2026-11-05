// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_redacting;

public class by_event_source_when_already_redacted_with_leftover_history : given.an_event_with_revision_history
{
    IEnumerable<EventType> _result;

    async Task Establish()
    {
        await Revise();
        await Read();
        await _storage.Redact(new EventSourceId(_entry.EventSourceId), "reason", [], CorrelationId.New(), [], [], DateTimeOffset.UtcNow);
        await using var context = CreateContext();
        var entry = await context.Events.SingleAsync();
        entry.OriginalContent = "{\"format\":1}";
        entry.Revisions = "[]";
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        _result = await _storage.Redact(new EventSourceId(_entry.EventSourceId), "reason", [], CorrelationId.New(), [], [], DateTimeOffset.UtcNow);
        await Read();
    }

    [Fact] void should_not_report_affected_event_types() => _result.ShouldBeEmpty();
    [Fact] void should_clear_the_original_envelope() => _entry.OriginalContent.ShouldBeNull();
    [Fact] void should_clear_the_revision_array() => _entry.Revisions.ShouldBeNull();
}
