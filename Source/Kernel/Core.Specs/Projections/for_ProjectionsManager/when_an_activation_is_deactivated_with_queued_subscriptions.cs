// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionsManager;

/// <summary>
/// Both cancellation race orderings defer work; only a true grant is released.
/// </summary>
public class when_an_activation_is_deactivated_with_queued_subscriptions : given.a_projections_manager_grain
{
    readonly TaskCompletionSource _queued = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<CancellationTokenRegistration> _registrations = [];
    readonly List<TaskCompletionSource<bool>> _waits = [];
    CancellationToken _token;
    int _count;

    /// <inheritdoc/>
    protected override IProjectionSubscriptionThrottle CreateThrottle() => Substitute.For<IProjectionSubscriptionThrottle>();

    void Establish()
    {
        _readModelDefinitions = [CreateReadModelDefinition("model")];
        _throttle.WaitAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _token = call.ArgAt<CancellationToken>(1);
            var wait = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waits.Add(wait);
            var grantBeforeCancellation = ++_count == 1;
            _registrations.Add(_token.Register(() =>
            {
                if (grantBeforeCancellation) wait.TrySetResult(true);
                else wait.TrySetCanceled(_token);
            }));
            if (_count == 2) _queued.TrySetResult();
            return wait.Task;
        });
    }

    async Task Because()
    {
        var registering = _grain.Register([CreateDefinition("first", "model"), CreateDefinition("second", "model")]);
        await Task.WhenAny(registering, _queued.Task);
        await _grain.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ShuttingDown, "spec"), CancellationToken.None);
        await registering.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        await _silo.TimerRegistry.FireAllAsync();
    }

    void Destroy()
    {
        foreach (var wait in _waits) wait.TrySetResult(false);
        foreach (var registration in _registrations) registration.Dispose();
    }

    [Fact] void should_allow_repeated_disposal() => Catch.Exception(() =>
    {
        _grain.Dispose();
        _grain.Dispose();
    }).ShouldBeNull();

    [Fact] void should_cancel_the_activation_token() => _token.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_release_only_the_true_grant() => _throttle.Received(1).Release();
    [Fact] void should_not_build_cancelled_pairs() => _projectionFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_keep_both_accepted_definitions() => _state.Projections.Count().ShouldEqual(2);
    [Fact] async Task should_not_subscribe_cancelled_pairs() => await _observerGrain.DidNotReceiveWithAnyArgs().Subscribe<IProjectionObserverSubscriber>(default, default!, default!);
}
