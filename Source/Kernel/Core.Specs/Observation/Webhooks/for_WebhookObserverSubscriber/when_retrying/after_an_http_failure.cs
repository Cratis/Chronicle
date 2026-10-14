// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_retrying;

public class after_an_http_failure : given.a_webhook_delivery
{
    ObserverSubscriberResult _failedResult;
    void Establish() => _receiver.StatusCode = HttpStatusCode.ServiceUnavailable;

    async Task Because()
    {
        _failedResult = await _subscriber.OnNext(_partition, _events, new(null));
        _receiver.StatusCode = HttpStatusCode.OK;
        _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null));
    }

    [Fact] void should_fail_without_acknowledging_the_first_attempt() => _failedResult.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_deliver_identical_payloads_on_retry() => _receiver.Payloads[1].ShouldEqual(_receiver.Payloads[0]);
    [Fact] void should_preserve_the_pinned_payload() => _receiver.Payloads[1].ShouldEqual(Fixture("pinned"));
    [Fact] void should_acknowledge_the_successful_retry() => _deliveryResult.LastSuccessfulObservation.Value.ShouldEqual(44ul);
}
