// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers.and_the_reduction_does_not_change_state.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers;

[Collection(ChronicleCollection.Name)]
public class and_the_reduction_does_not_change_state(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_watched_reducer(fixture)
    {
        async Task Because()
        {
            await Change("target", 1, "initial");
            var unchanged = await EventStore.EventLog.Append("target", new ReducerWatchedNoChange());
            await Handler.WaitTillReachesEventSequenceNumber(unchanged.SequenceNumber);
            await Change("target", 2, "same-key barrier");
        }
    }

    [Fact] void should_watch_only_changed_state() => Context.Watch.For("target").Select(_ => _.ReadModel!.Value).ShouldEqual([1, 2]);
    [Fact] void should_dispatch_only_changed_state() => Context.Reactions.For("target").Select(_ => _.ReadModel!.Value).ShouldEqual([1, 2]);
}
