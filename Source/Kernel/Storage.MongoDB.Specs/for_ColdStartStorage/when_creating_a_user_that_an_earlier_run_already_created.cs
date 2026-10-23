// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using Cratis.Chronicle.Storage.MongoDB.Security;
using Cratis.Chronicle.Storage.Security;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage;

public class when_creating_a_user_that_an_earlier_run_already_created : Specification
{
    IMongoCollection<User> _collection;
    UserStorage _storage;
    User _user;
    Exception _result;

    void Establish()
    {
        _collection = Substitute.For<IMongoCollection<User>>();
        var database = Substitute.For<IDatabase>();
        database.GetCollection<User>(WellKnownCollectionNames.Users).Returns(_collection);
        _storage = new(database);

        var id = Guid.Parse("7f1d3c8e-1111-4c1e-9a1e-2b5f6a7d8e90");
        _user = new() { Id = id, Username = "admin", SecurityStamp = "this-run", CreatedAt = DateTimeOffset.UnixEpoch.AddDays(1) };
        var earlier = new User { Id = id, Username = "admin", SecurityStamp = "earlier-run", CreatedAt = DateTimeOffset.UnixEpoch };

        _collection.InsertOneAsync(Arg.Any<User>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(duplicate_keys.Duplicate()));
        _collection.FindAsync(Arg.Any<FilterDefinition<User>>(), Arg.Any<FindOptions<User, User>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(duplicate_keys.CursorOf(earlier)));
    }

    async Task Because() => _result = await Catch.Exception(() => _storage.Create(_user));

    [Fact] void should_converge_on_the_stored_user() => _result.ShouldBeNull();
}
