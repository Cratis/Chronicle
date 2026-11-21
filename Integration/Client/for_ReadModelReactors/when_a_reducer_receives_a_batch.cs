// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_a_reducer_receives_a_batch.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[Collection(ChronicleCollection.Name)]
public class when_a_reducer_receives_a_batch(context setup) : Given<context>(setup)
{
    public class context(ChronicleFixture fixture) : given.a_watched_reducer(fixture)
    {
        public EventSequenceNumber LastSequence;
        public ReducedModel Model;

        async Task Because()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var token = timeout.Token;
            Subscribe();
            await Ready(token);

            // AppendMany persists and enqueues the two same-key events together; the reducer's page bound is two.
            var result = await EventStore.EventLog.AppendMany("batch", [new Reduction(1), new Reduction(2)]);
            result.IsSuccess.ShouldBeTrue();
            LastSequence = result.SequenceNumbers.Last();
            await Received("batch", ReadModelChangeType.Added, LastSequence, token);
            Model = await EventStore.ReadModels.GetInstanceById<ReducedModel>("batch");
        }
    }

    [Fact] void should_actually_reduce_two_events_in_one_batch() => Context.Reducer.BatchSizes.Last().ShouldEqual(2);
    [Fact] void should_watch_one_change_for_the_materialized_batch() => Context.WatchFrames.For("batch").Select(_ => _.Type).ShouldEqual([ReadModelChangeType.Added]);
    [Fact] void should_dispatch_one_reactor_change_for_the_materialized_batch() => Context.ReactorFrames.For("batch").Select(_ => _.Type).ShouldEqual([ReadModelChangeType.Added]);
    [Fact] void should_report_the_last_event_in_the_batch() => Context.WatchFrames.For("batch").Single().Context.SequenceNumber.ShouldEqual(Context.LastSequence);
    [Fact] void should_preserve_the_reactor_batch_context() => Context.ReactorFrames.For("batch").Single().Context.ShouldEqual(Context.WatchFrames.For("batch").Single().Context);
    [Fact] void should_materialize_the_last_reduction() => Context.Model.Value.ShouldEqual(2);
}
