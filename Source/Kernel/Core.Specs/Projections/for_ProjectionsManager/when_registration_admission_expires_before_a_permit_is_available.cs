// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager;

/// <summary>
/// A timed-out queue keeps the accepted definition and its retired-observer revival obligation.
/// </summary>
public class when_registration_admission_expires_before_a_permit_is_available : given.a_projections_manager_grain
{
    readonly TaskCompletionSource<bool> _wait = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ProjectionDefinition _definition;
    TimeSpan _timeout;
    CancellationToken _token;
    bool _installed;
    bool _installedBeforeRepair;
    bool _builtBeforeRepair;
    bool _revived;
    bool _repairUsedInfiniteWait;
    int _waitCount;
    int _releasesBeforeRepair;

    /// <inheritdoc/>
    protected override IProjectionSubscriptionThrottle CreateThrottle() => Substitute.For<IProjectionSubscriptionThrottle>();

    void Establish()
    {
        _readModelDefinitions = [CreateReadModelDefinition("model")];
        _definition = CreateDefinition("projection", "model") with { SubscribesToAllEvents = true };
        _observerGrain.GetState().Returns(new ObserverState { AlertDisposition = AlertDisposition.Retired });
        _observerGrain.IsSubscriptionEstablished().Returns(_ => _installed);
        _observerGrain.SubscribeToAllEvents<IProjectionObserverSubscriber>(default, default!, default, default, default).ReturnsForAnyArgs(call =>
        {
            _revived = call.ArgAt<bool>(4);
            _installed = true;
            return Task.CompletedTask;
        });
        _throttle.WaitAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _timeout = call.ArgAt<TimeSpan>(0);
            _token = call.ArgAt<CancellationToken>(1);
            var granted = ++_waitCount == 2;
            if (_waitCount == 2) _entered.TrySetResult();
            return CompleteWait(granted);
        });
    }

    async Task Because()
    {
        var registering = _grain.Register([_definition, _definition with { Identifier = "granted-after-expiry" }]);
        await Task.WhenAny(registering, _entered.Task);
        _clock.Advance(ProjectionsManager.RegistrationSubscribeBudget + TimeSpan.FromSeconds(1));
        _wait.TrySetResult(false);
        await registering;
        _installedBeforeRepair = _installed;
        _builtBeforeRepair = _projectionFactory.ReceivedCalls().Any();
        _releasesBeforeRepair = _throttle.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscriptionThrottle.Release));
        _throttle.WaitAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _repairUsedInfiniteWait = call.ArgAt<TimeSpan>(0) == Timeout.InfiniteTimeSpan;
            return Task.FromResult(true);
        });
        await _silo.TimerRegistry.FireAllAsync();
    }

    async Task<bool> CompleteWait(bool granted)
    {
        await _wait.Task;
        return granted;
    }

    void Destroy() => _wait.TrySetResult(false);

    [Fact] void should_pass_the_remaining_budget() => _timeout.ShouldEqual(ProjectionsManager.RegistrationSubscribeBudget);
    [Fact] void should_pass_an_activation_token() => _token.CanBeCanceled.ShouldBeTrue();
    [Fact] void should_not_build_before_repair() => _builtBeforeRepair.ShouldBeFalse();
    [Fact] void should_not_install_on_timeout() => _installedBeforeRepair.ShouldBeFalse();
    [Fact] void should_persist_the_accepted_definition() => _state.Projections.ShouldContain(_definition);
    [Fact] void should_repair_without_a_registration_deadline() => _repairUsedInfiniteWait.ShouldBeTrue();
    [Fact] void should_revive_in_repair() => (_installed && _revived).ShouldBeTrue();
    [Fact] void should_release_the_late_grant_but_not_the_timeout() => _releasesBeforeRepair.ShouldEqual(1);
    [Fact] void should_release_the_late_grant_and_both_repair_grants() => _throttle.Received(3).Release();
    [Fact] void should_keep_both_accepted_definitions() => _state.Projections.Count().ShouldEqual(2);
}
