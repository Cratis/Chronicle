// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents a <see cref="IReadModelDefinitionsStorage"/> for projection definitions in MongoDB.
/// </summary>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
public class ReadModelDefinitionsStorage(
    IEventStoreDatabase eventStoreDatabase) : IReadModelDefinitionsStorage
{
    IMongoCollection<ReadModel> Collection => eventStoreDatabase.GetCollection<ReadModel>(WellKnownCollectionNames.ReadModelDefinitions);

    /// <inheritdoc/>
    public async Task<IEnumerable<ReadModelDefinition>> GetAll()
    {
        var filter = Builders<ReadModel>.Filter.Empty;
        var readModels = await Collection.Find(filter).ToListAsync();
        var readModelDefinitions = new List<ReadModelDefinition>();
        foreach (var readModel in readModels)
        {
            readModelDefinitions.Add(await CreateDefinitionFrom(readModel));
        }

        return readModelDefinitions;
    }

    /// <inheritdoc/>
    public Task Delete(ReadModelName name) =>
        Collection.DeleteOneAsync(rm => rm.Id == name.Value);

    /// <inheritdoc/>
    public async Task<ReadModelDefinition> Get(ReadModelName name)
    {
        var readModel = await Collection.Find(rm => rm.Id == name.Value).SingleAsync();
        return await CreateDefinitionFrom(readModel);
    }

    /// <inheritdoc/>
    public Task<bool> Has(ReadModelName name) =>
        Collection.Find(rm => rm.Id == name.Value).AnyAsync();

    /// <inheritdoc/>
    public Task Save(ReadModelDefinition definition)
    {
        var readModel = new ReadModel(definition.Name.Value, definition.Owner, definition.Schemas.ToDictionary(_ => _.Key.ToString(), _ => BsonDocument.Parse(_.Value.ToJson())));
        return Collection.ReplaceOneAsync(rm => rm.Id == readModel.Id, readModel, new ReplaceOptions { IsUpsert = true });
    }

    async Task<ReadModelDefinition> CreateDefinitionFrom(ReadModel readModel)
    {
        Dictionary<ReadModelGeneration, JsonSchema> schemas = new();
        foreach (var (key, schema) in readModel.Schemas)
        {
            var generation = (ReadModelGeneration)key!;
            schemas[generation] = await JsonSchema.FromJsonAsync(schema.ToJson());
        }
        return new(readModel.Id, readModel.Owner, schemas);
    }
}
