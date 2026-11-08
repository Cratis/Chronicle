// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_reconnecting.and_the_read_model_is_watched.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.when_reconnecting;

[Collection(ChronicleCollection.Name)]
public class and_the_read_model_is_watched(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(WatchedEvent)];
        public override IEnumerable<Type> Projections => [typeof(WatchedProjection)];
        public override IEnumerable<Type> ReadModelReactors => [typeof(RecordingReconnectingReadModelReactor)];

        public RecordingReconnectingReadModelReactor Reactor;

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new RecordingReconnectingReadModelReactor();
            services.AddSingleton(Reactor);
        }

        async Task Because()
        {
            await EventStore.ReadModels.GetWatcherFor<WatchedReadModel>().Subscribed.WaitAsync(TimeSpan.FromSeconds(30));
            await EventStore.EventLog.Append("first", new WatchedEvent(1));
            await Reactor.WaitForAdded("first");

            await EventStore.Connection.Lifecycle.Disconnected();
            await EventStore.Connection.Connect();

            await EventStore.ReadModels.GetWatcherFor<WatchedReadModel>().Subscribed.WaitAsync(TimeSpan.FromSeconds(30));
            await EventStore.EventLog.Append("second", new WatchedEvent(2));
            await Reactor.WaitForAdded("second");
        }
    }

    [Fact] void should_continue_delivering_changes() => Context.Reactor.CountFor("second").ShouldEqual(1);
    [Fact] void should_deliver_the_original_change_once() => Context.Reactor.CountFor("first").ShouldEqual(1);
}
