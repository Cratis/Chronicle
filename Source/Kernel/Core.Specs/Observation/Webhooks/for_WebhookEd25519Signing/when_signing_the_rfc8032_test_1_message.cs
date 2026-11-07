// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookEd25519Signing;

public class when_signing_the_rfc8032_test_1_message : given.an_ed25519_fixture
{
    byte[] _signature;

    void Because() => _signature = WebhookEd25519Signing.ComputeSignature([], _test1Seed);

    [Fact] void should_match_the_published_signature() => _signature.ShouldEqual(_test1Signature);
}
