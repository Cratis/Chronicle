// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_a_signature_that_is_not_base64 : given.a_verifier
{
    void Because() => _result = Verify(Id, Timestamp, "v1a,!!!not-base64!!!");

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_a_signature_mismatch() => _result.Failure.ShouldEqual(WebhookSignatureFailure.SignatureMismatch);
}
