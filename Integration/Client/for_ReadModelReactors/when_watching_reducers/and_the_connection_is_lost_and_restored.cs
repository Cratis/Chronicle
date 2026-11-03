// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers.and_the_connection_is_lost_and_restored.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers;

[Collection(ChronicleCollection.Name)]
public class and_the_connection_is_lost_and_restored(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_watched_reducer(fixture)
    {
        async Task Because()
        {
            await Change("existing", 1, "before reconnect");
            await EventStore.Connection.Lifecycle.Disconnected();
            await EventStore.Connection.Connect();
            await StartWatching();
            await Change("existing", 2, "after reconnect");
            await Change("genuinely-new", 1, "new after reconnect");
        }
    }

    [Fact] void should_report_modified_for_the_existing_key() => Context.Watch.For("existing").Single().ChangeType.ShouldEqual(ReadModelChangeType.Modified);
    [Fact] void should_dispatch_modified_for_the_existing_key() => Context.Reactions.For("existing").Single().ChangeType.ShouldEqual(ReadModelChangeType.Modified);
    [Fact] void should_report_added_for_the_new_key() => Context.Watch.For("genuinely-new").Single().ChangeType.ShouldEqual(ReadModelChangeType.Added);
    [Fact] void should_dispatch_added_for_the_new_key() => Context.Reactions.For("genuinely-new").Single().ChangeType.ShouldEqual(ReadModelChangeType.Added);
}
