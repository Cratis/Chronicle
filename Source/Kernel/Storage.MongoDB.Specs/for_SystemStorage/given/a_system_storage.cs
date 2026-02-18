// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemStorage.given;

public class a_system_storage : Specification
{
    protected SystemStorage _storage;
    protected IDatabase _database;
    protected IMongoCollection<MongoDBSystemInformation> _collection;

    void Establish()
    {
        _database = Substitute.For<IDatabase>();
        _collection = Substitute.For<IMongoCollection<MongoDBSystemInformation>>();
        _database.GetCollection<MongoDBSystemInformation>(Arg.Any<string>()).Returns(_collection);
        _storage = new SystemStorage(_database);
    }
}
