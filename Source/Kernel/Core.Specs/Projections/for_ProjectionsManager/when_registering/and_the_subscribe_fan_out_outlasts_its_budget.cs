// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// Registering a large artifact set into a store with many namespaces fans out over definitions x namespaces, and the
/// grain is non-reentrant: while the registration waits for that fan-out, every other registration - including the
/// kernel's own at startup - queues behind it and times out. In production 171 definitions across 37 namespaces kept
/// the grain busy past the 30 second response timeout, and the kernel stopped starting. A registration therefore stops
/// waiting once its subscribe budget is spent and leaves the remaining pairs to the background repair.
/// </summary>
public class and_the_subscribe_fan_out_outlasts_its_budget : given.a_projections_manager_grain
{
    const int DefinitionCount = 30;
    const int SubscribesBeforeTheBudgetIsSpent = 3;

    readonly ConcurrentDictionary<ProjectionId, bool> _established = new();
    int _subscribes;
    ProjectionDefinition[] _incoming = [];
    int _establishedWhenRegistrationReturned;

    void Establish()
    {
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _incoming = [.. Enumerable.Range(0, DefinitionCount).Select(index => CreateDefinition($"the-projection-{index}", "the-read-model") with { SubscribesToAllEvents = true })];

        foreach (var definition in _incoming)
        {
            var identifier = definition.Identifier;
            var observer = Substitute.For<Observation.IObserver>();
            observer.GetState().Returns(new ObserverState());
            observer.IsSubscriptionEstablished().Returns(_ => _established.GetValueOrDefault(identifier));
            observer
                .SubscribeToAllEvents<IProjectionObserverSubscriber>(default, default!, default, default, default)
                .ReturnsForAnyArgs(_ => Subscribed(identifier));
            observer
                .ReconcileSubscription<IProjectionObserverSubscriber>(default, default!, default!, default, default, default, default, default)
                .ReturnsForAnyArgs(_ => Subscribed(identifier));
            _observersByKey[new ObserverKey(identifier, EventStore, EventStoreNamespaceName.Default, definition.EventSequenceId).ToString()] = observer;
        }
    }

    async Task Because()
    {
        await _grain.Register(_incoming);
        _establishedWhenRegistrationReturned = _established.Count(pair => pair.Value);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_have_subscribed_some_before_the_budget_was_spent() => _establishedWhenRegistrationReturned.ShouldBeGreaterThan(0);
    [Fact] void should_return_before_every_pair_was_subscribed() => _establishedWhenRegistrationReturned.ShouldBeLessThan(DefinitionCount);
    [Fact] void should_leave_the_rest_to_the_background_repair() => _established.Count(pair => pair.Value).ShouldEqual(DefinitionCount);
    [Fact] void should_still_record_every_definition() => _state.Projections.Count().ShouldEqual(DefinitionCount);

    Task Subscribed(ProjectionId identifier)
    {
        _established[identifier] = true;
        if (Interlocked.Increment(ref _subscribes) == SubscribesBeforeTheBudgetIsSpent)
        {
            _clock.Advance(ProjectionsManager.RegistrationSubscribeBudget + TimeSpan.FromSeconds(1));
        }

        return Task.CompletedTask;
    }
}
