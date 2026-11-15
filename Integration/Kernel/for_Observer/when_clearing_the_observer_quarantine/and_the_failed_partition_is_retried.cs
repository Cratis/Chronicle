// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

using context = Cratis.Chronicle.Kernel.Integration.for_Observer.when_clearing_the_observer_quarantine.and_the_failed_partition_is_retried.context;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.when_clearing_the_observer_quarantine;

[Collection(given.QuarantineRecoveryCollection.Name)]
public class and_the_failed_partition_is_retried(context context) : Given<context>(context)
{
    public class context(given.QuarantineRecoveryFixture fixture) : given.a_quarantined_reactor(fixture)
    {
        public ReactorPartitionRetryOutcome RefusedOutcome;
        public ReactorPartitionRetryOutcome Outcome;
        public IEnumerable<Job> RetryJobs;
        public ReactorState State;
        public EventSequenceNumber NewEventSequenceNumber;

        public EventSequenceNumber QuarantinedNextEventSequenceNumber => _quarantinedState.NextEventSequenceNumber;

        async Task Establish()
        {
            RefusedOutcome = await RetryFailedPartition();
            Reactor.Mode = given.QuarantineRecoveryReactor.RecoveryMode.BlockUntilReleased;
        }

        async Task Because()
        {
            try
            {
                await ClearObserverQuarantine();
                await _handler.WaitTillActive(TimeSpan.FromSeconds(30));
                Outcome = await RetryFailedPartition();
                RetryJobs = await GetRetryJobs();
                Reactor.Release();
                await Reactor.RetryHandled.Task.WaitAsync(TimeSpan.FromSeconds(30));
                var appended = await _store.EventLog.Append("after-recovery", new given.QuarantineRecoveryEvent("after"));
                NewEventSequenceNumber = appended.SequenceNumber;
                await _handler.WaitTillReachesEventSequenceNumber(NewEventSequenceNumber, TimeSpan.FromSeconds(30));
                State = await _handler.GetState();
            }
            finally
            {
                Reactor.Release();
            }
        }
    }

    [Fact] void should_have_refused_the_retry_before_clearing() => Context.RefusedOutcome.ShouldEqual(ReactorPartitionRetryOutcome.ObserverQuarantined);
    [Fact] void should_start_the_retry_after_clearing() => Context.Outcome.ShouldEqual(ReactorPartitionRetryOutcome.Started);
    [Fact] void should_have_a_retry_job() => Context.RetryJobs.ShouldNotBeEmpty();
    [Fact] void should_be_active() => Context.State.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_be_subscribed() => Context.State.IsSubscribed.ShouldBeTrue();
    [Fact] void should_handle_the_new_event() => Context.State.LastHandledEventSequenceNumber.ShouldEqual(Context.NewEventSequenceNumber);
    [Fact] void should_advance_the_next_event_sequence_number_past_the_new_event() => Context.State.NextEventSequenceNumber.Value.ShouldBeGreaterThan(Context.NewEventSequenceNumber.Value);
    [Fact] void should_advance_the_next_event_sequence_number_from_the_quarantined_position() => Context.State.NextEventSequenceNumber.Value.ShouldBeGreaterThan(Context.QuarantinedNextEventSequenceNumber.Value);
}
