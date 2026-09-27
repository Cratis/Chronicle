// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_registering_explicitly.a_passive_projection_after_connecting.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_registering_explicitly;

[Collection(ChronicleCollection.Name)]
public class a_passive_projection_after_connecting(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public EventSourceId EventSourceId = $"passive-{Guid.NewGuid()}";
        public PassiveExplicitlyRegisteredReadModel Result;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        async Task Because()
        {
            await EventStore.Projections.Register<PassiveExplicitlyRegisteredReadModel>(projection => projection.Passive().From<SomeEvent>());
            await EventStore.EventLog.Append(EventSourceId, new SomeEvent(42));

            // A passive projection is never materialized, so reading it computes the instance from the events on demand.
            Result = await EventStore.ReadModels.GetInstanceById<PassiveExplicitlyRegisteredReadModel>(EventSourceId.Value);
        }
    }

    [Fact] void should_return_the_instance() => Context.Result.ShouldNotBeNull();
    [Fact] void should_compute_it_from_the_events() => Context.Result.Number.ShouldEqual(42);
}
