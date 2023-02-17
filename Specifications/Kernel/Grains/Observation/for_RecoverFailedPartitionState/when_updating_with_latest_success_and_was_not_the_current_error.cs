// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_RecoverFailedPartitionState;

public class when_updating_with_latest_success_and_was_not_the_current_error : Specification
{
    RecoverFailedPartitionState state;
    DateTimeOffset now;
    DateTimeOffset initial;
    DateTimeOffset last;

    Task Establish()
    {
        now = DateTimeOffset.UtcNow;
        initial = now.AddDays(-1);
        last = now.AddMinutes(-10);

        //This is not a valid state, but we want to test that it does not change
        state = new RecoverFailedPartitionState()
        {
            InitialError = EventSequenceNumber.First,
            CurrentError = EventSequenceNumber.Max,
            NextSequenceNumberToProcess = EventSequenceNumber.First,
            NumberOfAttemptsOnCurrentError = 10,
            NumberOfAttemptsOnSinceInitialized = 100,
            InitialPartitionFailedOn = initial,
            LastAttemptOnCurrentError = last
        };

        return Task.CompletedTask;
    }

    Task Because()
    {
        state.UpdateWithLatestSuccess(AppendedEvent.EmptyWithEventSequenceNumber(state.NextSequenceNumberToProcess));
        return Task.CompletedTask;
    }

    [Fact]
    void should_not_change_the_initial_error() => state.InitialError.ShouldEqual(EventSequenceNumber.First);

    [Fact]
    void should_not_change_the_current_error() =>
        state.CurrentError.ShouldEqual(EventSequenceNumber.Max);

    [Fact]
    void should_increment_the_next_sequence_number() =>
        state.NextSequenceNumberToProcess.ShouldEqual(EventSequenceNumber.First + 1);

    [Fact]
    void should_not_change_attempts_on_current_error() => state.NumberOfAttemptsOnCurrentError.ShouldEqual(10);

    [Fact]
    void should_not_change_last_attempt_on_current_error() => state.LastAttemptOnCurrentError.ShouldEqual(last);
}
