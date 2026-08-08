// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_seeded_events.with_repeated_identical_events.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_seeded_events;

/// <summary>
/// The pair that decides whether seeding can express a history at all, measured against a real kernel.
/// Three events are seeded on one event source, the first and the last byte-identical: they are two facts
/// that really happened, and both have to land. At the same time the client sends every entry twice - once
/// bucketed by event type, once by event source - and that double-send still has to fold back to one, or
/// six events would land instead of three. A value-based collapse gets exactly one of the two right.
/// </summary>
/// <param name="context">The context the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class with_repeated_identical_events(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public string EventSourceId;
        public ItemsReadModel Result;
        public IEnumerable<string> AppendedNames;

        public override IEnumerable<Type> EventTypes => [typeof(ItemAdded)];
        public override IEnumerable<Type> Projections => [typeof(ItemsProjection)];

        protected override void ConfigureServices(IServiceCollection services) =>
            services.AddSingleton(new ItemsProjection());

        void Establish() => EventSourceId = Guid.NewGuid().ToString();

        async Task Because()
        {
            EventStore.Seeding.ForEventSource(EventSourceId, [new ItemAdded("Recurring"), new ItemAdded("Between"), new ItemAdded("Recurring")]);
            await EventStore.Seeding.Register();

            var projection = EventStore.Projections.GetHandlerFor<ItemsProjection>();
            await projection.WaitTillSubscribed();

            var appendedEvents = await EventStore.EventLog.GetForEventSourceIdAndEventTypes(
                EventSourceId,
                [typeof(ItemAdded).GetEventType()]);

            var ordered = appendedEvents.OrderBy(_ => _.Context.SequenceNumber.Value).ToArray();
            AppendedNames = [.. ordered.Select(_ => ((ItemAdded)_.Content).Name)];

            var lastSequenceNumber = ordered.LastOrDefault()?.Context.SequenceNumber;
            if (lastSequenceNumber is not null)
            {
                await projection.WaitTillReachesEventSequenceNumber(lastSequenceNumber);
            }

            Result = await EventStore.ReadModels.GetInstanceById<ItemsReadModel>(new ReadModelKey(EventSourceId));
        }
    }

    [Fact] void should_append_every_seeded_fact() => Context.AppendedNames.Count().ShouldEqual(3);
    [Fact] void should_keep_the_repeat_in_its_place() => Context.AppendedNames.ToArray().ShouldEqual<string[]>(["Recurring", "Between", "Recurring"]);
    [Fact] void should_not_append_the_client_double_send() => Context.AppendedNames.Count(_ => _ == "Recurring").ShouldEqual(2);
    [Fact] void should_project_every_seeded_fact() => Context.Result.TotalCount.ShouldEqual(3);
}
