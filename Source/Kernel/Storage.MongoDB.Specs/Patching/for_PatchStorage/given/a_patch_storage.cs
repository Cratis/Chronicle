// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patching;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Storage.MongoDB.Patching;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Patching.for_PatchStorage.given;

public class a_patch_storage : Specification
{
    protected PatchStorage _storage;
    protected IDatabase _database;
    protected IMongoCollection<Patch> _collection;

    void Establish()
    {
        _database = Substitute.For<IDatabase>();
        _collection = Substitute.For<IMongoCollection<Patch>>();
        _database.GetCollection<Patch>(Arg.Any<string>()).Returns(_collection);
        _storage = new PatchStorage(_database);
    }
}
