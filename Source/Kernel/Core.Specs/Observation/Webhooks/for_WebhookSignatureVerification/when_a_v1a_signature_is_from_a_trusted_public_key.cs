// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_a_v1a_signature_is_from_a_trusted_public_key : given.a_mixed_key_fixture
{
    bool _result;

    void Because() => _result = VerifyMixed(_newEd25519Signature, _newPublicKey);

    [Fact] void should_accept() => _result.ShouldBeTrue();
}
