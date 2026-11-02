// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_WebhookMediator.when_reading_a_result;

public class with_an_oversized_body : given.a_batch_delivery
{
    WebhookDeliveryResult _result;

    void Establish() => _handler.ResponseBody = new string('x', (1024 * 1024) + 1);

    async Task Because() => _result = await _mediator.DeliverEnvelope(_request);

    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_acknowledge_nothing() => _result.LastHandled.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_carry_the_malformed_result() => _result.Error.ShouldBeOfExactType<DeliveryResultMalformed>();
    [Fact] void should_not_quote_the_body() => _result.Error!.ToString().ShouldNotContain(new string('x', 100));
    [Fact] void should_retry() => _result.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.Retry);
    [Fact] void should_have_no_deferral() => _result.Deferral.ShouldBeNull();
}
