// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_v1_and_v1a_signatures_are_both_present_and_only_the_public_key_is_trusted : given.a_mixed_key_fixture
{
    bool _result;

    void Because() => _result = VerifyMixed($"{_oldSignature} {_newEd25519Signature}", _newPublicKey);

    [Fact] void should_accept() => _result.ShouldBeTrue();
}
