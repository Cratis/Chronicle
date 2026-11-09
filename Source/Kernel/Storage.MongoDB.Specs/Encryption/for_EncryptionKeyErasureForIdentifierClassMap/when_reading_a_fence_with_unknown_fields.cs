// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyErasureForIdentifierClassMap;

public class when_reading_a_fence_with_unknown_fields : Specification
{
    EncryptionKeyErasureForIdentifier _result;

    void Because()
    {
        var document = new EncryptionKeyErasureForIdentifier("subject", 7u, ["fingerprint"], false).ToBsonDocument();
        document.Add("futureFenceMetadata", new BsonDocument("value", 42));
        _result = BsonSerializer.Deserialize<EncryptionKeyErasureForIdentifier>(document);
    }

    [Fact] void should_keep_the_erasure_boundary() => _result.ErasedThrough.Value.ShouldEqual(7u);
    [Fact] void should_keep_the_fingerprints() => _result.ErasedKeyFingerprints.ShouldEqual(["fingerprint"]);
    [Fact] void should_keep_provisioning_closed() => _result.NewKeyAllowed.ShouldBeFalse();
}
