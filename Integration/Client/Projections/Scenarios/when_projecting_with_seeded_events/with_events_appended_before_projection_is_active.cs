// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using MongoDB.Driver;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_seeded_events.with_events_appended_before_projection_is_active.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_seeded_events;

[Collection(ChronicleCollection.Name)]
public class with_events_appended_before_projection_is_active(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        ChronicleFixture _chronicleInProcessFixture = chronicleInProcessFixture;
#pragma warning restore CA2213 // Disposable fields should be disposed

        public EventSourceId EventSourceId;
        public ItemsReadModel Result;
        public int NumberOfEvents;
        public EventSequenceNumber LastEventSequenceNumber;

        public override IEnumerable<Type> EventTypes => [typeof(ItemAdded)];
        public override IEnumerable<Type> Projections => [typeof(ItemsProjection)];

        protected override void ConfigureServices(IServiceCollection services) =>
            services.AddSingleton(new ItemsProjection());

        void Establish()
        {
            EventSourceId = Guid.NewGuid().ToString();
            NumberOfEvents = 5;
        }

        async Task Because()
        {
            for (var i = 0; i < NumberOfEvents; i++)
            {
                var result = await EventStore.EventLog.Append(EventSourceId, new ItemAdded($"Item {i}"));
                LastEventSequenceNumber = result.SequenceNumber;
            }

            var projection = EventStore.Projections.GetHandlerFor<ItemsProjection>();
            await projection.WaitTillActive();
            await projection.WaitTillReachesEventSequenceNumber(LastEventSequenceNumber);

            var filter = Builders<ItemsReadModel>.Filter.Eq(new StringFieldDefinition<ItemsReadModel, string>("_id"), EventSourceId);
            var cursor = await _chronicleInProcessFixture.ReadModels.Database.GetCollection<ItemsReadModel>().FindAsync(filter);
            Result = await cursor.FirstOrDefaultAsync();
        }
    }

    [Fact] void should_have_correct_total_count() => Context.Result.TotalCount.ShouldEqual(Context.NumberOfEvents);
    [Fact] void should_have_last_handled_event_sequence_number() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
