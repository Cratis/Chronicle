// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_delivering;

public class with_a_signed_retry : given.a_signed_delivery
{
    async Task Because()
    {
        await Deliver(_identity);
        _clock.Advance(TimeSpan.FromSeconds(5));
        _result = await Deliver(_identity);
    }

    [Fact] void should_keep_the_message_id() => _receiver.Headers[1]["webhook-id"].ShouldEqual(_receiver.Headers[0]["webhook-id"]);
    [Fact] void should_refresh_the_attempt_timestamp() => _receiver.Headers[1]["webhook-timestamp"].ShouldContainOnly("5");
    [Fact] void should_preserve_the_body() => _receiver.Bodies[1].ShouldEqual(_receiver.Bodies[0]);
    [Fact] void should_refresh_the_signature() => (_receiver.Headers[1]["webhook-signature"].Single() != _receiver.Headers[0]["webhook-signature"].Single()).ShouldBeTrue();
    [Fact] void should_verify_both_attempts() => (_receiver.Verifies(0) && _receiver.Verifies(1)).ShouldBeTrue();
}
