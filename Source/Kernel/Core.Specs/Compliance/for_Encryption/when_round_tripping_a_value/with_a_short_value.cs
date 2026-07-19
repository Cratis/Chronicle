// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Compliance.for_Encryption.when_round_tripping_a_value;

public class with_a_short_value : given.a_key
{
    const string Original = "sensitive";
    byte[] _encrypted;
    byte[] _decrypted;

    void Because()
    {
        _encrypted = _encryption.Encrypt(Encoding.UTF8.GetBytes(Original), _key);
        _decrypted = _encryption.Decrypt(_encrypted, _key);
    }

    [Fact] void should_round_trip_to_the_original() => Encoding.UTF8.GetString(_decrypted).ShouldEqual(Original);
    [Fact] void should_not_store_the_value_in_plaintext() => Encoding.UTF8.GetString(_encrypted).ShouldNotEqual(Original);
}
