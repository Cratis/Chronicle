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

        // Keep production extra-element handling, but pin the old wire shape and construction locally.
        _classMap.MapIdProperty(_ => _.Id).SetElementName("_id");
        _classMap.MapProperty(_ => _.ErasedThrough).SetElementName("erasedThrough");
        _classMap.MapProperty(_ => _.ErasedKeyFingerprints).SetElementName("erasedKeyFingerprints");
        _classMap.MapProperty(_ => _.NewKeyAllowed).SetElementName("newKeyAllowed");
        _classMap.SetCreator(() => new legacy_fence()).Freeze();
        var document = new EncryptionKeyErasureForIdentifier("subject", 7u, ["fingerprint"], false, Version: 3, Lifecycle: 2, ErasedLifecycle: 2, Requests: [new("request", 7u, 2)], Seal: "seal").ToBsonDocument();
        using var reader = new BsonDocumentReader(document);
        _result = new BsonClassMapSerializer<legacy_fence>(_classMap).Deserialize(BsonDeserializationContext.CreateRoot(reader));
    }

    [Fact] void should_ignore_new_fields_under_production_conventions() => _classMap.IgnoreExtraElements.ShouldBeTrue();
    [Fact] void should_keep_the_identifier() => _result.Id.Value.ShouldEqual("subject");
    [Fact] void should_keep_the_erasure_boundary() => _result.ErasedThrough.Value.ShouldEqual(7u);
    [Fact] void should_keep_the_fingerprints() => _result.ErasedKeyFingerprints.ShouldEqual(["fingerprint"]);
    [Fact] void should_keep_provisioning_closed() => _result.NewKeyAllowed.ShouldBeFalse();

    class legacy_fence
    {
        public EncryptionKeyIdentifier Id { get; set; }

        public EncryptionKeyRevision ErasedThrough { get; set; }

        public string[] ErasedKeyFingerprints { get; set; }

        public bool NewKeyAllowed { get; set; }
    }
}
