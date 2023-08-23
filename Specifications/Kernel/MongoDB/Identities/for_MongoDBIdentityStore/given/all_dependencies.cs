// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Identities.for_MongoDBIdentityStore.given;

public class all_dependencies : Specification
{
    protected Mock<ITenantDatabase> tenant_database;
    protected Mock<IMongoCollection<MongoDBIdentity>> collection;
    protected List<MongoDBIdentity> identities_from_database;
    protected List<MongoDBIdentity> inserted_identities;

    void Establish()
    {
        tenant_database = new();
        collection = new();
        tenant_database.Setup(_ => _.GetCollection<MongoDBIdentity>(CollectionNames.Identities)).Returns(collection.Object);

        identities_from_database = new();
        inserted_identities = new();


        collection
            .Setup(_ => _.FindAsync<MongoDBIdentity>(IsAny<FilterDefinition<MongoDBIdentity>>(), null, default))
            .Returns(() =>
            {
                var cursor = new Mock<IAsyncCursor<MongoDBIdentity>>();
                cursor.SetupSequence(_ => _.MoveNextAsync(default))
                        .ReturnsAsync(true)
                        .ReturnsAsync(false);
                cursor.SetupGet(_ => _.Current)
                      .Returns(() => identities_from_database.ToArray());

                return Task.FromResult(cursor.Object);
            });

        collection
            .Setup(_ => _.InsertOneAsync(IsAny<MongoDBIdentity>(), null, default))
            .Callback<MongoDBIdentity, InsertOneOptions, CancellationToken>((identity, _, _) => inserted_identities.Add(identity))
            .Returns(Task.CompletedTask);
    }
}
