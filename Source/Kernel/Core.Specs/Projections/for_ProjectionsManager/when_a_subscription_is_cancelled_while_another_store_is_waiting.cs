// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionsManager;

/// <summary>
/// Subscription failure releases the same admission held through the build.
/// </summary>
public class when_a_subscription_is_cancelled_while_another_store_is_waiting : given.two_projections_managers_sharing_admission
{
    Exception? _error;
    bool _capacityIntact;

    void Establish()
    {
        HoldBuilds();
        _observerGrain.Subscribe<IProjectionObserverSubscriber>(default, default!, default!).ReturnsForAnyArgs(Task.FromCanceled(new CancellationToken(true)));
    }

    async Task Because()
    {
        var failing = _grain.Register(_firstDefinitions.Take(8));
        await _eightEntered.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        var healthy = _secondGrain.Register(_secondDefinitions);
        _release.TrySetResult();
        _error = await Catch.Exception(() => failing);
        await healthy;
        _capacityIntact = true;
        for (var index = 0; index < 8; index++) _capacityIntact &= await _throttle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        _capacityIntact &= !await _throttle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        for (var index = 0; index < 8; index++) _throttle.Release();
    }

    [Fact] void should_report_the_subscription_failure() => _error.ShouldBeOfExactType<SomeProjectionDefinitionsFailedToRegister>();
    [Fact] void should_complete_the_healthy_store() => _secondState.Projections.Count().ShouldEqual(12);
    [Fact] void should_preserve_the_aggregate_bound() => _peak.ShouldEqual(8);
    [Fact] void should_return_exactly_the_granted_capacity() => _capacityIntact.ShouldBeTrue();
}
