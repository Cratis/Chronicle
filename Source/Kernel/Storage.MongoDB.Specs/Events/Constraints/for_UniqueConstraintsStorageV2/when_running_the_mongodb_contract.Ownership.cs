// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorageV2;

public partial class when_running_the_mongodb_contract
{
    [Theory]
    [InlineData("owner", "owner ")]
    [InlineData("owner", "Owner")]
    [InlineData("é", "e\u0301")]
    [InlineData("owner", "owner\0")]
    public async Task should_never_refresh_replace_or_remove_another_exact_owner(string owner, string other)
    {
        await Initialize();
        (await _storage.Save(owner, _definition, 10, _value, _scope, _authority, _subject)).IsSuccess.ShouldBeTrue();
        ShouldRefuse(await _storage.Save(other, _definition, 20, _value, _scope, _authority, "other-subject"), UniqueConstraintWriteFailure.ConflictingClaim);
        (await _storage.Remove(other, _definition, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.GetFor(owner, _definition, _scope)).ShouldContainOnly(_value);
        (await _storage.GetFor(other, _definition, _scope)).ShouldBeEmpty();
        (await _storage.RemoveForSubject(_definition, "other-subject", _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.GetFor(owner, _definition, _scope)).ShouldContainOnly(_value);
    }

    [Fact]
    public async Task should_support_owners_and_subjects_beyond_sql_limits_without_truncation()
    {
        await Initialize();
        var owner = new string('o', 5000);
        var subject = new string('s', 6000);
        (await _storage.Save(owner, _definition, 10, _value, _scope, _authority, subject)).IsSuccess.ShouldBeTrue();
        (await _storage.Remove(owner + " ", _definition, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.RemoveForSubject(_definition, subject + " ", _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.GetFor(owner, _definition, _scope)).ShouldContainOnly(_value);
        (await _storage.RemoveForSubject(_definition, subject, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.GetFor(owner, _definition, _scope)).ShouldBeEmpty();
    }

    [Fact]
    public async Task should_create_binary_claims_beside_non_simple_collections()
    {
        await _database.CreateCollectionAsync("case-insensitive-sentinel", new CreateCollectionOptions { Collation = new Collation("en", strength: CollationStrength.Secondary) });
        await Seed();
        var collection = _database.GetCollection<BsonDocument>(Binding().Physical);
        var row = await collection.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        row["EventSourceId"].AsString.ShouldEqual(_owner.Value);
        (await _storage.GetFor("OWNER", _definition, _scope)).ShouldBeEmpty();
        using var indexes = await collection.Indexes.ListAsync();
        (await indexes.ToListAsync()).Single(index => index["name"] == "value")["unique"].AsBoolean.ShouldBeTrue();
    }

    [Fact]
    public async Task should_refuse_a_non_simple_bound_collection()
    {
        await Seed();
        await _database.DropCollectionAsync(Binding().Physical);
        await _database.CreateCollectionAsync(Binding().Physical, new CreateCollectionOptions { Collation = new Collation("en", strength: CollationStrength.Secondary) });
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.GetFor(_owner, _definition, _scope));
    }
}
