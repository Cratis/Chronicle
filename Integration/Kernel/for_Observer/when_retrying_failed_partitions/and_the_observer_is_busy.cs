// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Reactors;

using context = Cratis.Chronicle.Kernel.Integration.for_Observer.when_retrying_failed_partitions.and_the_observer_is_busy.context;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.when_retrying_failed_partitions;

/// <summary>
/// An operator retry of the failed partitions is accepted at once and runs as a job, even while the observer is busy delivering.
/// </summary>
/// <param name="context">The <see cref="context"/>.</param>
[Collection(given.OperatorCommandCollection.Name)]
public class and_the_observer_is_busy(context context) : Given<context>(context)
{
    public class context(given.OperatorCommandFixture fixture) : given.a_reactor_given_operator_commands(fixture)
    {
        public TimeSpan TimeToAccept;
        public IEnumerable<Job> Jobs;

        async Task Because()
        {
            Reactor.Mode = given.QuarantineRecoveryReactor.RecoveryMode.Fail;
            await _store.EventLog.Append("failing", new given.QuarantineRecoveryEvent("fail"));
            await Reactor.Failed.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await _handler.WaitForThereToBeFailedPartitions(TimeSpan.FromSeconds(30));

            Reactor.Mode = given.QuarantineRecoveryReactor.RecoveryMode.BlockUntilReleased;

            var started = DateTimeOffset.UtcNow;
            await KernelServices.Observers.RetryFailedPartitions(new()
            {
                EventStore = _store.Name,
                Namespace = _store.Namespace,
                ObserverId = _handler.Id,
                EventSequenceId = _handler.EventSequenceId
            }).WaitAsync(TimeSpan.FromSeconds(20));
            TimeToAccept = DateTimeOffset.UtcNow - started;

            Jobs = await WaitForOperatorCommandJobs(TimeSpan.FromSeconds(20));
            Reactor.Release();
            await WaitUntil(() => !Reactor.HandledSequenceNumbers.IsEmpty, TimeSpan.FromSeconds(90));
        }
    }

    [Fact] void should_accept_the_retry_without_waiting_for_the_observer() => Context.TimeToAccept.ShouldBeLessThan(TimeSpan.FromSeconds(15));
    [Fact] void should_run_the_retry_as_a_job() => Context.Jobs.ShouldNotBeEmpty();
    [Fact] void should_handle_the_failed_event_once_the_observer_is_free() => Context.Reactor.HandledSequenceNumbers.ShouldNotBeEmpty();
}
