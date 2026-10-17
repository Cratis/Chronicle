// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_observer_is_quarantined : given.a_projections_manager_grain
{
    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _state.Projections = [CreateDefinition("the-projection", "the-read-model") with { SubscribesToAllEvents = true }];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _observerGrain.IsSubscribed().Returns(false);
        _observerGrain.GetState().Returns(new ObserverState { RunningState = ObserverRunningState.Quarantined });
        _observerGrain.SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: false)
            .Returns(_ =>
            {
                _observerGrain.IsSubscribed().Returns(true);
                _observerGrain.GetState().Returns(new ObserverState());
                return Task.CompletedTask;
            });
    }

    async Task Because()
    {
        await _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);
        await _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);
    }

    [Fact] async Task should_leave_the_observer_unsubscribed() => (await _observerGrain.IsSubscribed()).ShouldBeFalse();
    [Fact] async Task should_preserve_quarantine() => (await _observerGrain.GetState()).RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_not_attempt_ordinary_subscription() => await _observerGrain.DidNotReceive().SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: Arg.Any<bool>());
    [Fact] async Task should_not_set_the_projection_definition() => await _projectionGrain.DidNotReceiveWithAnyArgs().SetDefinition(default!);
}
