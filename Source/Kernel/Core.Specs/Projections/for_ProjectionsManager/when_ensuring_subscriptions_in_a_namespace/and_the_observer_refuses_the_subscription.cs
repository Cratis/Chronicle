// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_observer_refuses_the_subscription : given.a_projections_manager_grain
{
    Exception _exception;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _state.Projections = [CreateDefinition("the-projection", "the-read-model") with { SubscribesToAllEvents = true }];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _observerGrain.IsSubscribed().Returns(false);
    }

    async Task Because()
    {
        _exception = await Catch.Exception(() => _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default));
        await _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);
    }

    [Fact] void should_not_fail_namespace_creation() => _exception.ShouldBeNull();
    [Fact] async Task should_retry_on_the_next_trigger() => await _observerGrain.Received(2).ReconcileSubscription<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<SiloAddress>(), true);
}
