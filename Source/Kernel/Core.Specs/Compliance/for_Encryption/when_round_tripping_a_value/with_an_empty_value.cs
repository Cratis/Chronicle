// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_Encryption.when_round_tripping_a_value;

public class with_an_empty_value : given.a_key
{
    readonly byte[] _original = [];
    byte[] _decrypted;

    void Because() => _decrypted = _encryption.Decrypt(_encryption.Encrypt(_original, _key), _key);

    [Fact] void should_round_trip_to_an_empty_value() => _decrypted.SequenceEqual(_original).ShouldBeTrue();
}
