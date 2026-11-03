// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers.and_several_events_create_one_model.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers;

[Collection(ChronicleCollection.Name)]
public class and_several_events_create_one_model(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_watched_reducer(fixture)
    {
        public EventSequenceNumber[] Sequences;

        async Task Because()
        {
            var result = await EventStore.EventLog.AppendMany("batch", [new ReducerWatchedEvent(1, "first"), new ReducerWatchedEvent(2, "last")]);
            Sequences = result.SequenceNumbers.ToArray();
            await Watch.WaitForSequence("batch", Sequences[^1], Token);
            await Reactions.WaitForSequence("batch", Sequences[^1], Token);
            if (!Control.Invocations.Values.Any(group => Sequences.All(group.Contains)))
            {
                throw new InvalidOperationException("STOP: the fixture did not deliver multiple target events through one reducer invoker instance");
            }
        }
    }

    [Fact] void should_reduce_more_than_one_event() => Context.Sequences.Length.ShouldEqual(2);
    [Fact] void should_watch_one_net_addition() => Context.Watch.For("batch").Single().ChangeType.ShouldEqual(ReadModelChangeType.Added);
    [Fact] void should_dispatch_one_net_addition() => Context.Reactions.For("batch").Single().ChangeType.ShouldEqual(ReadModelChangeType.Added);
    [Fact] void should_carry_the_last_events_context() => Context.Watch.For("batch").Single().ChangeContext!.SequenceNumber.ShouldEqual(Context.Sequences[^1]);
    [Fact] void should_carry_the_net_model() => Context.Watch.For("batch").Single().ReadModel!.Value.ShouldEqual(2);
}
