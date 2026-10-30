// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.SingleWriter;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBSingleWriterLeaseStorage.given;

public class a_lease(MongoDBFixture fixture) : Specification
{
    protected ISingleWriterLeaseStorage Storage;
    protected SingleWriterLeaseClaim Claim;
    IMongoCollection<SingleWriterLeaseDocument> _collection;
    IMongoClient _client;
    string _databaseName;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _databaseName = $"lease_spec_{Guid.NewGuid():N}";
        _collection = _client.GetDatabase(_databaseName).GetCollection<SingleWriterLeaseDocument>(WellKnownCollectionNames.SingleWriterLeases);
        var database = Substitute.For<IDatabase>();
        database.GetCollection<SingleWriterLeaseDocument>(WellKnownCollectionNames.SingleWriterLeases).Returns(_collection);
        Storage = new MongoDBSingleWriterLeaseStorage(database);
        Claim = new(new("Owner-A"), TimeSpan.FromSeconds(30), "cluster", "service", "host", "1.0");
    }

    protected Task Expire() => _collection.UpdateOneAsync(document => document.Id == "kernel", Builders<SingleWriterLeaseDocument>.Update.Set(document => document.ExpiresAt, DateTime.UnixEpoch));

    async Task Destroy() => await _client.DropDatabaseAsync(_databaseName);
}
