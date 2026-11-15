// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_the_receiver_trusts_old_and_new_public_keys_during_overlap : given.a_mixed_key_fixture
{
    bool _oldResult;
    bool _newResult;

    void Because()
    {
        _oldResult = VerifyMixed(_oldEd25519Signature, _oldPublicKey, _newPublicKey);
        _newResult = VerifyMixed(_newEd25519Signature, _oldPublicKey, _newPublicKey);
    }

    [Fact] void should_accept_the_old_key() => _oldResult.ShouldBeTrue();
    [Fact] void should_accept_the_new_key() => _newResult.ShouldBeTrue();
}
