// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_upgrading_a_sequence_with_an_event_redacted_by_an_older_node : for_EventSequenceStorage.given.a_storage_with_stored_generations
{
    string? _revisionsBefore;
    EventEntry _stored;

    async Task Establish()
    {
        await ReviseWithPersonalData();
        await RedactAsAnOlderNode();
        _revisionsBefore = Stored(0).Revisions;
        _migrator.ClearMigrationCache(_connectionString);
    }

    async Task Because()
    {
        await using (var context = CreateContext())
        {
            await context.EnsureTableExists();
        }

        _stored = Stored(0);
    }

    [Fact] void should_have_held_the_revision_before_the_upgrade() => _revisionsBefore.ShouldNotBeNull();
    [Fact] void should_remove_the_revisions_from_the_redacted_row() => _stored.Revisions.ShouldBeNull();
    [Fact] void should_keep_the_row_redacted() => _stored.Type.ShouldEqual(GlobalEventTypes.Redaction);
}
