// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;
using Cratis.Monads;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorageV2;

public partial class when_running_the_mongodb_contract
{
    [Fact]
    public async Task should_recheck_metadata_when_bootstrap_commits_between_catalog_reads()
    {
        var publisher = new UniqueConstraintsStorageV2(_database, _sequence);
        var publications = 0;
        _gates.AfterCall = async (target, method, args) =>
        {
            if (!IsCatalogLookup(target, method, args, V2Schema.Authorities)) return;
            if (Interlocked.CompareExchange(ref publications, 1, 0) != 0) return;

            // The absent Authorities cursor is already captured; bootstrap finishes before Bindings is read.
            (await publisher.InitializeAuthority(_definition.Name, _authority)).IsSuccess.ShouldBeTrue();
        };
        var authority = await _storage.GetAuthority(_definition.Name);
        _gates.AfterCall = null;
        publications.ShouldEqual(1);
        authority.ShouldEqual((Option<UniqueConstraintWriteAuthority>)_authority);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task should_recheck_a_binding_published_after_an_absent_lookup(bool afterPhysicalLookup, bool remove)
    {
        await Initialize();
        var publisher = new UniqueConstraintsStorageV2(_database, _sequence);
        var publications = 0;
        _gates.AfterCall = async (target, method, args) =>
        {
            var lookup = afterPhysicalLookup
                ? IsCatalogLookup(target, method, args, Binding().Physical)
                : IsBindingLookup(target, method, args);
            if (!lookup) return;
            if (Interlocked.CompareExchange(ref publications, 1, 0) != 0) return;

            // Publish after the chosen absent result, before the next ownership observation.
            (await publisher.Save(_owner, _definition, 10, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        };
        if (remove)
        {
            (await _storage.Remove(_owner, _definition, _scope, _authority)).IsSuccess.ShouldBeTrue();
            (await _storage.GetFor(_owner, _definition, _scope)).ShouldBeEmpty();
        }
        else
        {
            (await _storage.GetFor(_owner, _definition, _scope)).ShouldContainOnly(_value);
        }

        _gates.AfterCall = null;
        publications.ShouldEqual(1);
    }

    static bool IsCatalogLookup(object target, MethodInfo method, object?[] args, string name)
    {
        if (target is not IMongoDatabase) return false;
        if (method.Name != nameof(IMongoDatabase.ListCollectionsAsync)) return false;
        var options = args.OfType<ListCollectionsOptions>().SingleOrDefault();
        var filter = options?.Filter as BsonDocumentFilterDefinition<BsonDocument>;
        return filter?.Document.GetValue("name", BsonNull.Value) == new BsonString(name);
    }

    bool IsBindingLookup(object target, MethodInfo method, object?[] args)
    {
        if (target is not IMongoCollection<BsonDocument> collection) return false;
        if (collection.CollectionNamespace.CollectionName != V2Schema.Bindings) return false;
        if (method.Name != nameof(IMongoCollection<BsonDocument>.FindAsync)) return false;
        var filter = args.OfType<BsonDocumentFilterDefinition<BsonDocument>>().SingleOrDefault();
        return filter?.Document.GetValue("_id", BsonNull.Value) == new BsonString(Binding().Id);
    }
}
