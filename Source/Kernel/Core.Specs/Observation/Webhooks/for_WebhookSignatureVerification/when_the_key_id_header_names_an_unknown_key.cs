// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_the_key_id_header_names_an_unknown_key : given.a_verification_fixture
{
    bool _result;

    void Because() => _result = Verify(_newSignature, "key_unknown", _newSecret);

    [Fact] void should_accept() => _result.ShouldBeTrue();
}
