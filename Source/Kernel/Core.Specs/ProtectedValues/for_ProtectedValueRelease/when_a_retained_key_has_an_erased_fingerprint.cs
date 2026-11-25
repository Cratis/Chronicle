// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_a_retained_key_has_an_erased_fingerprint : given.a_rotated_key
{
    void Establish() => _fence = new EncryptionKeyErasure(1, [_old.Key.Fingerprint], true);

    async Task Because() => _failure = await Catch.Exception(Release);

    [Fact] void should_not_return_plaintext() => _released.ShouldBeNull();
    [Fact] void should_reject_the_erased_material() => _failure.ShouldBeOfExactType<EncryptionKeyUnrecoverable>();
}
