// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Security;
using Cratis.Chronicle.Storage.Security;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;

public class a_data_protection_key_collection : Specification
{
    protected IMongoCollection<DataProtectionKey> _collection;
    protected DataProtectionKeyStorage _storage;
    protected DataProtectionKey _key = new("key-1", "friendly", "<xml/>", DateTimeOffset.UnixEpoch);

    void Establish()
    {
        _collection = Substitute.For<IMongoCollection<DataProtectionKey>>();
        var database = Substitute.For<IDatabase>();
        database.GetCollection<DataProtectionKey>(WellKnownCollectionNames.DataProtectionKeys).Returns(_collection);
        _storage = new(database);
    }

    protected void InsertFailsWith(Exception exception) =>
        _collection.InsertOneAsync(Arg.Any<DataProtectionKey>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(exception));

    protected void WinnerIs(params DataProtectionKey[] winners) =>
        _collection.FindAsync(Arg.Any<FilterDefinition<DataProtectionKey>>(), Arg.Any<FindOptions<DataProtectionKey, DataProtectionKey>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(duplicate_keys.CursorOf(winners)));
}
