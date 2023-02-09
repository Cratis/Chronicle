namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_RecoverFailedPartitionState.when_getting_the_next_attempt_schedule;

public class and_it_is_the_eleventh_attempt : Specification
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
            NumberOfAttemptsOnCurrentError = 11,
            NumberOfAttemptsOnSinceInitialised = 11
        };

        return Task.CompletedTask;
    }
    
    Task Because()
    {
        nextScheduledAttempt = state.GetNextAttemptSchedule();
        return Task.CompletedTask;
    }
    
    [Fact] void should_return_34_minutes() => nextScheduledAttempt.ShouldEqual(TimeSpan.FromSeconds(2048));
}