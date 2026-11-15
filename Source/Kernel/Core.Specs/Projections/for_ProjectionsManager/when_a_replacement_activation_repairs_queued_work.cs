// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager;

/// <summary>
/// A replacement activation uses fresh cancellation and retains startup versus repair semantics.
/// </summary>
public class when_a_replacement_activation_repairs_queued_work : given.two_projections_managers_sharing_admission
{
    ProjectionsManager _replacement;
    IProjectionFactory _replacementFactory;
    IObserver _retired;
    IObserver _quarantined;
    int _startupBuilds;
    int _repairBuilds;
    Exception? _oldRegistrationError;

    void Establish() => HoldBuilds();

    async Task Because()
    {
        var holding = _secondGrain.Register(_secondDefinitions.Take(8));
        await _eightEntered.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        var queued = _grain.Register(_firstDefinitions.Take(2));
        _firstManagerDisposed = true;
        await _grain.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ShuttingDown, "spec"), CancellationToken.None);
        _release.TrySetResult();
        _oldRegistrationError = await Catch.Exception(() => Task.WhenAll(holding, queued));

        var retired = CreateDefinition("retired", "model");
        var quarantined = CreateDefinition("quarantined", "model");
        _state.Projections = [.. _state.Projections, retired, quarantined];
        _retired = Substitute.For<IObserver>();
        _retired.GetState().Returns(new ObserverState { AlertDisposition = AlertDisposition.Retired });
        _quarantined = Substitute.For<IObserver>();
        _quarantined.GetState().Returns(new ObserverState { RunningState = ObserverRunningState.Quarantined });
        _observerGrain.IsSubscriptionEstablished().Returns(false);
        var observers = new Dictionary<string, IObserver>
        {
            [new ObserverKey(retired.Identifier, EventStore, Concepts.EventStoreNamespaceName.Default, retired.EventSequenceId).ToString()] = _retired,
            [new ObserverKey(quarantined.Identifier, EventStore, Concepts.EventStoreNamespaceName.Default, quarantined.EventSequenceId).ToString()] = _quarantined
        };
        var replacementSilo = new TestKitSilo();
        var stateStorage = Substitute.For<IStorage<ProjectionsManagerState>>();
        stateStorage.State = _state;
        replacementSilo.Options.StorageFactory = _ => stateStorage;
        _replacementFactory = Substitute.For<IProjectionFactory>();
        _replacementFactory.Create(default!, default!, default!, default!, default!).ReturnsForAnyArgs(_engineProjection);
        _replacement = await CreateManager(replacementSilo, EventStore, _replacementFactory, key => observers.GetValueOrDefault(key) ?? _observerGrain);
        await replacementSilo.TimerRegistry.FireAllAsync();
        _startupBuilds = _replacementFactory.ReceivedCalls().Count();
        _replacementFactory.ClearReceivedCalls();
        await _replacement.EnsureSubscribedIn(Concepts.EventStoreNamespaceName.Default);
        _repairBuilds = _replacementFactory.ReceivedCalls().Count();
    }

    void Destroy() => _replacement?.Dispose();

    [Fact] void should_defer_without_failing_registration() => _oldRegistrationError.ShouldBeNull();
    [Fact] void should_build_normal_and_quarantined_startup_pairs() => _startupBuilds.ShouldEqual(3);
    [Fact] void should_only_repair_normal_pairs() => _repairBuilds.ShouldEqual(2);
    [Fact] async Task should_leave_retirement_alone() => await _retired.DidNotReceiveWithAnyArgs().Subscribe<IProjectionObserverSubscriber>(default, default!, default!);
    [Fact] async Task should_freshly_subscribe_quarantine_at_startup() => await _quarantined.ReceivedWithAnyArgs(1).Subscribe<IProjectionObserverSubscriber>(default, default!, default!);
    [Fact] async Task should_not_reconcile_quarantine() => await _quarantined.DidNotReceiveWithAnyArgs().ReconcileSubscription<IProjectionObserverSubscriber>(default, default!, default!, default);
}
