// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyErasureForIdentifierClassMap;

public class when_the_old_class_map_reads_a_new_fence : Specification
{
    BsonClassMap<legacy_fence> _classMap;
    legacy_fence _result;

    void Because()
    {
        // Reproduce the previous four-field map, without the new map's explicit IgnoreExtraElements setting.
        _classMap = new BsonClassMap<legacy_fence>();
        _classMap.AutoMap();
        _classMap.MapIdProperty(_ => _.Id);

        // Bind every legacy member to the immutable fixture's constructor rather than relying on creator inference.
        _classMap.MapCreator(_ => new legacy_fence(_.Id, _.ErasedThrough, _.ErasedKeyFingerprints, _.NewKeyAllowed));
        _classMap.Freeze();
        var document = new EncryptionKeyErasureForIdentifier("subject", 7u, ["fingerprint"], false, Version: 3, Lifecycle: 2, ErasedLifecycle: 2, Requests: [new("request", 7u, 2)], Seal: "seal").ToBsonDocument();
        using var reader = new BsonDocumentReader(document);
        _result = new BsonClassMapSerializer<legacy_fence>(_classMap).Deserialize(BsonDeserializationContext.CreateRoot(reader));
    }

    [Fact] void should_ignore_new_fields_under_production_conventions() => _classMap.IgnoreExtraElements.ShouldBeTrue();
    [Fact] void should_keep_the_identifier() => _result.Id.Value.ShouldEqual("subject");
    [Fact] void should_keep_the_erasure_boundary() => _result.ErasedThrough.Value.ShouldEqual(7u);
    [Fact] void should_keep_the_fingerprints() => _result.ErasedKeyFingerprints.ShouldEqual(["fingerprint"]);
    [Fact] void should_keep_provisioning_closed() => _result.NewKeyAllowed.ShouldBeFalse();

    record legacy_fence(EncryptionKeyIdentifier Id, EncryptionKeyRevision ErasedThrough, string[] ErasedKeyFingerprints, bool NewKeyAllowed);
}
