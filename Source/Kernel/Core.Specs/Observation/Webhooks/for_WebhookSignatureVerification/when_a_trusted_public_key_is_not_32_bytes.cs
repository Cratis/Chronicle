// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_a_trusted_public_key_is_not_32_bytes : given.a_mixed_key_fixture
{
    bool _shortResult;
    bool _longResult;
    bool _withValidKey;

    void Because()
    {
        _shortResult = VerifyMixed(_newEd25519Signature, new byte[31]);
        _longResult = VerifyMixed(_newEd25519Signature, new byte[33]);
        _withValidKey = VerifyMixed(_newEd25519Signature, new byte[31], _newPublicKey);
    }

    [Fact] void should_reject_a_short_key_without_throwing() => _shortResult.ShouldBeFalse();
    [Fact] void should_reject_a_long_key_without_throwing() => _longResult.ShouldBeFalse();
    [Fact] void should_not_prevent_matching_a_valid_key() => _withValidKey.ShouldBeTrue();
}
