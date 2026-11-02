// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_WebhookMediator.when_reading_a_result;

public class with_no_content : given.a_batch_delivery
{
    WebhookDeliveryResult _result;

    void Establish()
    {
        _handler.Status = HttpStatusCode.NoContent;
        _handler.ResponseBody = "not a result";
    }

    async Task Because() => _result = await _mediator.DeliverEnvelope(_request);

    [Fact] void should_acknowledge_the_whole_batch() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_the_last_event() => _result.LastHandled.Value.ShouldEqual(45UL);
}
