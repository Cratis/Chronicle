// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigning;

public class when_the_message_identifier_contains_a_delimiter : given.a_signing_fixture
{
    Exception _leading;
    Exception _embedded;
    Exception _trailing;

    void Because()
    {
        _leading = Catch.Exception(() => WebhookSigning.Sign(".msg", 1700000000, _payload, _secret));
        _embedded = Catch.Exception(() => WebhookSigning.Sign("msg.part", 1700000000, _payload, _secret));
        _trailing = Catch.Exception(() => WebhookSigning.Sign("msg.", 1700000000, _payload, _secret));
    }

    [Fact] void should_reject_a_leading_delimiter() => _leading.ShouldBeOfExactType<InvalidWebhookMessageId>();
    [Fact] void should_reject_an_embedded_delimiter() => _embedded.ShouldBeOfExactType<InvalidWebhookMessageId>();
    [Fact] void should_reject_a_trailing_delimiter() => _trailing.ShouldBeOfExactType<InvalidWebhookMessageId>();
}
