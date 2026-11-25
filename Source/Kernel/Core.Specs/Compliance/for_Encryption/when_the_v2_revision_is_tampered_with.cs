// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Compliance.for_Encryption;

public class when_the_v2_revision_is_tampered_with : given.a_v2_value
{
    Exception _failure;

    void Because()
    {
        _encrypted[8] = 8;
        _failure = Catch.Exception(() => _encryption.Decrypt(_encrypted, _key));
    }

    [Fact] void should_authenticate_the_revision() => _failure.ShouldBeOfExactType<AuthenticationTagMismatchException>();
}
