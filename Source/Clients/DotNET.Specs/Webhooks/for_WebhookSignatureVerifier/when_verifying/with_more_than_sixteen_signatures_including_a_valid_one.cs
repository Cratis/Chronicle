// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.Webhooks.for_WebhookSignatureVerifier.when_verifying;

public class with_more_than_sixteen_signatures_including_a_valid_one : given.a_verifier
{
    void Because() => _result = Verify(Id, Timestamp, string.Concat(Enumerable.Repeat("v1,AAAA ", 20)) + KnownSignature);

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
}
