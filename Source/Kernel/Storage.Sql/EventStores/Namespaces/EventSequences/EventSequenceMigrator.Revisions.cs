// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceMigrator
{
    async Task EnsureRevisionHistoryColumns(EventSequenceDbContext context, string tableName)
    {
        foreach (var column in new[] { nameof(EventEntry.OriginalContent), nameof(EventEntry.Revisions) })
        {
            if (await tableMigrator.ColumnExists(context, tableName, column)) continue;
            var migration = new MigrationBuilder(context.Database.ProviderName);
            migration.AddColumn<string>(column, tableName, nullable: true);
            await tableMigrator.ExecuteMigrationOperations(context, migration);
        }
    }

    async Task ClearRevisionsOfRedactedEvents(EventSequenceDbContext context, string tableName)
    {
        if (!await tableMigrator.ColumnExists(context, tableName, nameof(EventEntry.Type))) return;
        await context.Events
            .Where(entry => entry.Type == GlobalEventTypes.Redaction && (entry.OriginalContent != null || entry.Revisions != null))
            .ExecuteUpdateAsync(setters => setters.SetProperty(entry => entry.OriginalContent, (string?)null).SetProperty(entry => entry.Revisions, (string?)null));
    }
}
