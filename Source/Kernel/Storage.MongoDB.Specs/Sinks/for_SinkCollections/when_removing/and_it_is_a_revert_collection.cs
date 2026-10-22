// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkCollections.when_removing;

[Collection(MongoDBCollection.Name)]
public class and_it_is_a_revert_collection(MongoDBFixture fixture) : given.two_silos_sharing_a_database(fixture)
{
    IReadOnlyList<string> _collections = [];

    [Fact] public void should_drop_it() => _collections.ShouldNotContain("things-reverted");
    [Fact] public void should_keep_the_primary() => _collections.ShouldContain(ContainerName);

    protected override async Task Establish()
    {
        await Database.GetCollection<BsonDocument>("things-reverted").InsertOneAsync(new BsonDocument("_id", "old"));
        await FirstSilo.Remove("things-reverted");
        _collections = await CollectionNames();
    }
}
