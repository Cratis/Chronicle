// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyErasureForIdentifierClassMap;

public class when_comparing_deserialized_contract_snapshots : Specification
{
    EncryptionKeyErasure _expected;
    EncryptionKeyErasure _actual;

    void Because()
    {
        var document = new EncryptionKeyErasureForIdentifier("subject", 7u, ["fingerprint"], false, 3, 2, 2, [new("request", 7u, 2, ["fingerprint"], true)], "seal");
        var restored = BsonSerializer.Deserialize<EncryptionKeyErasureForIdentifier>(document.ToBson());
        _expected = document.ToFence();
        _actual = restored.ToFence();
    }

    [Fact] void should_preserve_the_boundary() => _actual.ErasedThrough.ShouldEqual(_expected.ErasedThrough);
    [Fact] void should_preserve_the_fingerprints_by_content() => _actual.ErasedKeyFingerprints.ShouldEqual(_expected.ErasedKeyFingerprints);
    [Fact] void should_preserve_the_provisioning_state() => _actual.NewKeyAllowed.ShouldEqual(_expected.NewKeyAllowed);
    [Fact] void should_preserve_the_version() => _actual.Version.ShouldEqual(3UL);
    [Fact] void should_preserve_the_lifecycle() => _actual.Lifecycle.ShouldEqual(2u);
    [Fact] void should_preserve_the_erased_lifecycle() => _actual.ErasedLifecycle.ShouldEqual(2u);
    [Fact] void should_preserve_the_seal() => _actual.Seal.ShouldEqual("seal");
    [Fact] void should_preserve_the_request_identifier() => _actual.Requests!.Single().RequestId.ShouldEqual("request");
    [Fact] void should_preserve_the_request_fingerprints() => _actual.Requests!.Single().Fingerprints!.ShouldEqual("fingerprint");
    [Fact] void should_preserve_the_confirmation() => _actual.Requests!.Single().Confirmed.ShouldBeTrue();
}
