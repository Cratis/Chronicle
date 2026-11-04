// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_v1_and_v1a_signatures_are_both_present_and_only_the_hmac_secret_is_trusted : given.a_mixed_key_fixture
{
    bool _result;

    void Because() => _result = WebhookSignatureVerification.Verify(MessageId, Timestamp, _payload, $"{_newEd25519Signature} {_oldSignature}", [_oldSecret], [], null);

    [Fact] void should_accept() => _result.ShouldBeTrue();
}
