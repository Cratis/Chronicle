// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNameCasing.given;

/// <summary>
/// A MongoDB server holding one database, 'Studio+es+Cratis', named under a prefix of its own.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
public abstract class a_server_with_a_database(MongoDBFixture fixture) : Specification
{
    protected readonly string _prefix = $"case_{Guid.NewGuid().ToString("N")[..8]}_";
    protected MongoClient _client;

    protected string ExistingName => $"{_prefix}Studio+es+Cratis";

    async Task Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        await _client.GetDatabase(ExistingName).GetCollection<BsonDocument>("existing").InsertOneAsync(new BsonDocument("_id", 1));
    }

    async Task Destroy()
    {
        foreach (var name in await (await _client.ListDatabaseNamesAsync()).ToListAsync())
        {
            if (name.StartsWith(_prefix, StringComparison.Ordinal))
            {
                await _client.DropDatabaseAsync(name);
            }
        }
    }
}
