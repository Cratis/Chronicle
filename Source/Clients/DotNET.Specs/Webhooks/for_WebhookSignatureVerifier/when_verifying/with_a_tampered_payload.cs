// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_a_tampered_payload : given.a_verifier
{
    void Because() => _result = Verify(Id, Timestamp, KnownSignature, System.Text.Encoding.UTF8.GetBytes("{\"test\": 2432232315}"));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_a_signature_mismatch() => _result.Failure.ShouldEqual(WebhookSignatureFailure.SignatureMismatch);
}
