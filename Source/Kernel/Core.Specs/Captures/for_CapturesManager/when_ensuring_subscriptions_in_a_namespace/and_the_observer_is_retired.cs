// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_observer_is_retired : given.a_captures_manager
{
    Exception _exception;

    void Establish()
    {
        StartedState(_events);
        _observer.GetState().Returns(new ObserverState { AlertDisposition = AlertDisposition.Retired });
    }

    async Task Because() => _exception = await Catch.Exception(() => _manager.EnsureSubscribedIn("tenant"));

    [Fact] void should_complete_without_failure() => _exception.ShouldBeNull();
    [Fact] async Task should_not_attempt_to_subscribe_the_retired_observer() => await _subscriptions.DidNotReceive().Subscribe(_eventStore, "tenant", Arg.Any<CaptureDefinition>());
    [Fact] void should_not_log_a_subscription_failure() => _logger.ReceivedCalls().Any(call => call.GetArguments().Any(argument => argument is Exception)).ShouldBeFalse();
}
