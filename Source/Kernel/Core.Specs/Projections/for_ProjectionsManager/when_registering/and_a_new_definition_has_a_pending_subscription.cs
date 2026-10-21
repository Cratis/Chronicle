// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_a_new_definition_has_a_pending_subscription : given.a_projections_manager_grain
{
    readonly TaskCompletionSource _subscriptionStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _subscriptionCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ProjectionDefinition _incoming;
    bool _registrationCompletedBeforeSubscription;

    void Establish()
    {
        _incoming = CreateDefinition("the-projection", "the-read-model");
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _observerGrain
            .Subscribe<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), reactivateRetired: true)
            .Returns(_ =>
            {
                _subscriptionStarted.SetResult();
                return _subscriptionCompleted.Task;
            });
    }

    async Task Because()
    {
        var registration = _grain.Register([_incoming]);
        try
        {
            await _subscriptionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _registrationCompletedBeforeSubscription = registration.IsCompleted;
        }
        finally
        {
            _subscriptionCompleted.SetResult();
        }

        await registration.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_wait_for_the_default_namespace_subscription_before_returning() => _registrationCompletedBeforeSubscription.ShouldBeFalse();
    [Fact] async Task should_subscribe_the_projection_observer() => await _observerGrain.Received(1).Subscribe<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), reactivateRetired: true);
    [Fact] void should_register_the_definition_after_subscription_completes() => _state.Projections.ShouldContainOnly(_incoming);
}
