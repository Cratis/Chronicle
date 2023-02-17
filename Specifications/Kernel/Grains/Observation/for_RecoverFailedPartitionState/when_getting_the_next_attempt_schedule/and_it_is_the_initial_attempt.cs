// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_RecoverFailedPartitionState.when_getting_the_next_attempt_schedule;

public class and_it_is_the_initial_attempt : Specification
{
    RecoverFailedPartitionState state;
    DateTimeOffset now;
    TimeSpan nextScheduledAttempt;

    Task Establish()
    {
        now = DateTimeOffset.UtcNow;
        state = new RecoverFailedPartitionState()
        {
            InitialError = EventSequenceNumber.First,
            CurrentError = EventSequenceNumber.First,
            NextSequenceNumberToProcess = 2,
            NumberOfAttemptsOnCurrentError = 0,
            NumberOfAttemptsOnSinceInitialized = 0
        };

        return Task.CompletedTask;
    }

    Task Because()
    {
        nextScheduledAttempt = state.GetNextAttemptSchedule();
        return Task.CompletedTask;
    }

    [Fact] void should_return_an_immediate_schedule() => nextScheduledAttempt.ShouldEqual(TimeSpan.Zero);
}
