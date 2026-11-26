// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts;
using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping.and_a_replay_ended_with_failures_when_the_client_left.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping;

/// <summary>
/// A replay that was started while the client was holding events, and ended with failures because the client left, leaves the observer
/// with a failed partition and a job. It is not removed, however long its client has been away.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_a_replay_ended_with_failures_when_the_client_left(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string ObserverId = string.Empty;
        public int RemovedBySweep;
        public given.Footprint Before = given.Footprint.None;
        public given.Footprint After = given.Footprint.None;
        public DateTimeOffset GoneSince;
        public DateTimeOffset SweptAt;

        async Task Because()
        {
            var blocking = new given.UnseenBlockingReactor();
            var client = await StartClient(blocking);
            ObserverId = client.Reactor<given.UnseenBlockingReactor>().Id.Value;
            var appended = await Append(_store, 2);
            await WaitForHandledInOrder(blocking, appended);

            blocking.Block();
            await ((IChronicleServicesAccessor)_store.Connection).Services.Observers.Replay(new()
            {
                EventStore = _storeName,
                Namespace = Concepts.EventStoreNamespaceName.Default.Value,
                ObserverId = ObserverId,
                EventSequenceId = EventSequenceId.Log.Value
            });
            await WaitForHandled(blocking, 3);

            await GoAwayLeavingTheObserverAsItIs(client, ObserverId);
            await WaitUntil("the replay to have ended with a failed partition", async () => (await FootprintOf(ObserverId)) is { Jobs: > 0, FailedPartitions: > 0 });
            Before = await FootprintOf(ObserverId);
            await WaitUntilUnseenForThePeriod(ObserverId);
            GoneSince = await LastSeen(ObserverId);
            SweptAt = DateTimeOffset.UtcNow;

            RemovedBySweep = await Sweeper.Sweep();
            After = await FootprintOf(ObserverId);
        }
    }

    [Fact] void should_have_been_unseen_for_longer_than_the_period() => (Context.SweptAt - Context.GoneSince).ShouldBeGreaterThan(given.UnseenObserverFixture.Period);
    [Fact] void should_remove_nothing() => Context.RemovedBySweep.ShouldEqual(0);
    [Fact] void should_keep_the_definition() => Context.After.HasDefinition.ShouldBeTrue();
    [Fact] void should_keep_the_state() => Context.After.HasState.ShouldBeTrue();
    [Fact] void should_keep_the_replay_job() => Context.After.Jobs.ShouldEqual(Context.Before.Jobs);
    [Fact] void should_keep_the_failed_partition() => Context.After.FailedPartitions.ShouldEqual(Context.Before.FailedPartitions);
}
