// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Compliance.for_Encryption;

public class when_a_prefixed_envelope_is_malformed : given.a_key
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(99)]
    void should_reject_without_falling_back_to_legacy_rsa(byte version)
    {
        byte[] bytes = [.. "CENV"u8.ToArray(), version];
        _encryption.IsEncrypted(bytes).ShouldBeTrue();
        Catch.Exception(() => _encryption.Decrypt(bytes, _key)).ShouldBeOfExactType<CryptographicException>();
    }
}
