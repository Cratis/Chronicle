// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_observer_is_quarantined : given.a_captures_manager
{
    readonly ObserverState _quarantined = new() { RunningState = ObserverRunningState.Quarantined };

    void Establish()
    {
        StartedState(_events);
        _observer.IsSubscribed().Returns(false);
        _observer.GetState().Returns(_quarantined);
        _subscriptions.Subscribe(_eventStore, "tenant", Arg.Any<CaptureDefinition>()).Returns(_ =>
        {
            _observer.IsSubscribed().Returns(true);
            _observer.GetState().Returns(new ObserverState());
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await _manager.EnsureSubscribedIn("tenant");
        await _manager.EnsureSubscribedIn("tenant");
    }

    [Fact] async Task should_leave_the_observer_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] async Task should_preserve_quarantine() => (await _observer.GetState()).RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_not_attempt_ordinary_subscription() => await _subscriptions.DidNotReceive().Subscribe(_eventStore, "tenant", Arg.Any<CaptureDefinition>());
}
