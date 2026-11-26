// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_an_active_key_without_an_overlap_end : given.a_verifier
{
    void Establish() => _verifier = new(new WebhookSigningPublicKey[] { new("current", WebhookSigningKeyStatus.Active, PublicKey, DateTimeOffset.UnixEpoch, null) }, timeProvider: _time);

    void Because() => _result = Verify(Id, Timestamp, KnownSignature);

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
}
