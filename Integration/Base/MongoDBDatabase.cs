// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.Base;

/// <summary>
/// Represents a MongoDB database.
/// </summary>
public class MongoDBDatabase : IDisposable
{
    readonly Subject<ChangeStreamDocument<BsonDocument>> _changes = new();

    /// <summary>
    /// Initializes a new instance of <see cref="MongoDBDatabase"/>
    /// </summary>
    /// <param name="mongoDBContainer"><see cref="IContainer"/> for the MongoDB server.</param>
    /// <param name="database">Database to work with.</param>
    public MongoDBDatabase(IContainer mongoDBContainer, string database)
    {
        var mongoClient = new MongoClient($"mongodb://{mongoDBContainer.Hostname}:{mongoDBContainer.GetMappedPublicPort(27017)}");
        Database = mongoClient.GetDatabase(database);
        var changeDatabase = mongoClient.GetDatabase($"{database}-changes");

        _ = Task.Run(async () =>
        {
            var filter = Builders<ChangeStreamDocument<BsonDocument>>.Filter.In(
                new StringFieldDefinition<ChangeStreamDocument<BsonDocument>, string>("operationType"),
                ["insert", "replace", "update", "delete"]);

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>().Match(filter);

            var cursor = await Database.WatchAsync(
                pipeline,
                new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup });

            while (await cursor.MoveNextAsync())
            {
                if (!cursor.Current.Any()) continue;

                foreach (var document in cursor.Current)
                {
                    _changes.OnNext(document);
                    var changesCollection = changeDatabase.GetCollection<BsonDocument>(document.CollectionNamespace.CollectionName);
                    var changeDocument = new BsonDocument
                    {
                        { "_id", ObjectId.GenerateNewId() },
                        { "documentKey", document.DocumentKey },
                        { "operationType", document.OperationType.ToString() },
                        { "fullDocument", document.FullDocument }
                    };
                    await changesCollection.InsertOneAsync(changeDocument);
                }
            }
        });
    }

    /// <summary>
    /// Gets the <see cref="IMongoDatabase"/> for the database.
    /// </summary>
    public IMongoDatabase Database { get; }

    /// <summary>
    /// Gets the changes for the database.
    /// </summary>
    public IObservable<ChangeStreamDocument<BsonDocument>> Changes => _changes;

    /// <inheritdoc/>
    public void Dispose()
    {
        _changes.Dispose();
    }

    /// <summary>
    /// Called when complete.
    /// </summary>
    public void Complete() => _changes.OnCompleted();
}
