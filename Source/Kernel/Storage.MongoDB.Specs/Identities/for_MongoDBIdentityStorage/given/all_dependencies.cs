// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.given;

public class all_dependencies : Specification
{
    protected IEventStoreNamespaceDatabase _database;
    protected IMongoCollection<MongoDBIdentity> _collection;
    protected List<MongoDBIdentity> _identitiesFromDatabase;
    protected List<MongoDBIdentity> _insertedIdentities;

    void Establish()
    {
        _database = Substitute.For<IEventStoreNamespaceDatabase>();
        _collection = Substitute.For<IMongoCollection<MongoDBIdentity>>();
        _database.GetCollection<MongoDBIdentity>(WellKnownCollectionNames.Identities).Returns(_collection);

        _identitiesFromDatabase = [];
        _insertedIdentities = [];

        _collection
            .FindAsync<MongoDBIdentity>(Arg.Any<FilterDefinition<MongoDBIdentity>>(), null, default)
            .Returns(_ =>
            {
                var cursor = Substitute.For<IAsyncCursor<MongoDBIdentity>>();
                cursor
                    .MoveNextAsync(default)
                    .Returns(true, false);

                cursor.Current
                      .Returns(_ => [.. _identitiesFromDatabase]);

                return Task.FromResult(cursor);
            });

        _collection
            .When(_ => _.InsertOneAsync(Arg.Any<MongoDBIdentity>(), null, default))
            .Do(callInfo => _insertedIdentities.Add(callInfo.Arg<MongoDBIdentity>()));
    }
}
