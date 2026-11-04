// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Security;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// Models graceful reactivation followed by the caller-free kernel subscription, not a full process restart.
/// Reactivation alone retains quarantine; the explicit subscription releases it.
/// </summary>
public class when_a_restart_resubscribes_a_quarantined_kernel_reactor : given.an_observer_with_subscription
{
    Observer _original;
    Observer _restarted;

    protected override ObserverId _observerId => $"$system.{typeof(UsersReactor).GetReactorId()}";
    protected override ObserverKey _observerKey => new(_observerId, EventStoreName.System, EventStoreNamespaceName.Default, EventSequenceId.Log);

    async Task Establish()
    {
        _original = _observer;
        ObserverKey.Parse(_original.GetPrimaryKeyString()).ShouldEqual(_observerKey);
        await _original.TransitionTo<QuarantinedObserver>();
        _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
        _restarted = await Reactivate();
        (await _restarted.IsObserverQuarantined()).ShouldBeTrue();
        _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
        var subscriber = Substitute.For<IReactorObserverSubscriber<UsersReactor>>();
        _silo.AddProbe(_ => subscriber);
    }

    async Task Because() => await _restarted.Subscribe<IReactorObserverSubscriber<UsersReactor>>(
        ObserverType.Reactor, typeof(UsersReactor).GetEventTypes(), SiloAddress.Zero, null, isReplayable: false, automatic: false);

    [Fact] void should_use_a_fresh_activation() => ReferenceEquals(_original, _restarted).ShouldBeFalse();
    [Fact] void should_reactivate_the_same_system_observer() => ObserverKey.Parse(_restarted.GetPrimaryKeyString()).ShouldEqual(_observerKey);
    [Fact] async Task should_no_longer_be_quarantined() => (await _restarted.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_have_left_the_quarantined_state() => (await _restarted.GetCurrentState()).GetType().ShouldNotEqual(typeof(QuarantinedObserver));
    [Fact] void should_persist_that_it_is_not_quarantined() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
    [Fact] void should_write_the_recovered_state() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
