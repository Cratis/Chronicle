// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_handling_changes.and_a_read_model_is_added_by_a_multi_event_batch.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.when_handling_changes;

[Collection(ChronicleCollection.Name)]
public class and_a_read_model_is_added_by_a_multi_event_batch(context context) : Given<context>(context)
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
            // Ensure the watch subscription is established before producing the events.
            await EventStore.ReadModels.GetWatcherFor<WatchedReadModel>().Subscribed;

            // Two events for the SAME new source, appended together so the projection observer
            // handles them as a single batch. The instance is new as of the first event, so the
            // reactor must see an Added carrying the final projected state — not a Modified — even
            // though a later event in the same batch updates it.
            var events = EventForEventSourceIdHelpers.CreateMultiple(i => new WatchedEvent(i == 0 ? 1 : 42), 2, EventSourceId).ToList();
            await EventStore.EventLog.AppendMany(events);

            await Task.WhenAny(Reactor.AddedSignal.Task, Reactor.ModifiedSignal.Task).WaitAsync(TimeSpan.FromSeconds(30));
        }
    }

    [Fact] void should_invoke_the_added_method() => Context.Reactor.AddedModel.ShouldNotBeNull();
    [Fact] void should_not_invoke_the_modified_method() => Context.Reactor.ModifiedModel.ShouldBeNull();
    [Fact] void should_pass_the_final_projected_state() => Context.Reactor.AddedModel!.Number.ShouldEqual(42);
}
