// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_delivering;

public class with_a_signed_redirect : given.a_signed_delivery
{
    void Establish() => _receiver.Status = HttpStatusCode.Found;
    async Task Because() => _result = await Deliver(_identity);

    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_replay_to_the_redirect_location() => _receiver.Bodies.Count.ShouldEqual(1);
    [Fact] void should_sign_the_original_attempt() => _receiver.Verifies(0).ShouldBeTrue();
}
