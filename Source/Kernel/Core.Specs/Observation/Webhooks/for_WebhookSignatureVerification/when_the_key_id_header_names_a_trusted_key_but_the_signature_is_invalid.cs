// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_the_key_id_header_names_a_trusted_key_but_the_signature_is_invalid : given.a_verification_fixture
{
    bool _result;

    void Because() => _result = Verify(_oldSignature, "key_trusted", _newSecret);

    [Fact] void should_reject() => _result.ShouldBeFalse();
}
