// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_releasing_legacy_rsa_after_rotation : given.a_rotated_key
{
    void Establish()
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(_old.Key.Public, out _);
        _encrypted = rsa.Encrypt("\"value\""u8.ToArray(), RSAEncryptionPadding.Pkcs1);
    }

    async Task Because() => await Release();

    [Fact] void should_recover_the_legacy_value() => _released.GetValue<string>().ShouldEqual("value");
}
