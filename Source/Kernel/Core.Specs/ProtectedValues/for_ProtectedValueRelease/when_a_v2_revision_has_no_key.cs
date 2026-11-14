// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_a_v2_revision_has_no_key : given.a_rotated_key
{
    void Establish()
    {
        _encrypted = Compliance.Encryption.EncryptEnvelope("\"value\""u8.ToArray(), _old, 2);
        _revisions.Clear();
    }

    async Task Because() => await Release();

    [Fact] void should_preserve_missing_key_semantics() => _released.ShouldBeNull();
    [Fact] void should_not_fall_back_to_latest() => _storage.DidNotReceiveWithAnyArgs().TryGetLatestFor(default, default, default);
}
