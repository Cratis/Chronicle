// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_a_timestamp_exactly_at_the_tolerance : given.a_verifier
{
    void Establish() => _time.Advance(300);

    void Because() => _result = Verify(Id, Timestamp, KnownSignature);

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
}
