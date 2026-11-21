// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_only_an_hmac_signature : given.a_verifier
{
    void Because() => _result = Verify(Id, Timestamp, "v1,AAAA");

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_no_supported_signature() => _result.Failure.ShouldEqual(WebhookSignatureFailure.NoSupportedSignature);
}
