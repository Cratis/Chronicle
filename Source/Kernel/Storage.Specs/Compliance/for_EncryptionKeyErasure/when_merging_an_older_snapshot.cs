// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyErasure;

public class when_merging_an_older_snapshot : Specification
{
    EncryptionKeyErasure _current;
    EncryptionKeyErasure _older;
    EncryptionKeyErasure _result;

    void Establish()
    {
        _current = new(9u, ["new"], true, Version: 10, Lifecycle: 3, ErasedLifecycle: 2, Requests: [new("same", 9u, 2, ["new"], true)]);
        _older = new(2u, ["old"], false, Version: 2, Lifecycle: 1, ErasedLifecycle: 1, Requests: [new("same", 2u, 1, ["old"]), new("other", 1u, 1)]);
    }

    void Because() => _result = _current.Merge(_older);

    [Fact] void should_keep_the_greatest_floor() => _result.ErasedThrough.ShouldEqual(_current.ErasedThrough);
    [Fact] void should_union_the_fingerprints() => _result.ErasedKeyFingerprints.ShouldContainOnly("new", "old");
    [Fact] void should_preserve_the_lifecycle() => _result.Lifecycle.ShouldEqual(3u);
    [Fact] void should_preserve_the_erased_lifecycle() => _result.ErasedLifecycle.ShouldEqual(2u);
    [Fact] void should_increment_the_current_version() => _result.Version.ShouldEqual(11ul);
    [Fact] void should_close_provisioning() => _result.NewKeyAllowed.ShouldBeFalse();
    [Fact] void should_keep_both_requests() => _result.Requests.Count.ShouldEqual(2);
    [Fact] void should_keep_the_original_request_boundary() => _result.Requests[0].Through.ShouldEqual(_current.Requests[0].Through);
    [Fact] void should_not_unconfirm_a_request() => _result.Requests[0].Confirmed.ShouldBeTrue();
    [Fact] void should_union_request_fingerprints() => _result.Requests[0].Fingerprints.ShouldContainOnly("new", "old");
}
