// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_removing;

public class and_subscription_setup_is_in_flight : given.a_manager_with_an_orphaned_projection
{
    readonly TaskCompletionSource _settingDefinition = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseDefinition = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _removalWaited;

    void Establish()
    {
        _definition = _definition with { SubscribesToAllEvents = true };
        _state.Projections = [_definition];
        _observerGrain.IsSubscriptionEstablished().Returns(false);
        _projectionGrain.SetDefinition(_definition).Returns(_ =>
        {
            _settingDefinition.TrySetResult();
            return _releaseDefinition.Task;
        });
    }

    async Task Because()
    {
        var pass = _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);
        await _settingDefinition.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var removal = _grain.Remove(_definition.Identifier);
        _removalWaited = !removal.IsCompleted;
        _releaseDefinition.TrySetResult();
        await Task.WhenAll(pass, removal).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_drain_the_subscription_pass() => _removalWaited.ShouldBeTrue();
    [Fact] void should_forget_the_definition() => _state.Projections.ShouldBeEmpty();
    [Fact] async Task should_not_resubscribe_the_removed_projection() => await _observerGrain.DidNotReceive().SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: Arg.Any<bool>());
}
