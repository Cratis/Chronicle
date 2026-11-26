// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_TokenHttpMessageHandler;

public class when_a_signed_request_is_retried_after_a_401 : given.a_signed_token_delivery
{
    Task Because() => Deliver();

    [Fact] void should_deliver_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_send_twice() => _receiver.Bodies.Count.ShouldEqual(2);
    [Fact] void should_preserve_the_body() => _receiver.Bodies[1].ShouldEqual(_receiver.Bodies[0]);
    [Fact] void should_preserve_the_id() => _receiver.Headers[1]["webhook-id"].ShouldEqual(_receiver.Headers[0]["webhook-id"]);
    [Fact] void should_refresh_the_timestamp() => _receiver.Headers[1]["webhook-timestamp"].ShouldContainOnly("1005");
    [Fact] void should_refresh_the_signature() => _receiver.Headers[1]["webhook-signature"].Single().ShouldNotEqual(_receiver.Headers[0]["webhook-signature"].Single());
    [Fact] void should_verify_the_first_attempt() => _receiver.Verifies(0).ShouldBeTrue();
    [Fact] void should_verify_the_retry() => _receiver.Verifies(1).ShouldBeTrue();
    [Fact] void should_use_the_refreshed_token() => _receiver.Headers[1]["Authorization"].ShouldContainOnly("Bearer token-2");
    [Fact] void should_keep_one_advisory_key_id() => _receiver.Headers[1]["Cratis-Key-Id"].ShouldContainOnly("active");
}
