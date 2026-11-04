// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;
using Cratis.Chronicle.Storage.Projections;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents event-store-scoped projection definition history in MongoDB.
/// </summary>
/// <param name="eventStoreDatabase">The event store database.</param>
public class ProjectionDefinitionHistoryStorage(IEventStoreDatabase eventStoreDatabase) : IProjectionDefinitionHistoryStorage
{
    IMongoCollection<ProjectionDefinitionHistoryDocument> Collection => eventStoreDatabase.GetCollection<ProjectionDefinitionHistoryDocument>(WellKnownCollectionNames.ProjectionDefinitionHistory);

    /// <inheritdoc/>
    public async Task Record(ProjectionDefinitionHistoryEntry entry)
    {
        var document = new ProjectionDefinitionHistoryDocument
        {
            Id = new(entry.Projection, entry.Digest),
            DigestAlgorithm = entry.DigestAlgorithm,
            RegisteringVersion = entry.RegisteringVersion,
            RecordedAt = entry.RecordedAt,
            Definition = entry.Definition.ToMongoDB()
        };
        try
        {
            await Collection.InsertOneAsync(document);
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // The first registration owns both the content and its metadata.
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinitionHistoryEntry>> GetFor(ProjectionId projectionId)
    {
        using var cursor = await Collection.FindAsync(_ => _.Id.Projection == projectionId);
        var documents = await cursor.ToListAsync();
        return documents.Select(_ => new ProjectionDefinitionHistoryEntry(
                _.Id.Projection, _.Id.Digest, _.DigestAlgorithm, _.RegisteringVersion, _.RecordedAt, _.Definition.ToKernel()))
            .OrderBy(_ => _.RecordedAt)
            .ThenBy(_ => _.Digest.Value, StringComparer.Ordinal)
            .ToArray();
    }
}
