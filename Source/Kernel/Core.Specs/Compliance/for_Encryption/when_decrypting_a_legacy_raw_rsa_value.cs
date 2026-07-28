// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Compliance.for_Encryption;

public class when_decrypting_a_legacy_raw_rsa_value : given.a_key
{
    const string Original = "legacy";
    byte[] _legacyEncrypted;
    byte[] _decrypted;

    void Establish()
    {
        // Values written by earlier Chronicle versions were encrypted with raw RSA over the whole payload.
        // Decrypt must still read them so previously stored PII does not become unreadable after the upgrade.
        using var provider = new RSACryptoServiceProvider(2048);
        provider.ImportRSAPublicKey(_key.Public, out _);
        _legacyEncrypted = provider.Encrypt(Encoding.UTF8.GetBytes(Original), RSAEncryptionPadding.Pkcs1);
    }

    void Because() => _decrypted = _encryption.Decrypt(_legacyEncrypted, _key);

    [Fact] void should_decrypt_to_the_original() => Encoding.UTF8.GetString(_decrypted).ShouldEqual(Original);
}
