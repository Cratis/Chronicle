// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_WebhookMediator.when_reading_a_result;

public class with_a_failure_at_the_last_event : given.a_batch_delivery
{
    WebhookDeliveryResult _result;
    ObserverSubscriberResult _downstream;

    void Establish() => _handler.ResponseBody = Result("failed", 45);

    async Task Because()
    {
        _result = await _mediator.DeliverEnvelope(_request);

        // The failed-subscriber seam used by HandleEventsForPartition must not record a prefix.
        _downstream = ObserverSubscriberResult.Failed(_result.LastHandled);
    }

    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_acknowledge_nothing() => _result.LastHandled.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_carry_the_malformed_result() => _result.Error.ShouldBeOfExactType<DeliveryResultMalformed>();
    [Fact] void should_retry() => _result.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.Retry);
    [Fact] void should_have_no_deferral() => _result.Deferral.ShouldBeNull();
    [Fact] void should_fail_downstream() => _downstream.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_record_no_progress_downstream() => _downstream.HandledAnyEvents.ShouldBeFalse();
    [Fact] void should_keep_the_downstream_checkpoint_unavailable() => _downstream.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
}
