// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

/// <summary>
/// The notification was fired and forgotten, so a single failed call left the observer waiting for a replay that
/// had concluded. It is retried, and kept so it can be observed.
/// </summary>
public class and_the_observer_does_not_take_the_notification_at_first : given.a_replay_observer_job
{
    int _calls;

    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _job.NotificationRetryDelay = TimeSpan.Zero;
        _observer.Replayed(Arg.Any<EventSequenceNumber>()).Returns(_ => ++_calls == 1
            ? Task.FromException(new TimeoutException())
            : Task.CompletedTask);
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
        await _job.CompletionNotification;
    }

    [Fact] void should_notify_the_observer_again() => _observer.Received(2).Replayed(42UL);
}
