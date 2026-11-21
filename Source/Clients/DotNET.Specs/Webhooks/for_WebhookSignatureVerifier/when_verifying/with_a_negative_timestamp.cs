// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_a_negative_timestamp : given.a_verifier
{
    void Because() => _result = Verify(Id, "-1", KnownSignature);

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_an_invalid_timestamp() => _result.Failure.ShouldEqual(WebhookSignatureFailure.InvalidTimestamp);
}
