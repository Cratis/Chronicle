// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_a_malformed_entry_accompanies_a_valid_one : given.a_verification_fixture
{
    bool _result;

    void Because() => _result = Verify($"v1,%%%not-base64%%% {_newSignature}", "key_new", _newSecret);

    [Fact] void should_accept() => _result.ShouldBeTrue();
}
