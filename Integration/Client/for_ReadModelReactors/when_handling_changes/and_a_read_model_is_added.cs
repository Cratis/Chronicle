// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_handling_changes.and_a_read_model_is_added.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.when_handling_changes;

[Collection(ChronicleCollection.Name)]
public class and_a_read_model_is_added(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(WatchedEvent)];
        public override IEnumerable<Type> Projections => [typeof(WatchedProjection)];
        public override IEnumerable<Type> ReadModelReactors => [typeof(RecordingReadModelReactor)];

        public RecordingReadModelReactor Reactor;
        public EventSourceId EventSourceId;

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new RecordingReadModelReactor();
            services.AddSingleton(Reactor);
        }

        void Establish() => EventSourceId = "watched-source";

        async Task Because()
        {
            // Ensure the underlying watch subscription is established before producing the event.
            await EventStore.ReadModels.GetWatcherFor<WatchedReadModel>().Subscribed;

            await EventStore.EventLog.Append(EventSourceId, new WatchedEvent(42));

            await Reactor.AddedSignal.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
    }

    [Fact] void should_invoke_the_added_method() => Context.Reactor.AddedModel.ShouldNotBeNull();
    [Fact] void should_pass_the_projected_read_model() => Context.Reactor.AddedModel!.Number.ShouldEqual(42);
    [Fact] void should_provide_the_causing_event_context() => Context.Reactor.AddedContext!.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
}
