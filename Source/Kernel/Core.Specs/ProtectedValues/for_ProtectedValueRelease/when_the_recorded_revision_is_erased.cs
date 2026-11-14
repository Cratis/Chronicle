// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_the_recorded_revision_is_erased : given.a_rotated_key
{
    void Establish()
    {
        _encrypted = Compliance.Encryption.EncryptEnvelope("\"value\""u8.ToArray(), _old, 2);
        _fence = new EncryptionKeyErasure(2, [], false);
    }

    async Task Because() => await Release();

    [Fact] void should_not_fetch_erased_material() => _storage.DidNotReceiveWithAnyArgs().TryGetAtRevisionFor(default, default, default, default);
    [Fact] void should_preserve_erased_semantics() => _released.ShouldBeNull();
}
