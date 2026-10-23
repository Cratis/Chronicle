// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_resolving_the_subscriber;

public class for_a_kernel_projection : given.a_performing_job_step
{
    ObserverSubscription _subscription;
    ObserverSubscriberKey _result;

    void Establish() => _subscription = new(
        "observer-id",
        new ObserverKey("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log),
        [],
        typeof(IProjectionObserverSubscriber),
        SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1));

    void Because() => _result = (ObserverSubscriberKey)typeof(HandleEventsForPartition)
        .GetMethod("GetObserverSubscriberKey", BindingFlags.Instance | BindingFlags.NonPublic)
        .Invoke(_jobStep, [_subscription, (Key)"some-partition"]);

    [Fact] void should_keep_the_live_delivery_identity() => _result.ShouldEqual(_subscription.GetSubscriberKeyFor("some-partition", _subscription.SiloAddress));
    [Fact] void should_keep_the_subscriptions_silo() => _result.SiloAddress.ShouldEqual(_subscription.SiloAddress.ToParsableString());
}
