// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Security.for_AdministratorStorage;

public class when_grants_and_revocations_are_replayed : Specification
{
    IMongoCollection<Administrator> _collection;
    AdministratorStorage _storage;
    Administrator _grant;

    void Establish()
    {
        var database = Substitute.For<IDatabase>();
        _collection = Substitute.For<IMongoCollection<Administrator>>();
        database.GetCollection<Administrator>(WellKnownCollectionNames.Administrators).Returns(_collection);
        _collection.ReplaceOneAsync(Arg.Any<FilterDefinition<Administrator>>(), Arg.Any<Administrator>(), Arg.Any<ReplaceOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(1, 1, null)));
        _collection.DeleteOneAsync(Arg.Any<FilterDefinition<Administrator>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(1)));
        _storage = new AdministratorStorage(database);
        _grant = new Administrator(Guid.NewGuid(), GrantReason.Bootstrap, "spec");
    }

    async Task Because()
    {
        await _storage.Save(_grant);
        await _storage.Save(_grant);
        await _storage.Delete(_grant.Id);
        await _storage.Delete(_grant.Id);
    }

    [Fact] void should_upsert_each_replayed_grant_in_the_separate_collection() => _collection.Received(2).ReplaceOneAsync(
        Arg.Any<FilterDefinition<Administrator>>(), _grant, Arg.Is<ReplaceOptions>(options => options.IsUpsert), Arg.Any<CancellationToken>());
    [Fact] void should_delete_each_replayed_revocation() => _collection.Received(2).DeleteOneAsync(Arg.Any<FilterDefinition<Administrator>>(), Arg.Any<CancellationToken>());
}
