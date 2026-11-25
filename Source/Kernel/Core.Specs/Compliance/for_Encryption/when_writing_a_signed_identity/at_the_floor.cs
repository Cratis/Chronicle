// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Compliance.for_Encryption.when_writing_a_signed_identity;

public class at_the_floor : given.a_signed_value
{
    byte[] _decrypted;
    EncryptionKeyRevision _revision;
    bool _hasRevision;

    void Because()
    {
        _decrypted = _encryption.Decrypt(_encrypted, _key);
        _hasRevision = _encryption.TryGetKeyRevision(_encrypted, out _revision);
    }

    [Fact] void should_emit_v3() => _encrypted[4].ShouldEqual((byte)3);
    [Fact] void should_record_the_revision() => (_hasRevision && _revision == 7u).ShouldBeTrue();
    [Fact] void should_round_trip_the_value() => Encoding.UTF8.GetString(_decrypted).ShouldEqual("value");
    [Fact] void should_be_recognized_as_encrypted() => _encryption.IsEncrypted(_encrypted).ShouldBeTrue();
}
