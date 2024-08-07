// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.given;

public class all_dependencies : Specification
{
    protected Mock<IEventStoreNamespaceDatabase> database;
    protected Mock<IMongoCollection<MongoDBIdentity>> collection;
    protected List<MongoDBIdentity> identities_from_database;
    protected List<MongoDBIdentity> inserted_identities;

    void Establish()
    {
        database = new();
        collection = new();
        database.Setup(_ => _.GetCollection<MongoDBIdentity>(WellKnownCollectionNames.Identities)).Returns(collection.Object);

        identities_from_database = [];
        inserted_identities = [];


        collection
            .Setup(_ => _.FindAsync<MongoDBIdentity>(IsAny<FilterDefinition<MongoDBIdentity>>(), null, default))
            .Returns(() =>
            {
                var cursor = new Mock<IAsyncCursor<MongoDBIdentity>>();
                cursor.SetupSequence(_ => _.MoveNextAsync(default))
                        .ReturnsAsync(true)
                        .ReturnsAsync(false);
                cursor.SetupGet(_ => _.Current)
                      .Returns(() => [.. identities_from_database]);

                return Task.FromResult(cursor.Object);
            });

        collection
            .Setup(_ => _.InsertOneAsync(IsAny<MongoDBIdentity>(), null, default))
            .Callback<MongoDBIdentity, InsertOneOptions, CancellationToken>((identity, _, _) => inserted_identities.Add(identity))
            .Returns(Task.CompletedTask);
    }
}
