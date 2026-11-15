// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyErasure;

public class when_covering_an_existing_fence : Specification
{
    EncryptionKeyErasure _existing;
    EncryptionKeyErasure _result;

    void Establish() => _existing = new(8u, ["old"], true, Version: 4, Lifecycle: 3, ErasedLifecycle: 2, Requests: [new("first", 8u, 2, ["old"], true)], Seal: "seal");

    void Because() => _result = EncryptionKeyErasure.Covering(_existing, [(2u, new EncryptionKey([1], [2]))]);

    [Fact] void should_not_lower_the_floor() => _result.ErasedThrough.ShouldEqual(_existing.ErasedThrough);
    [Fact] void should_keep_old_fingerprints() => _result.ErasedKeyFingerprints.ShouldContain("old");
    [Fact] void should_increment_the_version() => _result.Version.ShouldEqual(5ul);
    [Fact] void should_cover_the_current_lifecycle() => _result.ErasedLifecycle.ShouldEqual(3u);
    [Fact] void should_preserve_requests() => _result.Requests.ShouldEqual(_existing.Requests);
    [Fact] void should_preserve_the_seal() => _result.Seal.ShouldEqual("seal");
    [Fact] void should_close_provisioning() => _result.NewKeyAllowed.ShouldBeFalse();
}
