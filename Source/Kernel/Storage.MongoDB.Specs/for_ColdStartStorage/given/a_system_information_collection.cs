// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;

public class a_system_information_collection : Specification
{
    protected IMongoCollection<MongoDBSystemInformation> _collection;
    protected SystemStorage _storage;

    void Establish()
    {
        _collection = Substitute.For<IMongoCollection<MongoDBSystemInformation>>();
        var database = Substitute.For<IDatabase>();
        database.GetCollection<MongoDBSystemInformation>("system-information").Returns(_collection);
        _storage = new(database);
    }

    protected void UpsertsReturn(params Task<ReplaceOneResult>[] results) =>
        _collection.ReplaceOneAsync(Arg.Any<FilterDefinition<MongoDBSystemInformation>>(), Arg.Any<MongoDBSystemInformation>(), Arg.Any<ReplaceOptions>(), Arg.Any<CancellationToken>())
            .Returns(results[0], results[1..]);
}
