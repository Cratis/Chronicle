// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections;

/// <summary>
/// Represents event-store-scoped projection definition history in SQL.
/// </summary>
/// <param name="eventStore">The event store name.</param>
/// <param name="database">The database.</param>
public class ProjectionDefinitionHistoryStorage(EventStoreName eventStore, IDatabase database) : IProjectionDefinitionHistoryStorage
{
    /// <inheritdoc/>
    public async Task Record(ProjectionDefinitionHistoryEntry entry)
    {
        var definition = entry.Definition.ToSql();
        await using var scope = await database.EventStore(eventStore);
        scope.DbContext.ProjectionDefinitionHistory.Add(new()
        {
            ProjectionId = entry.Projection,
            Digest = entry.Digest,
            DigestAlgorithm = entry.DigestAlgorithm,
            RegisteringVersion = entry.RegisteringVersion,
            RecordedAt = entry.RecordedAt,
            Definitions = definition.Definitions
        });
        try
        {
            await scope.DbContext.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (error.IsUniqueViolation())
        {
            // The first registration owns both the content and its metadata.
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinitionHistoryEntry>> GetFor(ProjectionId projectionId)
    {
        await using var scope = await database.EventStore(eventStore);
        var rows = await scope.DbContext.ProjectionDefinitionHistory.AsNoTracking().Where(_ => _.ProjectionId == projectionId).ToArrayAsync();
        return rows.Select(_ => new ProjectionDefinitionHistoryEntry(
                _.ProjectionId,
                _.Digest,
                _.DigestAlgorithm,
                _.RegisteringVersion,
                _.RecordedAt,
                new Projection { Id = _.ProjectionId, Definitions = _.Definitions }.ToKernel()))
            .OrderBy(_ => _.RecordedAt)
            .ThenBy(_ => _.Digest.Value, StringComparer.Ordinal)
            .ToArray();
    }
}
