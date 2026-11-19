// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_a_signature_from_the_second_of_two_keys : given.a_verifier
{
    void Establish() => _verifier = new(new WebhookSigningPublicKey[] { new("first", WebhookSigningKeyStatus.Active, PublicKey, DateTimeOffset.UnixEpoch, null), new("second", WebhookSigningKeyStatus.Overlap, OtherPublicKey, DateTimeOffset.UnixEpoch, null) }, timeProvider: _time);

    void Because() => _result = Verify(Id, Timestamp, Sign(OtherSeed, Id, Timestamp, Payload));

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_report_the_second_key() => _result.KeyId.ShouldEqual("second");
}
