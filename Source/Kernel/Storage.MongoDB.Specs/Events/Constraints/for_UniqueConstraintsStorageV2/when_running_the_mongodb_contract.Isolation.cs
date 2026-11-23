// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorageV2;

public partial class when_running_the_mongodb_contract
{
    [Fact]
    public async Task should_coordinate_racing_lazy_publication_on_independent_handles()
    {
        await Initialize();
        var other = Handle();
        var results = await Task.WhenAll(
            Task.Run(() => _storage.Save(_owner, _definition, 10, _value, _scope, _authority)),
            Task.Run(() => other.Save(_otherOwner, _definition, 20, _otherValue, _scope, _authority))).WaitAsync(TimeSpan.FromSeconds(40));
        results.All(result => result.IsSuccess).ShouldBeTrue();
        await ShouldRetainClaim();
        (await other.GetFor(_otherOwner, _definition, _scope)).ShouldContainOnly(_otherValue);
        (await Documents(V2Schema.Bindings)).Length.ShouldEqual(2);
    }

    [Fact]
    public async Task should_admit_only_one_exact_owner_for_competing_claims()
    {
        await Initialize();
        var other = Handle();
        var results = await Task.WhenAll(
            Task.Run(() => _storage.Save(_owner, _definition, 10, _value, _scope, _authority)),
            Task.Run(() => other.Save(_otherOwner, _definition, 20, _value, _scope, _authority))).WaitAsync(TimeSpan.FromSeconds(40));
        results.Count(result => result.IsSuccess).ShouldEqual(1);
        ShouldRefuse(results.Single(result => !result.IsSuccess), UniqueConstraintWriteFailure.ConflictingClaim);
        (await _database.GetCollection<BsonDocument>(Binding().Physical).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(1L);
    }

    [Fact]
    public async Task should_condition_competing_transitions_from_independent_handles_on_the_same_proof()
    {
        await Initialize();
        var other = Handle();
        var results = await _gates.Concurrent(
            () => _storage.TryTransition(_definition.Name, _authority, _authority.Phase),
            () => other.TryTransition(_definition.Name, _authority, _authority.Phase));
        results.Count(result => result.IsSuccess).ShouldEqual(1);
        results.Single(result => result.IsSuccess).AsT0.Revision.Value.ShouldEqual(1L);
        results.Single(result => !result.IsSuccess).AsT1.ShouldEqual(UniqueConstraintWriteFailure.AuthorityMismatch);
    }

    [Fact]
    public async Task should_isolate_sequences_constraints_modes_and_namespace_databases()
    {
        await Seed();
        var otherSequence = Handle("another-sequence");
        ShouldRefuse(await otherSequence.Save(_otherOwner, _definition, 20, _value, _scope, _authority), UniqueConstraintWriteFailure.MissingAuthority);
        (await otherSequence.InitializeAuthority(_definition.Name, _authority)).IsSuccess.ShouldBeTrue();
        (await otherSequence.Save(_otherOwner, _definition, 20, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        var otherDefinition = _definition with { Name = "another-constraint" };
        (await _storage.InitializeAuthority(otherDefinition.Name, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_otherOwner, otherDefinition, 20, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_otherOwner, PerValue, 20, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        var isolated = _database.Client.GetDatabase(MongoDBSpecDatabaseNames.New());
        try
        {
            var otherNamespace = Handle(database: isolated);
            ShouldRefuse(await otherNamespace.Remove(_owner, _definition, _scope, _authority), UniqueConstraintWriteFailure.MissingAuthority);
            (await otherNamespace.InitializeAuthority(_definition.Name, _authority)).IsSuccess.ShouldBeTrue();
            (await otherNamespace.Save(_otherOwner, _definition, 20, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
            await ShouldRetainClaim();
        }
        finally
        {
            await _database.Client.DropDatabaseAsync(isolated.DatabaseNamespace.DatabaseName);
        }
    }
}
