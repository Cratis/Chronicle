// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Compliance.for_Encryption.when_round_tripping_a_value;

public class with_a_value_larger_than_the_rsa_key_size : given.a_key
{
    static readonly string _original = new('a', 2000);
    byte[] _encrypted;
    byte[] _decrypted;

    void Because()
    {
        // Raw RSA-2048 with PKCS#1 v1.5 caps plaintext at ~245 bytes, so a realistic free-text PII field
        // (e.g. a 2000-character validator) used to throw here. Hybrid encryption must round-trip it.
        _encrypted = _encryption.Encrypt(Encoding.UTF8.GetBytes(_original), _key);
        _decrypted = _encryption.Decrypt(_encrypted, _key);
    }

    [Fact] void should_round_trip_to_the_original() => Encoding.UTF8.GetString(_decrypted).ShouldEqual(_original);
}
