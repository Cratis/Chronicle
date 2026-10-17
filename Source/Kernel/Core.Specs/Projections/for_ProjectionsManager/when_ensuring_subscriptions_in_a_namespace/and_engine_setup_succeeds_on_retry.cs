// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

public class and_engine_setup_succeeds_on_retry : given.a_projections_manager_grain
{
    bool _subscribed;
    bool _subscribedAfterSetupFailed;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _state.Projections = [CreateDefinition("the-projection", "the-read-model") with { SubscribesToAllEvents = true }];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _projectionsServiceClient.NamespaceAdded(EventStore, EventStoreNamespaceName.Default)
            .Returns(Task.FromException(new TimeoutException("Engine namespace setup failed")), Task.CompletedTask);
        _observerGrain.IsSubscribed().Returns(_ => _subscribed);
        _observerGrain.SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: false)
            .Returns(_ =>
            {
                _subscribed = true;
                return Task.CompletedTask;
            });
    }

    async Task Because()
    {
        await _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);
        _subscribedAfterSetupFailed = _subscribed;
        await _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);
        await _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);
    }

    [Fact] void should_subscribe_even_if_engine_setup_failed() => _subscribedAfterSetupFailed.ShouldBeTrue();
    [Fact] async Task should_retry_engine_setup_despite_the_healthy_subscription() => await _projectionsServiceClient.Received(2).NamespaceAdded(EventStore, EventStoreNamespaceName.Default);
    [Fact] async Task should_not_repeat_the_successful_subscription() => await _observerGrain.Received(1).SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: false);
}
