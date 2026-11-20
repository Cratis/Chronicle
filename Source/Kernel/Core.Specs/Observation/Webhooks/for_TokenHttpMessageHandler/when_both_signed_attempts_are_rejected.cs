// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Webhooks.for_TokenHttpMessageHandler;

public class when_both_signed_attempts_are_rejected : given.a_signed_token_delivery
{
    void Establish() => _receiver.FinalStatus = HttpStatusCode.Unauthorized;
    Task Because() => Deliver();

    [Fact] void should_stop_after_two_attempts() => _receiver.Bodies.Count.ShouldEqual(2);
    [Fact] void should_verify_the_retry() => _receiver.Verifies(1).ShouldBeTrue();
    [Fact] void should_return_the_final_rejection() => ((WebhookDeliveryFailed)_result.Error!).StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
}
