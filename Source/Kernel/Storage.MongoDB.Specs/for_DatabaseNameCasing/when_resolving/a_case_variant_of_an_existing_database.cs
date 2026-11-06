// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNameCasing.when_resolving;

/// <summary>
/// MongoDB refuses to create a database differing only in case from one that exists, so a name asked for in another
/// casing opens the existing database.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class a_case_variant_of_an_existing_database(MongoDBFixture fixture) : given.a_server_with_a_database(fixture)
{
    string _resolved;
    long _existingDocuments;

    async Task Because()
    {
        _resolved = DatabaseNameCasing.Resolve(_client, $"{_prefix}Studio+es+cratis");
        var database = _client.GetDatabaseByExistingCasing($"{_prefix}studio+ES+cratis");
        _existingDocuments = await database.GetCollection<BsonDocument>("existing").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        await database.GetCollection<BsonDocument>("written").Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("value")));
    }

    [Fact] void should_resolve_to_the_existing_casing() => _resolved.ShouldEqual(ExistingName);
    [Fact] void should_read_the_existing_database() => _existingDocuments.ShouldEqual(1L);
}
