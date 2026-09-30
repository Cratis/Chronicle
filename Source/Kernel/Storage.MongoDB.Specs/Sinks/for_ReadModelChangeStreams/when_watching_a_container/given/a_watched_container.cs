// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.when_watching_a_container.given;

/// <summary>
/// A database on a replica set holding a populated container, with the change stream that observes it open.
/// </summary>
/// <param name="connectionString">The connection string of the replica set.</param>
public abstract class a_watched_container(string connectionString) : Specification
{
    protected const string ContainerName = "observed";
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(30);

    protected IMongoDatabase _database;
    IMongoClient _client;
    string _databaseName;
    IChangeStreamCursor<BsonDocument> _cursor;

    async Task Establish()
    {
        _databaseName = $"chronicle_change_streams_{Guid.NewGuid():N}";
        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase(_databaseName);
        await _database.GetCollection<BsonDocument>(ContainerName).InsertOneAsync(new BsonDocument("_id", "existing"));
        _cursor = await _database.WatchAsync(ReadModelChangeStreams.ChangesTo(ContainerName));
    }

    void Destroy()
    {
        _cursor.Dispose();
        _client.DropDatabase(_databaseName);
        _client.Dispose();
    }

    /// <summary>
    /// Insert a document into the watched container, marking the end of what a spec does to it.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected Task InsertMarker() =>
        _database.GetCollection<BsonDocument>(ContainerName).InsertOneAsync(new BsonDocument("_id", "marker"));

    /// <summary>
    /// Collect the operation types the stream reports, in order, up to and including the insert of the marker.
    /// </summary>
    /// <returns>The operation types reported.</returns>
    /// <remarks>
    /// A change stream reports changes in the order they happened, so everything reported before the marker is
    /// exactly what the spec did before inserting it; nothing needs waiting out to prove a change was not reported.
    /// </remarks>
    protected async Task<string[]> OperationsUpToTheMarker()
    {
        using var deadline = new CancellationTokenSource(_deadline);
        var operations = new List<string>();
        while (await _cursor.MoveNextAsync(deadline.Token))
        {
            foreach (var change in _cursor.Current)
            {
                var operation = change["operationType"].AsString;
                operations.Add(operation);
                if (operation == "insert")
                {
                    return [.. operations];
                }
            }
        }

        return [.. operations];
    }
}
