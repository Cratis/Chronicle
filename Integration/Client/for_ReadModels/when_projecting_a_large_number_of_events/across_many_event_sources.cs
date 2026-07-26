// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_projecting_a_large_number_of_events.across_many_event_sources.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_projecting_a_large_number_of_events;

[Collection(ChronicleCollection.Name)]
public class across_many_event_sources(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public const int NumberOfEventSources = 500;

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

            for (var index = 0; index < NumberOfEventSources; index++)
            {
                await EventStore.EventLog.AppendMany(
                    EventSourceIds[index],
                    [new SomeEvent(index), new AnotherEvent($"value-{index}")]);
            }

            await WaitTillAllInstancesAreMaterialized();
            Instances = await EventStore.ReadModels.GetInstances<SomeReadModel>();
        }

        /// <summary>
        /// Polls the sink-backed endpoint until every instance has landed. The projection observer
        /// materializes asynchronously, so without this the assertions would run against a partially
        /// caught-up read side.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        async Task WaitTillAllInstancesAreMaterialized()
        {
            using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
            var readModels = Services.GetRequiredService<IServices>().ReadModels;
            while (true)
            {
                var response = await readModels.GetInstances(new GetInstancesRequest
                {
                    EventStore = Constants.EventStore,
                    Namespace = "Default",
                    ReadModel = typeof(SomeReadModel).FullName,
                    Page = 0,
                    PageSize = 1
                });

                if (response.TotalCount >= NumberOfEventSources)
                {
                    break;
                }

                await Task.Delay(100, cts.Token);
            }
        }
    }

    [Fact] void should_materialize_an_instance_per_event_source() => Context.Instances.Count().ShouldEqual(context.NumberOfEventSources);
    [Fact] void should_project_every_number_exactly_once() => Context.Instances.Select(_ => _.Number).Order().ShouldEqual(Enumerable.Range(0, context.NumberOfEventSources));
    [Fact] void should_project_the_value_for_every_instance() => Context.Instances.Count(_ => _.Value == $"value-{_.Number}").ShouldEqual(context.NumberOfEventSources);
}
