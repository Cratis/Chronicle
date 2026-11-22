// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ClientReactorSubscription.when_subscribing;

public class and_the_registration_is_retried_while_the_first_attempt_is_running : given.a_client_reactor_subscription_with_a_stored_definition
{
    readonly TaskCompletionSource _firstSubscribeCanFinish = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _first;
    Task _retry;
    int _subscribesWhileTheFirstRuns;

    void Establish() => _observer
        .Subscribe<IReactorObserverSubscriber>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>())
        .Returns(_ => _firstSubscribeCanFinish.Task);

    async Task Because()
    {
        _first = _subscription.SetDefinitionAndSubscribe(_connectionDefinition);
        await Task.Delay(100);
        _retry = _subscription.SetDefinitionAndSubscribe(_connectionDefinition);
        await Task.Delay(100);
        _subscribesWhileTheFirstRuns = _observer.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IObserver.Subscribe));
        _firstSubscribeCanFinish.SetResult();
        await Task.WhenAll(_first, _retry);
    }

    [Fact] void should_not_start_the_retry_before_the_first_attempt_finishes() => _subscribesWhileTheFirstRuns.ShouldEqual(1);
    [Fact] void should_subscribe_the_observer_once() => _observer.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IObserver.Subscribe)).ShouldEqual(1);
}
