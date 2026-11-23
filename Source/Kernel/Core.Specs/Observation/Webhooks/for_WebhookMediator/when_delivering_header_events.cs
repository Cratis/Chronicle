// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator;

public class when_delivering_header_events : given.a_signed_delivery
{
    string _body;

    void Establish() => _events = header_events.All();

    async Task Because()
    {
        _result = await Deliver(_identity);
        _body = Encoding.UTF8.GetString(_receiver.Bodies.Single());
    }

    [Fact] void should_deliver_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_redact_all_content_variants() => header_events.ContainsSentinel(_body).ShouldBeFalse();
    [Fact] void should_sign_the_public_body() => _receiver.Verifies(0).ShouldBeTrue();
    [Fact] void should_keep_header_names() => _body.Contains("X-First", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_internal_event_content() => _events.All(@event => header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
    [Fact] void should_keep_target_configuration() => _httpClientFactory.Received(1).Create(_target, Arg.Any<string?>());
}
