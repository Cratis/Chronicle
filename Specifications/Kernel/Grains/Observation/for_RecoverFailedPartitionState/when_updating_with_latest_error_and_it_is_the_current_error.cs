// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_RecoverFailedPartitionState;

public class when_updating_with_latest_error_and_it_is_the_current_error : Specification
{
    RecoverFailedPartitionState state;
    string[] messages;
    string stacktrace;
    DateTimeOffset now;

    Task Establish()
    {
        messages = new[] { "Something went wrong" };
        stacktrace = "it went wrong here";
        now = DateTimeOffset.UtcNow;

        state = new RecoverFailedPartitionState()
        {
            InitialError = EventSequenceNumber.First,
            CurrentError = EventSequenceNumber.First,
            NextSequenceNumberToProcess = 2,
            NumberOfAttemptsOnCurrentError = 10,
            NumberOfAttemptsOnSinceInitialized = 100,
            InitialPartitionFailedOn = now.AddDays(-1),
            LastAttemptOnCurrentError = now.AddMinutes(-10)
        };

        return Task.CompletedTask;
    }

    Task Because()
    {
        state.UpdateWithLatestError(EventSequenceNumber.First, messages, stacktrace, now);
        return Task.CompletedTask;
    }

    [Fact] void should_not_change_the_initial_error() => state.InitialError.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_change_the_current_error() => state.CurrentError.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_change_the_next_sequence_number() => state.NextSequenceNumberToProcess.ShouldEqual(new EventSequenceNumber(2));
    [Fact] void should_not_change_the_initial_failed_on() => state.InitialPartitionFailedOn.ShouldEqual(now.AddDays(-1));
    [Fact] void should_increment_the_number_of_attempts_current() => state.NumberOfAttemptsOnCurrentError.ShouldEqual(11);
    [Fact] void should_increment_the_number_of_attempts_total() => state.NumberOfAttemptsOnSinceInitialized.ShouldEqual(101);
    [Fact] void should_update_the_last_attempted() => state.LastAttemptOnCurrentError.ShouldEqual(now);
    [Fact] void should_change_the_error_messages() => state.Messages.ShouldEqual(messages);
    [Fact] void should_change_the_error_stacktrace() => state.StackTrace.ShouldEqual(stacktrace);
}
