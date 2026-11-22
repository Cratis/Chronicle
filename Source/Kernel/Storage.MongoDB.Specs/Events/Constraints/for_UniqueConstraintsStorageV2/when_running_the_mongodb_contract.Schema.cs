// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;
using Cratis.Monads;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorageV2;

public partial class when_running_the_mongodb_contract
{
    [Fact]
    public async Task should_not_provision_from_missing_reads_or_refused_mutations()
    {
        var before = await Catalog();
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual(Option<UniqueConstraintWriteAuthority>.None());
        (await _storage.IsAllowed(_owner, _definition, _value, _scope)).IsAllowed.ShouldBeTrue();
        (await _storage.GetFor(_owner, _definition, _scope)).ShouldBeEmpty();
        for (var operation = 0; operation < 6; operation++) ShouldRefuse(await Mutation(operation, _authority), UniqueConstraintWriteFailure.MissingAuthority);
        (await Catalog()).SequenceEqual(before).ShouldBeTrue();
    }

    [Fact]
    public async Task should_take_removal_fences_without_provisioning_absent_bindings()
    {
        await Initialize();
        var before = await Catalog();
        var authorityBefore = await Documents(V2Schema.Authorities);
        for (var operation = 1; operation < 6; operation++) (await Mutation(operation, _authority)).IsSuccess.ShouldBeTrue();
        (await Catalog()).SequenceEqual(before).ShouldBeTrue();
        (await Documents(V2Schema.Bindings)).Length.ShouldEqual(1);
        (await Documents(V2Schema.Authorities)).SequenceEqual(authorityBefore).ShouldBeFalse();
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)_authority);
    }

    [Fact]
    public async Task should_fail_closed_for_damaged_bindings_including_removals()
    {
        await Seed();
        await _database.DropCollectionAsync(Binding().Physical);
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.GetFor(_owner, _definition, _scope));
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.Remove(_owner, _definition, _scope, _authority));
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.Save(_owner, _definition, 10, _value, _scope, _authority));
        (await Catalog()).Contains(Binding().Physical).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Sequence")]
    [InlineData("Constraint")]
    [InlineData("Mode")]
    [InlineData("Physical")]
    public async Task should_verify_full_binding_identity_instead_of_only_its_digest(string field)
    {
        await Seed();
        await _database.GetCollection<BsonDocument>(V2Schema.Bindings).UpdateOneAsync(new BsonDocument("_id", Binding().Id), new BsonDocument("$set", new BsonDocument(field, "damaged")));
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.GetFor(_owner, _definition, _scope));
    }

    [Fact]
    public async Task should_reject_unowned_candidate_collections_without_repair()
    {
        await Initialize();
        await _database.CreateCollectionAsync(Binding().Physical);
        var before = await Documents(V2Schema.Bindings);
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.Save(_owner, _definition, 10, _value, _scope, _authority));
        (await Documents(V2Schema.Bindings)).SequenceEqual(before).ShouldBeTrue();
        using var indexes = await _database.GetCollection<BsonDocument>(Binding().Physical).Indexes.ListAsync();
        (await indexes.ToListAsync()).Count.ShouldEqual(1);
    }

    [Fact]
    public async Task should_reject_partial_or_unrecognized_metadata()
    {
        await _database.CreateCollectionAsync(V2Schema.Authorities);
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.InitializeAuthority(_definition.Name, _authority));
        (await Catalog()).ShouldContainOnly(V2Schema.Authorities);
    }

    [Fact]
    public async Task should_reject_missing_indexes_instead_of_repairing_them()
    {
        await Seed();
        var indexes = _database.GetCollection<BsonDocument>(Binding().Physical).Indexes;
        await indexes.DropOneAsync("value");
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.Save(_otherOwner, _definition, 20, _value, _scope, _authority));
        using var remaining = await indexes.ListAsync();
        (await remaining.ToListAsync()).Any(index => index["name"] == "value").ShouldBeFalse();
    }

    [Fact]
    public async Task should_reject_incompatible_index_options_without_changing_them()
    {
        await Seed();
        var indexes = _database.GetCollection<BsonDocument>(Binding().Physical).Indexes;
        await indexes.DropOneAsync("value");
        await indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument { { "Scope", 1 }, { "Value", 1 } }, new CreateIndexOptions { Name = "value", Sparse = true, Unique = true }));
        await Assert.ThrowsAsync<UniqueConstraintV2SchemaMismatch>(() => _storage.GetFor(_owner, _definition, _scope));
    }
}
