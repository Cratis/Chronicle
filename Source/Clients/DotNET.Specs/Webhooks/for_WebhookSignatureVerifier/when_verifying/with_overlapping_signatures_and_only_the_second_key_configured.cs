// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_overlapping_signatures_and_only_the_second_key_configured : given.a_verifier
{
    void Establish() => _verifier = new([OtherPublicKey], timeProvider: _time);

    void Because() => _result = Verify(Id, Timestamp, $"{KnownSignature} {Sign(OtherSeed, Id, Timestamp, Payload)}");

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_report_the_configured_key() => _result.KeyId.ShouldEqual(OtherPublicKey);
}
