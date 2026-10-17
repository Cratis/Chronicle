// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

public class and_one_projection_cannot_subscribe : given.a_projections_manager_grain
{
    readonly Exception _failure = new TimeoutException("Projection setup failed");
    ProjectionDefinition _failing;
    ProjectionDefinition _healthy;
    IGrainFactory _grains;
    Observation.IObserver _failingObserver;
    Observation.IObserver _healthyObserver;
    Exception _exception;
    bool _subscribed;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _failing = CreateDefinition("failing-projection", "the-read-model") with { SubscribesToAllEvents = true };
        _healthy = CreateDefinition("healthy-projection", "the-read-model") with { SubscribesToAllEvents = true };
        _state.Projections = [_failing, _healthy];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _projectionGrain.SetDefinition(_failing).Returns(Task.FromException(_failure));
        _failingObserver = Substitute.For<Observation.IObserver>();
        _failingObserver.GetState().Returns(new ObserverState());
        _healthyObserver = Substitute.For<Observation.IObserver>();
        _healthyObserver.GetState().Returns(new ObserverState());
        _observersByKey[new ObserverKey(_failing.Identifier, EventStore, EventStoreNamespaceName.Default, _failing.EventSequenceId).ToString()] = _failingObserver;
        _observersByKey[new ObserverKey(_healthy.Identifier, EventStore, EventStoreNamespaceName.Default, _healthy.EventSequenceId).ToString()] = _healthyObserver;
        _healthyObserver.IsSubscribed().Returns(_ => _subscribed);
        _healthyObserver.ReconcileSubscription<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<SiloAddress>(), true)
            .Returns(_ =>
            {
                _subscribed = true;
                return Task.CompletedTask;
            });
        _grains = Substitute.For<IGrainFactory>();
        _grains.GetGrain<INamespaces>(EventStore).Returns(_namespaces);
        _grains.GetGrain<IProjectionsManager>(EventStore).Returns(_grain);
        _grains.GetGrain<ICapturesManager>(EventStore).Returns(Substitute.For<ICapturesManager>());
    }

    async Task Because()
    {
        _exception = await Catch.Exception(() => new EnsureNamespace(EventStore, EventStoreNamespaceName.Default).Handle(_grains));
        await _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);
    }

    [Fact] void should_allow_namespace_creation_to_succeed() => _exception.ShouldBeNull();
    [Fact] async Task should_retry_the_failing_definition_on_the_next_trigger() => await _projectionGrain.Received(2).SetDefinition(_failing);
    [Fact] async Task should_attempt_the_healthy_definition() => await _projectionGrain.Received(1).SetDefinition(_healthy);
    [Fact] void should_await_the_healthy_subscription() => _subscribed.ShouldBeTrue();
    [Fact] async Task should_still_subscribe_the_healthy_projection() =>
        await _healthyObserver.Received(1).ReconcileSubscription<IProjectionObserverSubscriber>(
            ObserverType.Projection,
            Arg.Any<IEnumerable<Concepts.Events.EventType>>(),
            Arg.Any<SiloAddress>(),
            true);
    [Fact] async Task should_not_subscribe_the_failing_projection() =>
        await _failingObserver.DidNotReceive().ReconcileSubscription<IProjectionObserverSubscriber>(
            ObserverType.Projection,
            Arg.Any<IEnumerable<Concepts.Events.EventType>>(),
            Arg.Any<SiloAddress>(),
            true);
}
