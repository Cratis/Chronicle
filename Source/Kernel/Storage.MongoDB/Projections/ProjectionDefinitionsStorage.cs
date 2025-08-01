// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Applications.MongoDB;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Json;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Projections;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ProjectionDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents a <see cref="IProjectionDefinitionsStorage"/> for projection definitions in MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
/// </remarks>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
/// <param name="projectionDefinitionSerializer">Serializer for <see cref="ProjectionDefinition"/>.</param>
public class ProjectionDefinitionsStorage(
    IEventStoreDatabase eventStoreDatabase,
    IJsonProjectionDefinitionSerializer projectionDefinitionSerializer) : IProjectionDefinitionsStorage
{
    IMongoCollection<Projection> Collection => eventStoreDatabase.GetCollection<Projection>(WellKnownCollectionNames.ProjectionDefinitions);

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinition>> GetAll()
    {
        using var result = await Collection.FindAsync(FilterDefinition<Projection>.Empty);
        var projections = result.ToList();
        return projections.Select(projection =>
        {
            var definition = projection.Definitions.Last()!.Value;
            return BsonSerializer.Deserialize<ProjectionDefinition>(definition);
        }).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(ProjectionId id) => Collection.Find(_ => _.Id == id).AnyAsync();

    /// <inheritdoc/>
    public async Task<ProjectionDefinition> Get(ProjectionId id)
    {
        using var result = await Collection.FindAsync(_ => _.Id == id);
        var document = result.Single();
        return projectionDefinitionSerializer.Deserialize(JsonNode.Parse(document.Definitions.First().Value.ToJson())!);
    }

    /// <inheritdoc/>
    public Task Delete(ProjectionId id) =>
        Collection.DeleteOneAsync(_ => _.Id == id);

    /// <inheritdoc/>
    public async Task Save(ProjectionDefinition definition)
    {
        var projection = new Projection(
            definition.Identifier,
            definition.Owner,
            new(definition.ReadModel, ReadModelGeneration.First),
            new Dictionary<string, BsonDocument>
            {
                { ProjectionGeneration.First.ToString(), BsonDocument.Parse(projectionDefinitionSerializer.Serialize(definition).ToJsonString()) }
            });
        await Collection.ReplaceOneAsync(
            filter: _ => _.Id == definition.Identifier,
            replacement: projection,
            options: new ReplaceOptions { IsUpsert = true });
    }
}
