// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Observation.for_ObserverSubscription;

public class when_asking_whether_the_projection_collapses_event_sources : Specification
{
    static readonly ObserverKey _observerKey = new(
        "3ec5dbcb-3f7c-4a2b-9d6f-4a1f2b0c8e11",
        "some-event-store",
        "some-namespace",
        EventSequenceId.Log);

    [Fact] void should_recognize_the_collapsing_projection_subscriber() =>
        SubscriptionFor(typeof(ICollapsingProjectionObserverSubscriber)).IsCollapsingProjection.ShouldBeTrue();

    [Fact] void should_not_recognize_the_event_source_keyed_projection_subscriber() =>
        SubscriptionFor(typeof(IProjectionObserverSubscriber)).IsCollapsingProjection.ShouldBeFalse();

    [Fact] void should_not_recognize_the_reducer_subscriber() =>
        SubscriptionFor(typeof(IReducerObserverSubscriber)).IsCollapsingProjection.ShouldBeFalse();

    static ObserverSubscription SubscriptionFor(Type subscriberType) => new(
        _observerKey.ObserverId,
        _observerKey,
        [],
        subscriberType,
        SiloAddress.FromParsableString("127.0.0.1:11111@1"));
}
