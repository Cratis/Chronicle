// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookEd25519Signing;

public class when_verifying_with_the_wrong_signature_length : given.an_ed25519_fixture
{
    bool _shortResult;
    bool _longResult;

    void Because()
    {
        _shortResult = WebhookEd25519Signing.VerifySignature([], new byte[63], _test1PublicKey);
        _longResult = WebhookEd25519Signing.VerifySignature([], new byte[65], _test1PublicKey);
    }

    [Fact] void should_reject_a_short_signature() => _shortResult.ShouldBeFalse();
    [Fact] void should_reject_a_long_signature() => _longResult.ShouldBeFalse();
}
