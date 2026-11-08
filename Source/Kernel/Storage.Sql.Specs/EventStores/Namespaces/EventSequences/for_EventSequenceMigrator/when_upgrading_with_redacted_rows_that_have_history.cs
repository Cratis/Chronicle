// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_upgrading_with_redacted_rows_that_have_history : given.a_named_tag_migrator
{
    EventEntry[] _stored;

    async Task Establish()
    {
        await using var context = CreateContext();
        await context.EnsureTableExists();
        context.Events.AddRange(new EventEntry { SequenceNumber = 1, Type = GlobalEventTypes.Redaction }, new EventEntry { SequenceNumber = 2, Type = "ordinary" });
        await context.SaveChangesAsync();
        await context.Events.ExecuteUpdateAsync(setters => setters.SetProperty(entry => entry.OriginalContent, "original").SetProperty(entry => entry.Revisions, "revisions"));
    }

    async Task Because()
    {
        new TableMigrator<EventSequenceDbContext>(Substitute.For<ILogger<TableMigrator<EventSequenceDbContext>>>()).ClearMigrationCacheForConnectionString(_connection.ConnectionString);
        await using var context = CreateContext();
        await context.EnsureTableExists();
        await context.EnsureTableExists();
        _stored = await context.Events.AsNoTracking().OrderBy(entry => entry.SequenceNumber).ToArrayAsync();
    }

    [Fact] void should_clear_redacted_original_content() => _stored[0].OriginalContent.ShouldBeNull();
    [Fact] void should_clear_redacted_revisions() => _stored[0].Revisions.ShouldBeNull();
    [Fact] void should_keep_ordinary_original_content() => _stored[1].OriginalContent.ShouldEqual("original");
    [Fact] void should_keep_ordinary_revisions() => _stored[1].Revisions.ShouldEqual("revisions");
}
