// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_a_capture_cannot_subscribe : given.a_captures_manager
{
    Exception _firstFailure;
    Exception _retryFailure;
    int _attempts;

    void Establish()
    {
        StartedState(_events);
        _subscriptions.Recover(_eventStore, "tenant", Arg.Any<CaptureDefinition>()).Returns(_ =>
        {
            if (++_attempts == 1) return Task.FromException(new TimeoutException());
            _observer.IsSubscribed().Returns(true);
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        _firstFailure = await Catch.Exception(() => _manager.EnsureSubscribedIn("tenant"));
        _retryFailure = await Catch.Exception(() => _manager.EnsureSubscribedIn("tenant"));
    }

    [Fact] void should_not_fail_namespace_creation() => _firstFailure.ShouldBeNull();
    [Fact] void should_log_the_failed_subscription() => _logger.ReceivedCalls().Any(call => call.GetArguments().Any(argument => argument is TimeoutException)).ShouldBeTrue();
    [Fact] void should_allow_a_later_pass_to_retry() => _retryFailure.ShouldBeNull();
    [Fact] async Task should_retry_the_capture_in_the_same_namespace() => await _subscriptions.Received(2).Recover(_eventStore, "tenant", _eventsDefinition);
}
