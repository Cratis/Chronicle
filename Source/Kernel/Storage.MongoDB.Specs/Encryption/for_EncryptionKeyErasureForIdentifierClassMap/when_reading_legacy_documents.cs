// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyErasureForIdentifierClassMap;

public class when_reading_legacy_documents : Specification
{
    EncryptionKeyErasureForIdentifier _fence;
    EncryptionKeyForIdentifier _key;

    void Because()
    {
        var fence = new EncryptionKeyErasureForIdentifier("subject", 7u, ["fingerprint"], false).ToBsonDocument();
        foreach (var field in new[] { "version", "lifecycle", "erasedLifecycle", "requests", "seal" })
        {
            fence.Remove(field);
        }

        _fence = BsonSerializer.Deserialize<EncryptionKeyErasureForIdentifier>(fence);
        var key = new EncryptionKeyForIdentifier(new("subject", 7u), [1], [2]).ToBsonDocument();
        foreach (var field in new[] { "published", "stagedLifecycle", "writerNonce" })
        {
            key.Remove(field);
        }

        _key = BsonSerializer.Deserialize<EncryptionKeyForIdentifier>(key);
    }

    [Fact] void should_default_the_version_to_zero() => _fence.Version.ShouldEqual(0UL);
    [Fact] void should_default_the_lifecycle_to_one() => _fence.Lifecycle.ShouldEqual(1u);
    [Fact] void should_preserve_the_legacy_floor() => _fence.ErasedThrough.Value.ShouldEqual(7u);
    [Fact] void should_not_reopen_the_fence() => _fence.NewKeyAllowed.ShouldBeFalse();
    [Fact] void should_default_legacy_keys_to_published() => _key.Published.ShouldBeTrue();
    [Fact] void should_leave_legacy_keys_without_a_staged_lifecycle() => _key.StagedLifecycle.ShouldBeNull();
}
