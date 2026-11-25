// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Compliance.for_Encryption;

public class when_a_certificate_is_absent : given.a_forged_signed_value
{
    Task Because() => VerifyForgery(_ => []);

    [Fact] void should_have_a_valid_attacker_value_signature() => _attackerSignatureVerifies.ShouldBeTrue();
    [Fact] void should_reject_the_certificate() => _verificationFailure.ShouldBeOfExactType<EncryptionEnvelopeBindingFailed>();
    [Fact] void should_reject_the_malformed_envelope_on_release() => _releaseFailure.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_neither_release_nor_classify_the_value_as_erased() => _released.ShouldBeNull();
}
