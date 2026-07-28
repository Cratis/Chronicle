// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_Encryption.when_round_tripping_a_value;

public class with_a_multi_kilobyte_binary_value : given.a_key
{
    static readonly byte[] _original = Enumerable.Range(0, 8192).Select(i => (byte)(i % 256)).ToArray();
    byte[] _encrypted;
    byte[] _decrypted;

    void Because()
    {
        // An image-sized binary PII payload is far beyond anything raw RSA can encrypt — it must still round-trip.
        _encrypted = _encryption.Encrypt(_original, _key);
        _decrypted = _encryption.Decrypt(_encrypted, _key);
    }

    [Fact] void should_round_trip_to_the_original() => _decrypted.SequenceEqual(_original).ShouldBeTrue();
}
