// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_Encryption;

public class when_reading_a_v2_envelope : given.a_v2_value
{
    byte[] _plaintext;
    EncryptionKeyRevision _revision;
    bool _recorded;

    void Because()
    {
        _recorded = _encryption.TryGetKeyRevision(_encrypted, out _revision);
        _plaintext = _encryption.Decrypt(_encrypted, _key);
    }

    [Fact] void should_recognize_the_envelope() => _encryption.IsEncrypted(_encrypted).ShouldBeTrue();
    [Fact] void should_recover_the_plaintext() => _plaintext.SequenceEqual("value"u8.ToArray()).ShouldBeTrue();
    [Fact] void should_have_a_recorded_revision() => _recorded.ShouldBeTrue();
    [Fact] void should_read_the_recorded_revision() => _revision.ShouldEqual((EncryptionKeyRevision)7u);
}
