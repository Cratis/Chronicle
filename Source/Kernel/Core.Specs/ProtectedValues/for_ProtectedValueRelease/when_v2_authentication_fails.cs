// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_v2_authentication_fails : given.a_rotated_key
{
    void Establish()
    {
        _encrypted = Compliance.Encryption.EncryptEnvelope("\"value\""u8.ToArray(), _old, 2);
        _encrypted[^1] ^= 1;
    }

    async Task Because() => _failure = await Catch.Exception(Release);

    [Fact] void should_classify_the_failure() => _failure.ShouldBeOfExactType<EncryptionKeyUnrecoverable>();
    [Fact] void should_not_probe_any_other_revision() => _storage.ReceivedWithAnyArgs(1).TryGetAtRevisionFor(default, default, default, default);
    [Fact] void should_not_read_latest() => _storage.DidNotReceiveWithAnyArgs().TryGetLatestFor(default, default, default);
}
