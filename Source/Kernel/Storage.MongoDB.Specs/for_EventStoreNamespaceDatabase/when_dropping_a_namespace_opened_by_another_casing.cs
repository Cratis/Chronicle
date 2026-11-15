// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreNamespaceDatabase;

/// <summary>
/// Removing a namespace 'contoso' while the database on the server is 'Studio+es+Contoso' must not delete the events of
/// the namespace that database belongs to.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class when_dropping_a_namespace_opened_by_another_casing(MongoDBFixture fixture) : Specification
{
    readonly string _prefix = $"nsdrop_{Guid.NewGuid().ToString("N")[..8]}_";
    MongoClient _client;
    EventStoreNamespaceDatabase _caseVariant;
    EventStoreNamespaceDatabase _exact;
    string[] _afterCaseVariantDrop;
    string[] _afterExactDrop;

    async Task Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        await _client.GetDatabase($"{_prefix}Studio+es+Contoso").GetCollection<BsonDocument>(WellKnownCollectionNames.EventLog)
            .InsertOneAsync(new BsonDocument("_id", 0L));

        var clientManager = Substitute.For<IMongoDBClientManager>();
        clientManager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(_client);
        var mongoOptions = Options.Create(new MongoDBOptions { Server = fixture.ConnectionString, Database = "chronicle" });
        var storageOptions = Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix });
        _caseVariant = new("Studio", "contoso", clientManager, mongoOptions, storageOptions);
        _exact = new("Studio", "Contoso", clientManager, mongoOptions, storageOptions);
    }

    async Task Because()
    {
        await _caseVariant.Drop();
        _afterCaseVariantDrop = await Databases();
        await _exact.Drop();
        _afterExactDrop = await Databases();
    }

    [Fact] void should_keep_the_database_of_the_other_casing() => _afterCaseVariantDrop.ShouldContainOnly($"{_prefix}Studio+es+Contoso");
    [Fact] void should_drop_the_database_for_its_own_casing() => _afterExactDrop.ShouldBeEmpty();

    async Task<string[]> Databases() =>
        [.. (await (await _client.ListDatabaseNamesAsync()).ToListAsync()).Where(_ => _.StartsWith(_prefix, StringComparison.Ordinal))];

    async Task Destroy()
    {
        foreach (var name in await Databases())
        {
            await _client.DropDatabaseAsync(name);
        }
    }
}
