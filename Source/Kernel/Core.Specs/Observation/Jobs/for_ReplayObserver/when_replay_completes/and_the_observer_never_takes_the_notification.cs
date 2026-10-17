// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_the_observer_never_takes_the_notification : given.a_replay_observer_job
{
    void Establish()
    {
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _job.NotificationRetryDelay = TimeSpan.Zero;
        _observer.Replayed(Arg.Any<EventSequenceNumber>()).Returns(Task.FromException(new TimeoutException()));
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
        await _job.CompletionNotification;
    }

    [Fact] void should_stop_after_the_maximum_number_of_attempts() => _observer.Received(ReplayObserver.MaximumNotificationAttempts).Replayed(42UL);
}
