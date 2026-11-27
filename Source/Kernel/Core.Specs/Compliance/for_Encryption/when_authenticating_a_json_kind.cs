// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Releases;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_Encryption;

public class when_authenticating_a_json_kind : given.a_signed_value
{
    byte[] _typed;
    byte[] _local;
    byte[] _legacy;
    Exception _tampered;
    Exception _tamperedSignature;
    Exception _unknownKind;
    Exception _belowFloor;

    void Because()
    {
        var writer = CreateEncryptionAt(ClusterReleaseBoundary.TypedProtectedValues);
        var pair = new EncryptionKeyAtRevision(_key, 7);
        _typed = writer.Encrypt("42"u8.ToArray(), pair, _address, ProtectedValueKind.JsonNumber);
        _local = new Encryption().Encrypt("42"u8.ToArray(), pair, _address, ProtectedValueKind.JsonString);
        _legacy = writer.Encrypt("42"u8.ToArray(), _key, 7, _address);
        _belowFloor = Catch.Exception(() => _encryption.Encrypt("42"u8.ToArray(), pair, _address, ProtectedValueKind.JsonNumber));
        var tampered = (byte[])_typed.Clone();
        tampered[17] = (byte)ProtectedValueKind.JsonString;
        _tampered = Catch.Exception(() => writer.Decrypt(tampered, _key));
        _tamperedSignature = Catch.Exception(() => EnvelopeSigning.Verify(EncryptionEnvelope.Read(tampered), _address));
        tampered[17] = 255;
        _unknownKind = Catch.Exception(() => writer.Decrypt(tampered, _key));
    }

    [Fact] void should_write_v4_at_the_floor() => _typed[4].ShouldEqual((byte)4);
    [Fact] void should_write_v4_locally() => _local[4].ShouldEqual((byte)4);
    [Fact] void should_keep_untyped_writes_at_v3() => _legacy[4].ShouldEqual((byte)3);
    [Fact] void should_read_v3_with_the_new_reader() => _encryption.Decrypt(_encrypted, _key).ShouldEqual("value"u8.ToArray());
    [Fact] void should_read_the_typed_payload() => _encryption.Decrypt(_typed, _key).ShouldEqual("42"u8.ToArray());
    [Fact] void should_record_the_kind() => EncryptionEnvelope.Read(_typed).Kind.ShouldEqual(ProtectedValueKind.JsonNumber);
    [Fact] void should_authenticate_the_kind_with_aes() => _tampered.ShouldBeOfExactType<AuthenticationTagMismatchException>();
    [Fact] void should_authenticate_the_kind_with_the_signature() => _tamperedSignature.ShouldBeOfExactType<EncryptionEnvelopeBindingFailed>();
    [Fact] void should_reject_an_unknown_kind() => _unknownKind.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_not_discard_a_requested_kind_below_the_floor() => _belowFloor.ShouldBeOfExactType<NotSupportedException>();
}
