// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_the_receiver_trusts_only_the_new_key_during_overlap : given.a_verification_fixture
{
    bool _result;

    void Because() => _result = Verify($"{_oldSignature} {_newSignature}", "key_new", _newSecret);

    [Fact] void should_accept() => _result.ShouldBeTrue();
}
