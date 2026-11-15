// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyErasure;

public class when_covering_a_staged_successor : Specification
{
    readonly EncryptionKey _key = new([1], [2]);
    EncryptionKeyErasure _fence;
    bool _covered;

    void Establish() => _fence = new(1u, [], false, Lifecycle: 3, ErasedLifecycle: 3);

    void Because() => _covered = _fence.Covers(new(_key, 2u, 3));

    [Fact] void should_cover_a_higher_revision_staged_before_the_erasure() => _covered.ShouldBeTrue();
}
