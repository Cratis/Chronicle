// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_seeded_events.with_events_seeded_using_seeding_api.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_seeded_events;

[Collection(ChronicleCollection.Name)]
public class with_events_seeded_using_seeding_api(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        ChronicleInProcessFixture _chronicleInProcessFixture = chronicleInProcessFixture;
#pragma warning restore CA2213 // Disposable fields should be disposed

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
            await projection.WaitTillActive();

            var appendedEvents = await EventStore.EventLog.GetForEventSourceIdAndEventTypes(
                EventSourceId,
                [typeof(ItemAdded).GetEventType()]);

            var lastSequenceNumber = appendedEvents.MaxBy(_ => _.Context.SequenceNumber)?.Context.SequenceNumber;
            if (lastSequenceNumber is not null)
            {
                await projection.WaitTillReachesEventSequenceNumber(lastSequenceNumber);
            }

            var filter = Builders<ItemsReadModel>.Filter.Eq(new StringFieldDefinition<ItemsReadModel, string>("_id"), EventSourceId);
            var cursor = await _chronicleInProcessFixture.ReadModels.Database.GetCollection<ItemsReadModel>().FindAsync(filter);
            Result = await cursor.FirstOrDefaultAsync();
        }
    }

    [Fact] void should_have_correct_total_count() => Context.Result.TotalCount.ShouldEqual(Context.NumberOfSeedEvents);
}
