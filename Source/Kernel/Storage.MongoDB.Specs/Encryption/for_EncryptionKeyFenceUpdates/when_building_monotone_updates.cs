// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyFenceUpdates;

public class when_building_monotone_updates : Specification
{
    BsonDocument _raise;
    BsonDocument _authorize;
    BsonDocument _ensure;
    BsonDocument _visible;
    BsonDocument _covered;
    BsonDocument _owned;
    BsonDocument _observed;

    void Because()
    {
        var registry = BsonSerializer.SerializerRegistry;
        var fences = new RenderArgs<EncryptionKeyErasureForIdentifier>(registry.GetSerializer<EncryptionKeyErasureForIdentifier>(), registry);
        var keys = new RenderArgs<EncryptionKeyForIdentifier>(registry.GetSerializer<EncryptionKeyForIdentifier>(), registry);
        var next = new EncryptionKeyErasure(7u, ["fingerprint"], false, 3, 2, 2);
        _raise = EncryptionKeyFenceUpdates.Raise(next, new("request", 7u, 2)).Render(fences).AsBsonDocument;
        _authorize = EncryptionKeyFenceUpdates.Authorize(next with { NewKeyAllowed = true, Lifecycle = 3 }).Render(fences).AsBsonDocument;
        _ensure = EncryptionKeyFenceUpdates.Ensure("subject").Render(fences).AsBsonDocument;
        _visible = EncryptionKeyStorage.Visible("subject").Render(keys);
        _covered = EncryptionKeyStorage.Covered("subject", new("request", 7u, 2)).Render(keys);
        _owned = EncryptionKeyStorage.Owned(new(new("subject", 99u), [1], [2], false, 2, "owner")).Render(keys);
        _observed = EncryptionKeyFenceUpdates.Observed("subject", next).Render(fences);
    }

    [Fact] void should_raise_the_revision_floor_with_max() => _raise["$max"]["erasedThrough"].ShouldEqual(new BsonInt64(7));
    [Fact] void should_raise_the_lifecycle_boundary_with_max() => _raise["$max"]["erasedLifecycle"].ToInt32().ShouldEqual(2);
    [Fact] void should_union_fingerprints() => _raise["$addToSet"]["erasedKeyFingerprints"]["$each"].AsBsonArray.ShouldEqual([new BsonString("fingerprint")]);
    [Fact] void should_append_the_request() => _raise["$push"]["requests"]["requestId"].AsString.ShouldEqual("request");
    [Fact] void should_increment_the_version() => _raise["$inc"]["version"].ToInt64().ShouldEqual(1L);
    [Fact] void should_close_provisioning() => _raise["$set"]["newKeyAllowed"].AsBoolean.ShouldBeFalse();
    [Fact] void should_write_the_seal() => _raise["$set"]["seal"].AsString.Length.ShouldEqual(64);
    [Fact] void should_not_lower_floors_when_authorizing() => _authorize.Contains("$max").ShouldBeFalse();
    [Fact] void should_only_initialize_absent_fences() => _ensure.ElementCount.ShouldEqual(1);
    [Fact] void should_initialize_an_open_fence() => _ensure["$setOnInsert"]["newKeyAllowed"].AsBoolean.ShouldBeTrue();
    [Fact] void should_exclude_only_explicitly_unpublished_rows() => _visible["published"]["$ne"].AsBoolean.ShouldBeFalse();
    [Fact] void should_bound_the_sweep_by_revision_or_lifecycle() => _covered["$or"].AsBsonArray.Count.ShouldEqual(2);
    [Fact] void should_scope_undo_to_the_writer() => _owned["writerNonce"].AsString.ShouldEqual("owner");
    [Fact] void should_scope_undo_to_the_staged_lifecycle() => _owned["stagedLifecycle"].ToInt32().ShouldEqual(2);
    [Fact] void should_guard_the_observed_version() => _observed["version"].ToInt64().ShouldEqual(3L);
    [Fact] void should_guard_the_observed_lifecycle() => _observed["lifecycle"].ToInt32().ShouldEqual(2);
}
