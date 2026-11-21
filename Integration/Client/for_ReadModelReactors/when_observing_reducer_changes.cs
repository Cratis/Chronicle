// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_observing_reducer_changes.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[Collection(ChronicleCollection.Name)]
public class when_observing_reducer_changes(context setup) : Given<context>(setup)
{
    public class context(ChronicleFixture fixture) : given.a_watched_reducer(fixture)
    {
        public readonly List<EventSequenceNumber> Sequences = [];

        async Task Because()
        {
            Subscribe();
            await Ready();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var token = timeout.Token;
            Sequences.Add(await Append("target", new Reduction(1), ReadModelChangeType.Added, token));
            Sequences.Add(await Append("target", new Reduction(2), ReadModelChangeType.Modified, token));
            Sequences.Add(await Append("target", new Reduction(0, "remove"), ReadModelChangeType.Removed, token));
            Sequences.Add(await Append("target", new Reduction(3), ReadModelChangeType.Added, token));
            await VerifyNoChangeAndFailure(token);
        }

        async Task VerifyNoChangeAndFailure(CancellationToken token)
        {
            var unchanged = await EventStore.EventLog.Append("target", new Reduction(0, "unchanged"));
            await Handler.WaitTillReachesEventSequenceNumber(unchanged.SequenceNumber).WaitAsync(token);
            await EventStore.EventLog.Append("failed", new Reduction(0, "fail"));
            await Handler.WaitForThereToBeFailedPartitions().WaitAsync(token);
            await Append("after-failure", new Reduction(1), ReadModelChangeType.Added, token);
        }
    }

    [Fact] void should_watch_added_modified_removed_and_recreated() => Context.WatchFrames.For("target").Select(_ => _.Type).ShouldEqual([ReadModelChangeType.Added, ReadModelChangeType.Modified, ReadModelChangeType.Removed, ReadModelChangeType.Added]);
    [Fact] void should_dispatch_the_same_reactor_changes() => Context.ReactorFrames.For("target").Select(_ => _.Type).ShouldEqual(Context.WatchFrames.For("target").Select(_ => _.Type));
    [Fact] void should_keep_each_watch_sequence_number() => Context.WatchFrames.For("target").Select(_ => _.Context.SequenceNumber).ShouldEqual(Context.Sequences);
    [Fact] void should_keep_the_same_reactor_context() => Context.ReactorFrames.For("target").Select(_ => _.Context).ShouldEqual(Context.WatchFrames.For("target").Select(_ => _.Context));
    [Fact] void should_keep_the_event_store() => Context.WatchFrames.For("target").All(_ => _.Context.EventStore == Context.EventStore.Name).ShouldBeTrue();
    [Fact] void should_keep_the_namespace() => Context.WatchFrames.For("target").All(_ => _.Context.Namespace == Context.EventStore.Namespace).ShouldBeTrue();
    [Fact] void should_not_publish_failed_reductions_to_watches() => Context.WatchFrames.For("failed").ShouldBeEmpty();
    [Fact] void should_not_dispatch_failed_reductions_to_reactors() => Context.ReactorFrames.For("failed").ShouldBeEmpty();
}
