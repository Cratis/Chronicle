// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_one_of_two_captures_cannot_subscribe : given.a_captures_manager
{
    CaptureDefinition _healthy;
    Exception _exception;
    bool _healthySubscribed;

    void Establish()
    {
        _healthy = _eventsDefinition with { Id = _poll.Id, Name = _poll.Name };
        _languageService.Compile("poll-declaration").Returns(_healthy);
        StartedState(_events);
        StartedState(_poll);
        _subscriptions.Recover(_eventStore, "tenant", _eventsDefinition).Returns(Task.FromException(new TimeoutException()));
        _subscriptions.Recover(_eventStore, "tenant", _healthy).Returns(_ =>
        {
            _healthySubscribed = true;
            _observer.IsSubscriptionEstablished().Returns(true);
            return Task.CompletedTask;
        });
    }

    async Task Because() => _exception = await Catch.Exception(() => _manager.EnsureSubscribedIn("tenant"));

    [Fact] void should_not_fail_the_namespace_pass() => _exception.ShouldBeNull();
    [Fact] void should_log_the_failed_capture() => _logger.ReceivedCalls().Any(call => call.GetArguments().Any(argument => argument is TimeoutException)).ShouldBeTrue();
    [Fact] void should_await_the_healthy_subscription() => _healthySubscribed.ShouldBeTrue();
    [Fact] async Task should_subscribe_the_healthy_capture() => await _subscriptions.Received(1).Recover(_eventStore, "tenant", _healthy);
}
