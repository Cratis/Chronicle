// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition;

public class when_resolving_a_webhook_subscriber : given.a_performing_job_step
{
    ObserverSubscription _subscription;
    ObserverSubscriberKey _result;

    void Establish() => _subscription = new(
        "observer-id",
        new ObserverKey("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log),
        [],
        typeof(IWebhookObserverSubscriber),
        SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1));

    void Because() => _result = (ObserverSubscriberKey)typeof(HandleEventsForPartition)
        .GetMethod("GetObserverSubscriberKey", BindingFlags.Instance | BindingFlags.NonPublic)
        .Invoke(_jobStep, [_subscription, (Key)"some-partition"]);

    [Fact] void should_use_the_same_activation_as_live_delivery() =>
        _result.ShouldEqual(_subscription.GetSubscriberKeyFor("some-partition", SiloAddress.Zero));
}
