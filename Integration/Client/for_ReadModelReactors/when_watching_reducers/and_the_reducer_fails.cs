// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers.and_the_reducer_fails.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers;

[Collection(ChronicleCollection.Name)]
public class and_the_reducer_fails(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_watched_reducer(fixture)
    {
        public int WatchedBeforeRecovery;
        public int ReactionsBeforeRecovery;

        async Task Because()
        {
            var failed = await EventStore.EventLog.Append("failure", new ReducerWatchedFailure(1, "recovered"));
            await Control.Failed.Task.WaitAsync(Token);
            await Handler.WaitForThereToBeFailedPartitions();
            await Handshake();
            WatchedBeforeRecovery = Watch.For("failure").Length;
            ReactionsBeforeRecovery = Reactions.For("failure").Length;
            Control.Recover.TrySetResult();
            await Watch.WaitForSequence("failure", failed.SequenceNumber, Token);
            await Reactions.WaitForSequence("failure", failed.SequenceNumber, Token);
            await Change("failure", 2, "later update");
        }
    }

    [Fact] void should_not_watch_a_failed_reduction() => Context.WatchedBeforeRecovery.ShouldEqual(0);
    [Fact] void should_not_dispatch_a_failed_reduction() => Context.ReactionsBeforeRecovery.ShouldEqual(0);
    [Fact] void should_watch_the_later_update() => Context.Watch.For("failure")[^1].ReadModel!.Value.ShouldEqual(2);
    [Fact] void should_dispatch_the_later_update() => Context.Reactions.For("failure")[^1].ReadModel!.Value.ShouldEqual(2);
}
