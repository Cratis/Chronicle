// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_revising;

public class and_redaction_wins_the_first_write : given.an_event_with_revision_history
{
    void Establish() => _interceptors = [new given.before_revision_write(async () => await _storage.Redact(EventSequenceNumber.First, "reason", CorrelationId.New(), [], [], DateTimeOffset.UtcNow))];
    async Task Because()
    {
        await Revise();
        await Read();
    }

    [Fact] void should_not_resurrect_the_redacted_event() => _read.Context.EventType.Id.ShouldEqual(GlobalEventTypes.Redaction);
    [Fact] void should_leave_the_original_null() => _entry.OriginalContent.ShouldBeNull();
    [Fact] void should_leave_the_revisions_null() => _entry.Revisions.ShouldBeNull();
    [Fact] void should_not_persist_a_revision() => _read.Revisions.Count().ShouldEqual(0);
}
