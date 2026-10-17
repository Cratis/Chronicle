// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_observer_is_retired : given.a_projections_manager_grain
{
    Exception _exception;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _state.Projections = [CreateDefinition("the-projection", "the-read-model") with { SubscribesToAllEvents = true }];
        _observerGrain.IsSubscribed().Returns(false);
        _observerGrain.GetState().Returns(new ObserverState { AlertDisposition = AlertDisposition.Retired });
    }

    async Task Because() => _exception = await Catch.Exception(() => _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default));

    [Fact] void should_complete_without_failure() => _exception.ShouldBeNull();
    [Fact] async Task should_not_attempt_to_subscribe_the_retired_observer() => await _observerGrain.DidNotReceive().SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: Arg.Any<bool>());
    [Fact] async Task should_not_set_the_projection_definition() => await _projectionGrain.DidNotReceiveWithAnyArgs().SetDefinition(default!);
    [Fact] void should_not_log_a_subscription_failure() => _logger.ReceivedCalls().Any(call => call.GetArguments().Any(argument => argument is Exception)).ShouldBeFalse();
}
