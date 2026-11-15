// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage;

public class when_any_member_lacks_fence_guarantees : Specification
{
    CompositeEncryptionKeyStorage _composite;
    Exception _error;
    EncryptionKeyErasure? _fence;

    void Establish() => _composite = new(new InMemoryEncryptionKeyStorage(), Substitute.For<IEncryptionKeyStorage>());

    async Task Because() => _error = await Catch.Exception(async () => _fence = await _composite.GetErasureForRelease("store", "namespace", "subject"));

    [Fact] void should_not_advertise_a_partial_guarantee() => _composite.FenceGuarantees.ShouldEqual(EncryptionKeyFenceGuarantees.None);
    [Fact] void should_allow_release_n_reads() => _error.ShouldBeNull();
    [Fact] void should_return_the_legacy_fence() => _fence.ShouldBeNull();
}
