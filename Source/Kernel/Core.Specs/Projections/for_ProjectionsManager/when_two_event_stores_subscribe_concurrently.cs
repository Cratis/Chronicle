// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionsManager;

/// <summary>
/// The admission bound applies to the combined fan-out, not independently to each store.
/// </summary>
public class when_two_event_stores_subscribe_concurrently : given.two_projections_managers_sharing_admission
{
    int _buildsWhileSubscriptionsHeld;

    void Establish() => HoldBuilds(holdSubscriptions: true);

    async Task Because()
    {
        var first = _grain.Register(_firstDefinitions);
        await _eightEntered.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        var second = _secondGrain.Register(_secondDefinitions);
        _release.TrySetResult();
        await _subscriptionsEntered.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        _buildsWhileSubscriptionsHeld = _builds;
        _releaseSubscriptions.TrySetResult();
        await Task.WhenAll(first, second);
    }

    [Fact] void should_hold_admission_through_subscribe() => _buildsWhileSubscriptionsHeld.ShouldEqual(8);
    [Fact] void should_bound_combined_builds() => _peak.ShouldEqual(8);
    [Fact] void should_build_every_pair() => _builds.ShouldEqual(24);
    [Fact] void should_preserve_first_store_state() => _state.Projections.Select(definition => definition.Identifier).ShouldContainOnly(_firstDefinitions.Select(definition => definition.Identifier));
    [Fact] void should_preserve_second_store_state() => _secondState.Projections.Select(definition => definition.Identifier).ShouldContainOnly(_secondDefinitions.Select(definition => definition.Identifier));
    [Fact] async Task should_subscribe_every_first_store_pair() => await _observerGrain.ReceivedWithAnyArgs(12).Subscribe<IProjectionObserverSubscriber>(default, default!, default!);
    [Fact] async Task should_subscribe_every_second_store_pair() => await _secondObserver.ReceivedWithAnyArgs(12).Subscribe<IProjectionObserverSubscriber>(default, default!, default!);
}
