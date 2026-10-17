// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

public class and_another_pass_is_pending : given.a_projections_manager_grain
{
    readonly TaskCompletionSource _subscription = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _joinedPendingPass;
    bool _otherNamespaceCompleted;
    bool _otherNamespaceSubscribed;
    int _subscriptionAttempts;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _state.Projections = [CreateDefinition("the-projection", "the-read-model") with { SubscribesToAllEvents = true }];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _observerGrain.IsSubscribed().Returns(_ => _subscription.Task.IsCompleted || _otherNamespaceSubscribed);
        _observerGrain.ReconcileSubscription<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<SiloAddress>(), true)
            .Returns(_ =>
            {
                if (++_subscriptionAttempts == 1) return _subscription.Task;
                _otherNamespaceSubscribed = true;
                return Task.CompletedTask;
            });
    }

    async Task Because()
    {
        var first = _grain.EnsureSubscribedIn("pending-tenant");
        var second = _grain.EnsureSubscribedIn("pending-tenant");
        _joinedPendingPass = ReferenceEquals(first, second) && !second.IsCompleted;
        var other = _grain.EnsureSubscribedIn("other-tenant");
        _otherNamespaceCompleted = other.IsCompletedSuccessfully;
        _subscription.TrySetResult();
        await Task.WhenAll(first, second, other).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_join_work_already_running_for_the_same_namespace() => _joinedPendingPass.ShouldBeTrue();
    [Fact] void should_not_block_another_namespace() => _otherNamespaceCompleted.ShouldBeTrue();
    [Fact] async Task should_set_the_definition_once_across_namespace_passes() => await _projectionGrain.ReceivedWithAnyArgs(1).SetDefinition(default!);
    [Fact] async Task should_subscribe_each_distinct_namespace_once() => await _observerGrain.Received(2).ReconcileSubscription<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<SiloAddress>(), true);
}
