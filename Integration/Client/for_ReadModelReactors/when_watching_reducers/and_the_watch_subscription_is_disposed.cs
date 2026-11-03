// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers.and_the_watch_subscription_is_disposed.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers;

[Collection(ChronicleCollection.Name)]
public class and_the_watch_subscription_is_disposed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_watched_reducer(fixture)
    {
        public int DisposedWatchCount;
        public int OtherWatchCount;

        async Task Because()
        {
            await Change("target", 1, "initial");
            using var other = new Collector(EventStore.ReadModels.Watch<ReducerWatchedReadModel>());
            Watch.Dispose();
            var result = await EventStore.EventLog.Append("target", new ReducerWatchedEvent(2, "after dispose"));
            await other.WaitForSequence("target", result.SequenceNumber, Token);
            await Reactions.WaitForSequence("target", result.SequenceNumber, Token);
            DisposedWatchCount = Watch.For("target").Length;
            OtherWatchCount = other.For("target").Length;
        }
    }

    [Fact] void should_not_notify_the_disposed_subscription() => Context.DisposedWatchCount.ShouldEqual(1);
    [Fact] void should_keep_notifying_the_other_subscription() => Context.OtherWatchCount.ShouldEqual(1);
    [Fact] void should_keep_notifying_the_reactor() => Context.Reactions.For("target").Length.ShouldEqual(2);
}
