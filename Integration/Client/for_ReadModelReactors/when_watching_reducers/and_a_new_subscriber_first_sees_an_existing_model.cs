// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers.and_a_new_subscriber_first_sees_an_existing_model.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers;

[Collection(ChronicleCollection.Name)]
public class and_a_new_subscriber_first_sees_an_existing_model(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_watched_reducer(fixture)
    {
        protected override bool SubscribeInitially => false;

        async Task Because()
        {
            var result = await EventStore.EventLog.Append("existing", new ReducerWatchedEvent(1, "before subscription"));
            await Handler.WaitTillReachesEventSequenceNumber(result.SequenceNumber);
            await StartWatching();
            await Change("existing", 2, "after subscription");
        }
    }

    [Fact] void should_report_modified_to_the_new_watcher() => Context.Watch.For("existing").Single().ChangeType.ShouldEqual(ReadModelChangeType.Modified);
    [Fact] void should_report_modified_to_the_reactor_collector() => Context.Reactions.For("existing").Single().ChangeType.ShouldEqual(ReadModelChangeType.Modified);
}
