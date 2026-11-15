// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookEd25519Signing;

public class when_the_message_identifier_contains_a_delimiter : given.an_ed25519_fixture
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => WebhookEd25519Signing.Sign("msg.part", 1700000000, [], _test1Seed));

    [Fact] void should_reject_the_identifier() => _result.ShouldBeOfExactType<InvalidWebhookMessageId>();
}
