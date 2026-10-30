// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_redacting;

public class and_an_older_node_already_redacted_a_revised_event : given.a_storage_with_stored_generations
{
    string? _revisionsBefore;
    AppendedEvent _result;
    EventEntry _stored;

    async Task Establish()
    {
        await ReviseWithPersonalData();
        await RedactAsAnOlderNode();
        _revisionsBefore = Stored(0).Revisions;
    }

    async Task Because()
    {
        _result = await _storage.Redact(0, "reason", CorrelationId.NotSet, [], [], DateTimeOffset.UtcNow);
        _stored = Stored(0);
    }

    [Fact] void should_have_held_the_revision_before_redacting() => _revisionsBefore.ShouldNotBeNull();
    [Fact] void should_remove_the_revisions() => _stored.Revisions.ShouldBeNull();
    [Fact] void should_keep_the_row_redacted() => _stored.Type.ShouldEqual(GlobalEventTypes.Redaction);
    [Fact] void should_report_the_redaction_as_already_applied() => _result.Context.EventType.Id.ShouldEqual(GlobalEventTypes.Redaction);
}
