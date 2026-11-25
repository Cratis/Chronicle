// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.for_Encryption;

public class when_writing_during_the_reader_first_rollout : given.a_key
{
    byte[] _encrypted;
    byte[] _encryptedAtAnAddress;

    void Because()
    {
        _encrypted = _encryption.Encrypt("value"u8.ToArray(), _key, 7);
        _encryptedAtAnAddress = _encryption.Encrypt("value"u8.ToArray(), _key, 7, new(Concepts.EventStoreName.NotSet, Concepts.EventStoreNamespaceName.NotSet, "subject"));
    }

    [Fact] void should_emit_v1_despite_a_concrete_revision() => _encrypted[4].ShouldEqual((byte)1);
    [Fact] void should_not_advertise_a_revision_in_a_v1_envelope() => _encryption.TryGetKeyRevision(_encrypted, out _).ShouldBeFalse();
    [Fact] void should_emit_v1_when_the_cluster_releases_are_not_known() => _encryptedAtAnAddress[4].ShouldEqual((byte)1);
}
