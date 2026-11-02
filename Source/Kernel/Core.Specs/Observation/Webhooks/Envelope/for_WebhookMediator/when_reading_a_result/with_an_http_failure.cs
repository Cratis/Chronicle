// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_WebhookMediator.when_reading_a_result;

public class with_an_http_failure : given.a_batch_delivery
{
    WebhookDeliveryResult _result;

    void Establish()
    {
        _handler.Status = HttpStatusCode.TooManyRequests;
        _handler.ResponseBody = Result("success", 45);
    }

    async Task Because() => _result = await _mediator.DeliverEnvelope(_request);

    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_acknowledge_nothing() => _result.LastHandled.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_preserve_the_http_error() => _result.Error.ShouldBeOfExactType<WebhookDeliveryFailed>();
    [Fact] void should_retry() => _result.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.Retry);
    [Fact] void should_have_no_deferral_without_retry_after() => _result.Deferral.ShouldBeNull();
}
