// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_v2_authentication_fails : given.a_rotated_key
{
    void Establish()
    {
        _encrypted = Compliance.Encryption.EncryptEnvelope("\"value\""u8.ToArray(), _old, 2);
        _encrypted[^1] ^= 1;
    }

    async Task Because() => _failure = await Catch.Exception(Release);

    [Fact] void should_preserve_the_authentication_failure() => _failure.ShouldBeOfExactType<AuthenticationTagMismatchException>();
    [Fact] void should_not_probe_any_other_revision() => _storage.ReceivedWithAnyArgs(1).TryGetAtRevisionFor(Address.Item1, Address.Item2, Address.Item3, _old.Revision);
    [Fact] void should_not_read_latest() => _storage.DidNotReceiveWithAnyArgs().TryGetLatestFor(Address.Item1, Address.Item2, Address.Item3);
}
