// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_seeded_events.with_events_seeded_using_seeding_api.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_seeded_events;

[Collection(ChronicleCollection.Name)]
public class with_events_seeded_using_seeding_api(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public string EventSourceId;
        public ItemsReadModel Result;
        public int NumberOfSeedEvents;

        public override IEnumerable<Type> EventTypes => [typeof(ItemAdded)];
        public override IEnumerable<Type> Projections => [typeof(ItemsProjection)];

        protected override void ConfigureServices(IServiceCollection services) =>
            services.AddSingleton(new ItemsProjection());

        void Establish()
        {
            EventSourceId = Guid.NewGuid().ToString();
            NumberOfSeedEvents = 5;
        }

        async Task Because()
        {
            var seedEvents = Enumerable.Range(0, NumberOfSeedEvents)
                .Select(i => (object)new ItemAdded($"Item {i}"))
                .ToList();

            EventStore.Seeding.ForEventSource(EventSourceId, seedEvents);
            await EventStore.Seeding.Register();

            var projection = EventStore.Projections.GetHandlerFor<ItemsProjection>();
            await projection.WaitTillSubscribed();

            var appendedEvents = await EventStore.EventLog.GetForEventSourceIdAndEventTypes(
                EventSourceId,
                [typeof(ItemAdded).GetEventType()]);

            var lastSequenceNumber = appendedEvents.MaxBy(_ => _.Context.SequenceNumber)?.Context.SequenceNumber;
            if (lastSequenceNumber is not null)
            {
                await projection.WaitTillReachesEventSequenceNumber(lastSequenceNumber);
            }

            Result = await EventStore.ReadModels.GetInstanceById<ItemsReadModel>(new ReadModelKey(EventSourceId));
        }
    }

    [Fact] void should_have_correct_total_count() => Context.Result.TotalCount.ShouldEqual(Context.NumberOfSeedEvents);
}
