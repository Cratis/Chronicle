// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookEd25519Signing;

public class when_deriving_the_rfc8032_test_1_public_key : given.an_ed25519_fixture
{
    byte[] _publicKey;

    void Because() => _publicKey = WebhookEd25519Signing.DerivePublicKey(_test1Seed);

    [Fact] void should_match_the_published_public_key() => _publicKey.ShouldEqual(_test1PublicKey);
}
