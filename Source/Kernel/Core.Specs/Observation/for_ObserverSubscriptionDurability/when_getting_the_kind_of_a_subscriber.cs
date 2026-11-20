// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSubscriptionDurability;

public class when_getting_the_kind_of_a_subscriber : Specification
{
    class KernelReactor : Reactors.Kernel.IReactor
    {
        public void Initialize(Cratis.Chronicle.EventSequences.IEventSerializer eventSerializer) => throw new NotSupportedException();

        public Task<ObserverSubscriberResult> OnNext(IEnumerable<Concepts.Events.AppendedEvent> events) => throw new NotSupportedException();
    }

    [Fact] void should_recognize_a_client_reactor() => typeof(IReactorObserverSubscriber).GetSubscriptionKind().ShouldEqual(ObserverSubscriptionKind.ClientReactor);
    [Fact] void should_recognize_a_client_reactor_that_takes_pages() => typeof(IReactorPageObserverSubscriber).GetSubscriptionKind().ShouldEqual(ObserverSubscriptionKind.ClientReactor);
    [Fact] void should_recognize_a_client_reducer() => typeof(IReducerObserverSubscriber).GetSubscriptionKind().ShouldEqual(ObserverSubscriptionKind.ClientReducer);
    [Fact] void should_recognize_a_client_reducer_that_takes_pages() => typeof(IReducerPageObserverSubscriber).GetSubscriptionKind().ShouldEqual(ObserverSubscriptionKind.ClientReducer);
    [Fact] void should_recognize_a_projection() => typeof(IProjectionObserverSubscriber).GetSubscriptionKind().ShouldEqual(ObserverSubscriptionKind.Projection);
    [Fact] void should_recognize_a_collapsing_projection() => typeof(ICollapsingProjectionObserverSubscriber).GetSubscriptionKind().ShouldEqual(ObserverSubscriptionKind.CollapsingProjection);
    [Fact] void should_recognize_a_kernel_reactor() => typeof(Reactors.Kernel.IReactorObserverSubscriber<KernelReactor>).GetSubscriptionKind().ShouldEqual(ObserverSubscriptionKind.KernelReactor);
    [Fact] void should_recognize_a_webhook() => typeof(IWebhookObserverSubscriber).GetSubscriptionKind().ShouldEqual(ObserverSubscriptionKind.Webhook);
    [Fact] void should_recognize_an_event_store_subscription() => typeof(IEventStoreSubscriptionObserverSubscriber).GetSubscriptionKind().ShouldEqual(ObserverSubscriptionKind.EventStoreSubscription);
    [Fact] void should_not_recognize_the_subscriber_of_an_unsubscribed_observer() => typeof(NullObserverSubscriber).GetSubscriptionKind().ShouldBeNull();
}
