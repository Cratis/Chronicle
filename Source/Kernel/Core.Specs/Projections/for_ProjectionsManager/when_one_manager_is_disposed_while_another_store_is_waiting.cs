// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionsManager;

/// <summary>
/// Disposing a queued activation neither admits its work nor disposes host admission.
/// </summary>
public class when_one_manager_is_disposed_while_another_store_is_waiting : given.two_projections_managers_sharing_admission
{
    bool _capacityIntact;
    Exception? _error;
    Exception? _disposeError;

    void Establish() => HoldBuilds();

    async Task Because()
    {
        var holding = _secondGrain.Register(_secondDefinitions);
        await _eightEntered.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        var queued = _grain.Register(_firstDefinitions.Take(2));
        _firstManagerDisposed = true;
        _disposeError = Catch.Exception(() =>
        {
            _grain.Dispose();
            _grain.Dispose();
        });
        _release.TrySetResult();
        _error = await Catch.Exception(() => Task.WhenAll(holding, queued));
        _capacityIntact = true;
        for (var index = 0; index < 8; index++) _capacityIntact &= await _throttle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        _capacityIntact &= !await _throttle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        for (var index = 0; index < 8; index++) _throttle.Release();
    }

    [Fact] void should_allow_repeated_disposal() => _disposeError.ShouldBeNull();
    [Fact] void should_not_fail_the_registrations() => _error.ShouldBeNull();
    [Fact] void should_not_build_the_disposed_managers_pairs() => _projectionFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_complete_the_other_store() => _secondState.Projections.Count().ShouldEqual(12);
    [Fact] void should_leave_exactly_eight_host_permits() => _capacityIntact.ShouldBeTrue();
}
