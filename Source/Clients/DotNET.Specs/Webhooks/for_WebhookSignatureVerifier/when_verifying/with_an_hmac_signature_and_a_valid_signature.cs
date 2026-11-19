// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_an_hmac_signature_and_a_valid_signature : given.a_verifier
{
    void Because() => _result = Verify(Id, Timestamp, $"v1,AAAA {KnownSignature}");

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
}
