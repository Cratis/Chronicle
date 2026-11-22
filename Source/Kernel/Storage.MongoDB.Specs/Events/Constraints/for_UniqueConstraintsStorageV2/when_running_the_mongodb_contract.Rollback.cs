// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorageV2;

public partial class when_running_the_mongodb_contract
{
    [Fact]
    public async Task should_abort_metadata_bootstrap_without_leaving_partial_ownership()
    {
        _gates.BeforeCall = (target, method, args) =>
        {
            if (CreatesBindingCollection(target, method, args)) throw new InvalidOperationException("injected schema failure");
            return Task.CompletedTask;
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _storage.InitializeAuthority(_definition.Name, _authority));
        (await Catalog()).ShouldBeEmpty();
        _gates.BeforeCall = null;
        await Initialize();
    }

    [Fact]
    public async Task should_abort_claim_schema_on_a_duplicate_binding_publication()
    {
        await Initialize();
        var before = await Documents(V2Schema.Bindings);
        _gates.BeforeCall = (target, method, _) =>
        {
            if (IsCollectionCall(target, method, (V2Schema.Bindings, nameof(IMongoCollection<BsonDocument>.InsertOneAsync))))
            {
                var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
                throw new MongoCommandException(connection, "injected duplicate", new BsonDocument(), new BsonDocument { { "ok", 0 }, { "code", 11000 }, { "errmsg", "injected duplicate" } });
            }

            return Task.CompletedTask;
        };
        await Assert.ThrowsAsync<MongoCommandException>(() => _storage.Save(_owner, _definition, 10, _value, _scope, _authority));
        _gates.BeforeCall = null;
        (await Documents(V2Schema.Bindings)).SequenceEqual(before).ShouldBeTrue();
        (await Catalog()).Contains(Binding().Physical).ShouldBeFalse();
    }

    [Fact]
    public async Task should_abort_replacement_and_fence_on_cancellation()
    {
        await Seed();
        var authority = await Documents(V2Schema.Authorities);
        using var cancellation = new CancellationTokenSource();
        _gates.BeforeCall = async (target, method, _) =>
        {
            if (IsCollectionCall(target, method, (Binding().Physical, nameof(IMongoCollection<BsonDocument>.InsertOneAsync))))
            {
                await cancellation.CancelAsync();
                cancellation.Token.ThrowIfCancellationRequested();
            }
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.Save(_owner, _definition, 20, _otherValue, _scope, _authority, cancellationToken: cancellation.Token));
        _gates.BeforeCall = null;
        await ShouldRetainClaim();
        (await Documents(V2Schema.Authorities)).SequenceEqual(authority).ShouldBeTrue();
    }

    [Fact]
    public async Task should_abort_subject_refresh_and_fence_on_a_driver_boundary_failure()
    {
        await Seed();
        var authority = await Documents(V2Schema.Authorities);
        _gates.AfterCall = (target, method, _) =>
        {
            if (IsCollectionCall(target, method, (Binding().Physical, nameof(IMongoCollection<BsonDocument>.UpdateOneAsync)))) throw new InvalidOperationException("injected refresh failure");
            return Task.CompletedTask;
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _storage.Save(_owner, _definition, 99, _value, _scope, _authority, "new-subject"));
        _gates.AfterCall = null;
        await ShouldRetainClaim();
        (await Documents(V2Schema.Authorities)).SequenceEqual(authority).ShouldBeTrue();
        (await _storage.RemoveForSubject(_definition, _subject, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.GetFor(_owner, _definition, _scope)).ShouldBeEmpty();
    }

    static bool CreatesBindingCollection(object target, MethodInfo method, object?[] args) => target is IMongoDatabase && method.Name == nameof(IMongoDatabase.CreateCollectionAsync) && args[1] as string == V2Schema.Bindings;

    static bool IsCollectionCall(object target, MethodInfo method, (string Collection, string Method) call) => target is IMongoCollection<BsonDocument> collection && collection.CollectionNamespace.CollectionName == call.Collection && method.Name == call.Method;
}
