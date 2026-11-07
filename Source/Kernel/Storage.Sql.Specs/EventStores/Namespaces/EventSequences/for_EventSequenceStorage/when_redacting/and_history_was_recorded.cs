// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_redacting;

public class and_history_was_recorded : given.an_event_with_revision_history
{
    async Task Establish() => await Revise();
    async Task Because()
    {
        await _storage.Redact(EventSequenceNumber.First, "reason", CorrelationId.New(), [], [], DateTimeOffset.UtcNow);
        await Revise("ignored");
        await Read();
    }

    [Fact] void should_clear_the_original_envelope() => _entry.OriginalContent.ShouldBeNull();
    [Fact] void should_clear_the_revision_array() => _entry.Revisions.ShouldBeNull();
    [Fact] void should_not_record_a_revision_of_the_redaction() => _read.Revisions.Count().ShouldEqual(0);
}
