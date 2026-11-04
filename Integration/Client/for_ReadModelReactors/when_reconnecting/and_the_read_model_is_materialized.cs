// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_reconnecting.and_the_read_model_is_materialized.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.when_reconnecting;

[Collection(ChronicleCollection.Name)]
public class and_the_read_model_is_materialized(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(WatchedEvent)];
        public override IEnumerable<Type> Projections => [typeof(MaterializedProjection)];
        public override IEnumerable<Type> ReadModelReactors => [typeof(RecordingMaterializedReadModelReactor)];

        public RecordingMaterializedReadModelReactor Reactor;

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new RecordingMaterializedReadModelReactor();
            services.AddSingleton(Reactor);
        }

        async Task Because()
        {
            await EventStore.EventLog.Append("first", new WatchedEvent(1));
            await Reactor.WaitForAdded("first");

            await EventStore.Connection.Lifecycle.Disconnected();
            await EventStore.Connection.Connect();

            await EventStore.EventLog.Append("second", new WatchedEvent(2));
            await Reactor.WaitForAdded("second");
        }
    }

    [Fact] void should_deliver_the_new_instance_once() => Context.Reactor.CountFor("second").ShouldEqual(1);
    [Fact] void should_not_deliver_the_replayed_instance_again() => Context.Reactor.CountFor("first").ShouldEqual(1);
}
