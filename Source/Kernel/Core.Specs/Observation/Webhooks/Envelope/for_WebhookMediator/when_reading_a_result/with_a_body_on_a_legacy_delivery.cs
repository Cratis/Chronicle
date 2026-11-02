// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_WebhookMediator.when_reading_a_result;

public class with_a_body_on_a_legacy_delivery : given.a_batch_delivery
{
    WebhookDeliveryResult _result;

    void Establish() => _handler.ResponseBody = "this is not JSON";

    async Task Because() => _result = await _mediator.Deliver(_request.EventStore, _request.Target, _request.Partition, _request.Events);

    [Fact] void should_preserve_legacy_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_the_last_event() => _result.LastHandled.Value.ShouldEqual(45UL);
    [Fact] void should_have_no_error() => _result.Error.ShouldBeNull();
}
