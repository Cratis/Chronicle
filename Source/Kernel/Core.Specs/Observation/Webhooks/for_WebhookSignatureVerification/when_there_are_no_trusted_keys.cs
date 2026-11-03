// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_there_are_no_trusted_keys : given.a_verification_fixture
{
    bool _result;

    void Because() => _result = Verify(_newSignature, "key_new");

    [Fact] void should_reject() => _result.ShouldBeFalse();
}
