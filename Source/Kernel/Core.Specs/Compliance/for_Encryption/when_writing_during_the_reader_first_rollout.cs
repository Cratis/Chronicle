// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_Encryption;

public class when_writing_during_the_reader_first_rollout : given.a_key
{
    byte[] _encrypted;

    void Because() => _encrypted = _encryption.Encrypt("value"u8.ToArray(), _key, 7);

    [Fact] void should_pin_the_writer_to_v1() => Encryption.WriterVersion.ShouldEqual((byte)1);
    [Fact] void should_emit_v1_despite_a_concrete_revision() => _encrypted[4].ShouldEqual((byte)1);
    [Fact] void should_not_advertise_a_revision_in_a_v1_envelope() => _encryption.TryGetKeyRevision(_encrypted, out _).ShouldBeFalse();
}
