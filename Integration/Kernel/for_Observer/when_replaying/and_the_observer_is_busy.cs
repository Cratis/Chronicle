// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;

using context = Cratis.Chronicle.Kernel.Integration.for_Observer.when_replaying.and_the_observer_is_busy.context;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.when_replaying;

/// <summary>
/// An operator replay is accepted at once and runs as a job, even while the observer is busy delivering.
/// </summary>
/// <param name="context">The <see cref="context"/>.</param>
[Collection(given.OperatorCommandCollection.Name)]
public class and_the_observer_is_busy(context context) : Given<context>(context)
{
    public class context(given.OperatorCommandFixture fixture) : given.a_reactor_given_operator_commands(fixture)
    {
        public TimeSpan TimeToAccept;
        public IEnumerable<Job> Jobs;
        public int HandledAfterRelease;

        async Task Because()
        {
            Reactor.Mode = given.QuarantineRecoveryReactor.RecoveryMode.BlockUntilReleased;
            await _store.EventLog.Append("busy", new given.QuarantineRecoveryEvent("busy"));
            await Reactor.RetryReached.Task.WaitAsync(TimeSpan.FromSeconds(30));

            var started = DateTimeOffset.UtcNow;
            await KernelServices.Observers.Replay(new()
            {
                EventStore = _store.Name,
                Namespace = _store.Namespace,
                ObserverId = _handler.Id,
                EventSequenceId = _handler.EventSequenceId
            }).WaitAsync(TimeSpan.FromSeconds(20));
            TimeToAccept = DateTimeOffset.UtcNow - started;

            Jobs = await WaitForOperatorCommandJobs(TimeSpan.FromSeconds(20));
            Reactor.Release();
            await WaitUntil(() => Reactor.HandledSequenceNumbers.Count >= 2, TimeSpan.FromSeconds(90));
            HandledAfterRelease = Reactor.HandledSequenceNumbers.Count;
        }
    }

    [Fact] void should_accept_the_replay_without_waiting_for_the_observer() => Context.TimeToAccept.ShouldBeLessThan(TimeSpan.FromSeconds(15));
    [Fact] void should_run_the_replay_as_a_job() => Context.Jobs.ShouldNotBeEmpty();
    [Fact] void should_deliver_the_event_again_once_the_observer_is_free() => Context.HandledAfterRelease.ShouldBeGreaterThanOrEqual(2);
}
