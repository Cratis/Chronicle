// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class without_the_message_identifier : given.a_verifier
{
    WebhookSignatureVerificationResult _emptyResult;

    void Because()
    {
        _result = Verify(null, Timestamp, KnownSignature);
        _emptyResult = Verify(string.Empty, Timestamp, KnownSignature);
    }

    [Fact] void should_not_be_valid_when_null() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_a_missing_header_when_null() => _result.Failure.ShouldEqual(WebhookSignatureFailure.MissingHeader);
    [Fact] void should_not_be_valid_when_empty() => _emptyResult.IsValid.ShouldBeFalse();
    [Fact] void should_report_a_missing_header_when_empty() => _emptyResult.Failure.ShouldEqual(WebhookSignatureFailure.MissingHeader);
}
