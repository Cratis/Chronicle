// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_the_known_vector : given.a_verifier
{
    void Because() => _result = Verify(Id, Timestamp, KnownSignature);

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_report_no_failure() => _result.Failure.ShouldEqual(WebhookSignatureFailure.None);
    [Fact] void should_report_the_matching_key() => _result.KeyId.ShouldEqual(PublicKey);
}
