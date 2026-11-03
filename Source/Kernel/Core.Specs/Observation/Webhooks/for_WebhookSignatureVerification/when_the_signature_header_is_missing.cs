// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_the_signature_header_is_missing : given.a_verification_fixture
{
    bool _result;

    void Because() => _result = Verify(null, "key_new", _newSecret);

    [Fact] void should_reject() => _result.ShouldBeFalse();
}
