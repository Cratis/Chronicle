// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Storage.Sinks;
using Cratis.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Kernel.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SinkCollections"/> class.
/// </remarks>
/// <param name="model">The <see cref="Model"/> the context is for.</param>
/// <param name="database">The <see cref="IMongoDatabase"/> to use.</param>
public class SinkCollections(
    Model model,
    IMongoDatabase database) : ISinkCollections
{
    bool _isReplaying;

    string ReplayCollectionName => $"replay-{model.Name}";

    /// <inheritdoc/>
    public async Task BeginReplay()
    {
        _isReplaying = true;
        await PrepareInitialRun();
    }

    /// <inheritdoc/>
    public async Task EndReplay()
    {
        var rewindName = ReplayCollectionName;
        var rewoundCollectionsPrefix = $"{model.Name}-";
        var collectionNames = (await database.ListCollectionNamesAsync()).ToList();
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
                .OrderDescending()
                .First() + 1;
        }
        var oldCollectionName = $"{rewoundCollectionsPrefix}{nextCollectionSequenceNumber}";

        if (collectionNames.Contains(model.Name))
        {
            await database.RenameCollectionAsync(model.Name, oldCollectionName);
        }

        if (collectionNames.Contains(rewindName))
        {
            await database.RenameCollectionAsync(rewindName, model.Name);
        }

        _isReplaying = false;
    }

    /// <inheritdoc/>
    public async Task PrepareInitialRun()
    {
        var collection = GetCollection();
        await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    }

    /// <inheritdoc/>
    public IMongoCollection<BsonDocument> GetCollection() => _isReplaying ? database.GetCollection<BsonDocument>(ReplayCollectionName) : database.GetCollection<BsonDocument>(model.Name);
}
