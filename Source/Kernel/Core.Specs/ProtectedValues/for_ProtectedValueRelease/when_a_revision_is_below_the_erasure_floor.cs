// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_a_revision_is_below_the_erasure_floor : given.a_rotated_key
{
    void Establish() => _fence = new EncryptionKeyErasure(2, [], true);

    async Task Because() => _failure = await Catch.Exception(Release);

    [Fact] void should_not_fetch_the_erased_revision() => _storage.DidNotReceive().TryGetAtRevisionFor(Address.Item1, Address.Item2, Address.Item3, (EncryptionKeyRevision)2u);
    [Fact] void should_not_return_plaintext() => _released.ShouldBeNull();
    [Fact] void should_classify_the_exhausted_search() => _failure.ShouldBeOfExactType<EncryptionKeyUnrecoverable>();
}
