// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_an_identifier_containing_a_dot : given.a_verifier
{
    void Because() => _result = Verify("msg.1", Timestamp, KnownSignature);

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_an_invalid_identifier() => _result.Failure.ShouldEqual(WebhookSignatureFailure.InvalidId);
}
