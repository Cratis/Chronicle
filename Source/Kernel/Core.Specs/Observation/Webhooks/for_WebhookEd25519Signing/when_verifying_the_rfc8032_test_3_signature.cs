// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookEd25519Signing;

public class when_verifying_the_rfc8032_test_3_signature : given.an_ed25519_fixture
{
    bool _result;

    void Because() => _result = WebhookEd25519Signing.VerifySignature(_test3Message, _test3Signature, _test3PublicKey);

    [Fact] void should_accept_the_published_signature() => _result.ShouldBeTrue();
}
