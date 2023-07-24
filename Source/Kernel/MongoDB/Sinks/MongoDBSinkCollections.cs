// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Engines.Sinks;
using Aksio.Cratis.Projections;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for MongoDB.
/// </summary>
public class MongoDBSinkCollections : IMongoDBSinkCollections
{
    readonly IMongoDatabase _database;
    readonly Model _model;

    string ReplayCollectionName => $"replay-{_model.Name}";

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBSinkCollections"/> class.
    /// </summary>
    /// <param name="database"><see cref="IMongoDatabase"/> to use.</param>
    /// <param name="model">The <see cref="Model"/> the context is for.</param>
    public MongoDBSinkCollections(IMongoDatabase database, Model model)
    {
        _database = database;
        _model = model;
    }

    /// <inheritdoc/>
    public Task BeginReplay() => PrepareInitialRun(true);

    /// <inheritdoc/>
    public async Task EndReplay()
    {
        var rewindName = ReplayCollectionName;
        var rewoundCollectionsPrefix = $"{_model.Name}-";
        var collectionNames = (await _database.ListCollectionNamesAsync()).ToList();
        var nextCollectionSequenceNumber = 1;
        var rewoundCollectionNames = collectionNames.Where(_ => _.StartsWith(rewoundCollectionsPrefix, StringComparison.InvariantCulture)).ToArray();
        if (rewoundCollectionNames.Length > 0)
        {
            nextCollectionSequenceNumber = rewoundCollectionNames
                .Select(_ =>
                {
                    var postfix = _.Substring(rewoundCollectionsPrefix.Length);
                    if (int.TryParse(postfix, out var value))
                    {
                        return value;
                    }
                    return -1;
                })
                .Where(_ => _ >= 0)
                .OrderByDescending(_ => _)
                .First() + 1;
        }
        var oldCollectionName = $"{rewoundCollectionsPrefix}{nextCollectionSequenceNumber}";

        if (collectionNames.Contains(_model.Name))
        {
            await _database.RenameCollectionAsync(_model.Name, oldCollectionName);
        }

        if (collectionNames.Contains(rewindName))
        {
            await _database.RenameCollectionAsync(rewindName, _model.Name);
        }
    }

    /// <inheritdoc/>
    public async Task PrepareInitialRun(bool isReplaying)
    {
        var collection = GetCollection(isReplaying);
        await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    }

    /// <inheritdoc/>
    public IMongoCollection<BsonDocument> GetCollection(bool isReplaying) => isReplaying ? _database.GetCollection<BsonDocument>(ReplayCollectionName) : _database.GetCollection<BsonDocument>(_model.Name);
}
