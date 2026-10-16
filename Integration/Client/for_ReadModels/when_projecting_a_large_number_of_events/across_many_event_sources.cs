// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_projecting_a_large_number_of_events.across_many_event_sources.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_projecting_a_large_number_of_events;

[Collection(ChronicleCollection.Name)]
public class across_many_event_sources(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public const int NumberOfEventSources = 500;
        const int EventsPerEventSource = 2;

        public IReadOnlyList<EventSourceId> EventSourceIds { get; private set; }
        public IEnumerable<SomeReadModel> Instances { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(AnotherEvent)];
        public override IEnumerable<Type> Projections => [typeof(SomeProjection)];

        void Establish() =>
            EventSourceIds = [.. Enumerable.Range(0, NumberOfEventSources).Select(_ => (EventSourceId)$"large-source-{_}")];

        async Task Because()
        {
            var projection = EventStore.Projections.GetHandlerFor<SomeProjection>();
            await projection.WaitTillActive();

            var tail = EventSequenceNumber.Unavailable;
            for (var index = 0; index < NumberOfEventSources; index++)
            {
                var result = await EventStore.EventLog.AppendMany(
                    EventSourceIds[index],
                    [new SomeEvent(index), new AnotherEvent($"value-{index}")]);
                tail = result.TailSequenceNumber;
            }

            await projection.WaitTillReachesEventSequenceNumber(tail, TimeoutForMaterialization());
            Instances = await EventStore.ReadModels.GetInstances<SomeReadModel>();
        }

        /// <summary>
        /// The projection handles every appended event before the sink is read, so the wait is sized for the
        /// number of events rather than for a single one. A slow machine only makes the projection take longer;
        /// it fails here only when the projection never reaches the last appended event.
        /// </summary>
        /// <returns>The time allowed for the projection to reach the last appended event.</returns>
        static TimeSpan TimeoutForMaterialization()
        {
            var defaultTimeout = TimeSpanFactory.DefaultTimeout();
            if (defaultTimeout == Timeout.InfiniteTimeSpan)
            {
                return defaultTimeout;
            }

            var forEveryEvent = TimeSpan.FromMilliseconds(NumberOfEventSources * EventsPerEventSource * 100);
            return forEveryEvent > defaultTimeout ? forEveryEvent : defaultTimeout;
        }
    }

    [Fact] void should_materialize_an_instance_per_event_source() => Context.Instances.Count().ShouldEqual(context.NumberOfEventSources);
    [Fact] void should_project_every_number_exactly_once() => Context.Instances.Select(_ => _.Number).Order().ShouldEqual(Enumerable.Range(0, context.NumberOfEventSources));
    [Fact] void should_project_the_value_for_every_instance() => Context.Instances.Count(_ => _.Value == $"value-{_.Number}").ShouldEqual(context.NumberOfEventSources);
}
