// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_the_timestamp_differs_from_the_v1a_signed_one : given.a_mixed_key_fixture
{
    bool _result;

    void Because() => _result = WebhookSignatureVerification.Verify(MessageId, Timestamp + 1, _payload, _newEd25519Signature, [], [_newPublicKey], null);

    [Fact] void should_reject() => _result.ShouldBeFalse();
}
