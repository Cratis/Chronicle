// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_RecoverFailedPartitionState;

public class when_updating_with_latest_success_and_was_current_error : Specification
{
    RecoverFailedPartitionState state;
    DateTimeOffset now;

    Task Establish()
    {
        now = DateTimeOffset.UtcNow;
        
        state = new RecoverFailedPartitionState()
        {
            InitialError = EventSequenceNumber.First,
            CurrentError = EventSequenceNumber.First,
            NextSequenceNumberToProcess = EventSequenceNumber.First,
            NumberOfAttemptsOnCurrentError = 10,
            NumberOfAttemptsOnSinceInitialised = 100,
            InitialPartitionFailedOn = now.AddDays(-1),
            LastAttemptOnCurrentError = now.AddMinutes(-10)
        };

        return Task.CompletedTask;
    }
    
    Task Because()
    {
        state.UpdateWithLatestSuccess(AppendedEvent.EmptyWithEventSequenceNumber(EventSequenceNumber.First));
        return Task.CompletedTask;
    }
    
    [Fact] void should_not_change_the_initial_error() => state.InitialError.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_change_the_current_error_to_unavailable() => state.CurrentError.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_increment_the_next_sequence_number() => state.NextSequenceNumberToProcess.ShouldEqual(EventSequenceNumber.First + 1);
    [Fact] void should_zero_the_attempts_on_the_current_error() => state.NumberOfAttemptsOnCurrentError.ShouldEqual(0);
    [Fact] void should_update_last_attempt_on_the_current_error() => state.LastAttemptOnCurrentError.ShouldEqual(DateTimeOffset.MinValue);
}