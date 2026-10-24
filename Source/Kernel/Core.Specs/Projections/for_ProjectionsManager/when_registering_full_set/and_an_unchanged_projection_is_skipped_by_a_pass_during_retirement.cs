// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

/// <summary>
/// A full set re-registers an unchanged projection (a distinct but equal instance, as a deserialized definition
/// is) while retiring an orphan. A subscribe pass that runs while the retirement is awaited must still subscribe
/// the unchanged projection: it is not being replaced, so it must not stay blocked by the registration.
/// </summary>
public class and_an_unchanged_projection_is_skipped_by_a_pass_during_retirement : given.a_projections_manager_grain
{
    readonly TaskCompletionSource _retireStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _retireGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ProjectionDefinition _stored;
    ProjectionDefinition _incoming;
    ProjectionDefinition _orphan;
    IObserver _orphanObserver;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _stored = CreateDefinition("the-projection", "the-read-model");
        _incoming = CreateDefinition("the-projection", "the-read-model");
        _orphan = CreateDefinition("orphaned-projection", "orphaned-read-model");
        _state.Projections = [_stored, _orphan];
        _readModelDefinitions =
        [
            CreateReadModelDefinition("the-read-model", "theContainer"),
            CreateReadModelDefinition("orphaned-read-model", "orphanedContainer")
        ];
        _observerGrain.IsSubscriptionEstablished().Returns(false);
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);

        _orphanObserver = Substitute.For<IObserver>();
        _orphanObserver.GetState().Returns(new ObserverState());
        _orphanObserver.Retire().Returns(_ =>
        {
            _retireStarted.TrySetResult();
            return _retireGate.Task;
        });
        _observersByKey[new ObserverKey(_orphan.Identifier, EventStore, EventStoreNamespaceName.Default, _orphan.EventSequenceId).ToString()] = _orphanObserver;
    }

    async Task Because()
    {
        var registration = _grain.Register([_incoming], ProjectionOwner.Client);
        await _retireStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _silo.TimerRegistry.FireAllAsync();
        _retireGate.TrySetResult();
        await registration.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact]
    void should_subscribe_the_unchanged_projection() =>
        _observerGrain.Received().ReconcileSubscription<IProjectionObserverSubscriber>(
            ObserverType.Projection,
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<SiloAddress>(),
            Arg.Any<bool>(),
            Arg.Any<EventGenerationDelivery>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<ObserverFilters?>());
}
