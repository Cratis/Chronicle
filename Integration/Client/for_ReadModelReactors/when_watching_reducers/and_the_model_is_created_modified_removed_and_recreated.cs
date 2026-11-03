// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers.and_the_model_is_created_modified_removed_and_recreated.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers;

[Collection(ChronicleCollection.Name)]
public class and_the_model_is_created_modified_removed_and_recreated(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_watched_reducer(fixture)
    {
        public readonly List<EventSequenceNumber> Sequences = [];
        public ReducerWatchedReadModel Persisted;
        public DateTimeOffset Occurred = DateTimeOffset.Parse("2026-01-02T03:04:05Z");
        public Guid Correlation = Guid.NewGuid();

        async Task Because()
        {
            var added = await EventStore.EventLog.Append("target", new ReducerWatchedEvent(1, "created"), correlationId: Correlation, occurred: Occurred);
            await Watch.WaitForSequence("target", added.SequenceNumber, Token);
            await Reactions.WaitForSequence("target", added.SequenceNumber, Token);
            Sequences.Add(added.SequenceNumber);
            Sequences.Add((await Change("target", 2, "modified")).ChangeContext!.SequenceNumber);
            var removed = await EventStore.EventLog.Append("target", new ReducerWatchedDelete());
            await Watch.WaitForSequence("target", removed.SequenceNumber, Token);
            await Reactions.WaitForSequence("target", removed.SequenceNumber, Token);
            Sequences.Add(removed.SequenceNumber);
            Sequences.Add((await Change("target", 3, "recreated")).ChangeContext!.SequenceNumber);
            Persisted = await EventStore.ReadModels.GetInstanceById<ReducerWatchedReadModel>("target");
        }
    }

    [Fact] void should_watch_the_persisted_lifecycle() => Context.Watch.For("target").Select(_ => _.ChangeType).ShouldEqual([ReadModelChangeType.Added, ReadModelChangeType.Modified, ReadModelChangeType.Removed, ReadModelChangeType.Added]);
    [Fact] void should_dispatch_the_same_lifecycle() => Context.Reactions.For("target").Select(_ => _.ChangeType).ShouldEqual([ReadModelChangeType.Added, ReadModelChangeType.Modified, ReadModelChangeType.Removed, ReadModelChangeType.Added]);
    [Fact] void should_not_expose_a_removed_model() => Context.Watch.For("target")[2].ReadModel.ShouldBeNull();
    [Fact] void should_carry_the_causing_sequences() => Context.Watch.For("target").Select(_ => _.ChangeContext!.SequenceNumber).ShouldEqual(Context.Sequences);
    [Fact] void should_carry_the_reactor_sequences() => Context.Reactions.For("target").Select(_ => _.ChangeContext!.SequenceNumber).ShouldEqual(Context.Sequences);
    [Fact] void should_carry_the_occurrence_time() => Context.Watch.For("target")[0].ChangeContext!.Occurred.ShouldEqual(Context.Occurred);
    [Fact] void should_carry_the_correlation() => Context.Watch.For("target")[0].ChangeContext!.CorrelationId.Value.ShouldEqual(Context.Correlation);
    [Fact] void should_persist_the_recreated_model() => Context.Persisted.ShouldEqual(new ReducerWatchedReadModel(3, "recreated"));
}
