// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_an_overlap_key_that_expires_after_construction : given.a_verifier
{
    void Establish() => _verifier = new(new WebhookSigningPublicKey[] { new("old", WebhookSigningKeyStatus.Overlap, PublicKey, DateTimeOffset.UnixEpoch, DateTimeOffset.FromUnixTimeSeconds(UnixNow + 10)) }, tolerance: TimeSpan.FromHours(1), timeProvider: _time);

    void Because()
    {
        _time.Advance(11);
        _result = Verify(Id, Timestamp, KnownSignature);
    }

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
}
