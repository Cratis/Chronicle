// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Compliance.for_Encryption.when_writing_a_signed_identity;

public class below_the_floor : given.a_signed_value
{
    byte[] _written;

    void Because() => _written = CreateEncryptionAt(new SemanticVersion(19, 178, 0)).Encrypt("value"u8.ToArray(), _key, 7, _address);

    [Fact] void should_emit_v1() => _written[4].ShouldEqual((byte)1);
}
