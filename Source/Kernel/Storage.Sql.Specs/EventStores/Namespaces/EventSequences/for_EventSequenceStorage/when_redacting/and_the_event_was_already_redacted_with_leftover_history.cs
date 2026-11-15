// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_redacting;

public class and_the_event_was_already_redacted_with_leftover_history : given.an_event_with_revision_history
{
    AppendedEvent _result;

    async Task Establish()
    {
        await Revise();
        await _storage.Redact(EventSequenceNumber.First, "reason", CorrelationId.New(), [], [], DateTimeOffset.UtcNow);
        await using var context = CreateContext();
        var entry = await context.Events.SingleAsync();
        entry.OriginalContent = "{\"format\":1}";
        entry.Revisions = "[]";
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        _result = await _storage.Redact(EventSequenceNumber.First, "reason", CorrelationId.New(), [], [], DateTimeOffset.UtcNow);
        await Read();
    }

    [Fact] void should_still_report_the_redaction() => _result.Context.EventType.Id.ShouldEqual(GlobalEventTypes.Redaction);
    [Fact] void should_clear_the_original_envelope() => _entry.OriginalContent.ShouldBeNull();
    [Fact] void should_clear_the_revision_array() => _entry.Revisions.ShouldBeNull();
}
